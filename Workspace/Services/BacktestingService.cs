using WebApp.Data;
using WebApp.Models;
using WebApp.Services;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Services
{
    /// <summary>
    /// Interface for backtesting and trading bot execution services
    /// </summary>
    public interface IBacktestingService
    {
        Task<bool> ExecuteUserBotAsync(long userBotId);
        Task<bool> SimulateTradeAsync(long sessionId, string symbol, decimal currentPrice, DateTime timestamp);
        Task<decimal> GetCurrentPriceAsync(string symbol);
        Task UpdatePerformanceMetricsAsync(long sessionId);
        Task<long> RunHistoricalBacktestAsync(long userBotId, DateTime startDate, DateTime endDate);
        Task<decimal> GetHistoricalPriceAsync(string symbol, DateTime targetDate);
        Task<(DateTime? earliestDate, DateTime? latestDate)> GetAvailableDataRangeAsync(string symbol);
    }

    /// <summary>
    /// Service for handling trading bot execution, backtesting, and performance calculations
    /// This service simulates trading bot execution using historical market data
    /// </summary>
    public class BacktestingService : IBacktestingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITradingBotDAL _tradingBotService;
        private readonly IBotConfigurationService _botConfigService;

        public BacktestingService(ApplicationDbContext context, ITradingBotDAL tradingBotService, IBotConfigurationService botConfigService)
        {
            _context = context;
            _tradingBotService = tradingBotService;
            _botConfigService = botConfigService;
        }

        /// <summary>
        /// Compatibility adapter to provide legacy BotConfiguration properties from JSON
        /// This allows existing BacktestingService code to work without major changes
        /// </summary>
        private class LegacyBotConfiguration
        {
            public decimal WeeklyBuyAmount { get; set; }
            public decimal InvestmentAmount { get; set; }
            public string RiskLevel { get; set; } = "Medium";
            public int StartDay { get; set; }
            public string DCAFrequency { get; set; } = "Weekly";

            public static LegacyBotConfiguration FromJson(IBotConfigurationService configService, BotConfiguration config, string strategy)
            {
                var legacy = new LegacyBotConfiguration();

                if (config.Parameters != null && strategy == "DCA")
                {
                    var dcaConfig = configService.DeserializeConfiguration<DcaBotConfiguration>(config.Parameters);
                    if (dcaConfig != null)
                    {
                        legacy.WeeklyBuyAmount = dcaConfig.InvestmentAmount; // InvestmentAmount in DCA is per-execution amount
                        
                        // Use MaxTotalInvestment if set, otherwise leave as 0 to indicate unlimited
                        legacy.InvestmentAmount = dcaConfig.MaxTotalInvestment ?? 0;
                        
                        legacy.RiskLevel = "Medium"; // Default
                        legacy.StartDay = (int)(dcaConfig.DayOfWeek ?? DayOfWeek.Monday);
                        legacy.DCAFrequency = dcaConfig.Frequency.ToString();
                    }
                }

                return legacy;
            }
        }

        /// <summary>
        /// Execute a user bot according to its strategy and configuration
        /// </summary>
        public async Task<bool> ExecuteUserBotAsync(long userBotId)
        {
            try
            {
                var userBot = await _tradingBotService.GetUserBotByIdAsync(userBotId);
                if (userBot == null || userBot.Status != "Active")
                {
                    return false;
                }

                var configJson = await _tradingBotService.GetBotConfigurationAsync(userBotId);
                if (configJson == null)
                {
                    return false;
                }

                // Convert JSON config to legacy format for existing code compatibility
                var config = LegacyBotConfiguration.FromJson(_botConfigService, configJson, userBot.TradingBot?.Strategy ?? "DCA");

                // Check if it's time to execute based on strategy
                if (!ShouldExecuteNow(userBot, configJson, config))
                {
                    return true; // Not time to execute, but not an error
                }

                // Find or create active trading session
                var session = await GetOrCreateActiveSessionAsync(userBotId, config.InvestmentAmount);
                if (session == null)
                {
                    return false;
                }

                // Execute trade based on strategy
                var success = await ExecuteTradeByStrategyAsync(session.SessionId, userBot.TradingBot!.Strategy, config);
                
                if (success)
                {
                    // Update user bot last run time
                    await _tradingBotService.UpdateUserBotStatusAsync(userBotId, "Active");
                    
                    // Update performance metrics
                    await UpdatePerformanceMetricsAsync(session.SessionId);
                }

                return success;
            }
            catch (Exception ex)
            {
                // Log error (in a real application, use proper logging)
                Console.WriteLine($"Error executing user bot {userBotId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Simulate a trade execution for backtesting
        /// </summary>
        public async Task<bool> SimulateTradeAsync(long sessionId, string symbol, decimal currentPrice, DateTime timestamp)
        {
            try
            {
                var session = await _tradingBotService.GetTradingSessionByIdAsync(sessionId);
                if (session == null)
                {
                    return false;
                }

                var configJson = await _tradingBotService.GetBotConfigurationAsync(session.UserBotId);
                if (configJson == null)
                {
                    return false;
                }

                // Get user bot to determine strategy for config parsing
                var userBot = await _tradingBotService.GetUserBotByIdAsync(session.UserBotId);
                var config = LegacyBotConfiguration.FromJson(_botConfigService, configJson, userBot?.TradingBot?.Strategy ?? "DCA");

                // Calculate quantity to buy based on weekly amount
                var quantity = config.WeeklyBuyAmount / currentPrice;
                
                // Simulate trading fee (0.1% - typical for many exchanges)
                var fee = config.WeeklyBuyAmount * 0.001m;

                // Create the trade record with historical timestamp
                var success = await _tradingBotService.CreateTradeAsync(
                    sessionId, 
                    symbol, 
                    "BUY", 
                    currentPrice, 
                    quantity, 
                    fee,
                    timestamp  // Use historical timestamp instead of DateTime.UtcNow
                );

                return success;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get current price for a trading symbol from market data
        /// </summary>
        public async Task<decimal> GetCurrentPriceAsync(string symbol)
        {
            try
            {
                // Get the latest price from MarketData table
                var latestData = await _context.MarketData
                    .Where(m => m.Symbol == symbol.Replace("USDT", ""))
                    .OrderByDescending(m => m.OpenTime)
                    .FirstOrDefaultAsync();

                return latestData?.HighPrice ?? 50000m; // Default BTC price if no data
            }
            catch
            {
                // Return a default price if we can't get current price
                return symbol.StartsWith("BTC") ? 50000m : 3000m; // Default prices
            }
        }

        /// <summary>
        /// Update performance metrics for a trading session
        /// </summary>
        public async Task UpdatePerformanceMetricsAsync(long sessionId)
        {
            try
            {
                var trades = await _tradingBotService.GetTradesAsync(sessionId);
                if (!trades.Any())
                {
                    return;
                }

                // Calculate total invested (sum of all BUY trades)
                var totalInvested = trades
                    .Where(t => t.Side == "BUY")
                    .Sum(t => t.Value + t.Fee);

                // Calculate total quantity owned
                var totalQuantity = trades
                    .Where(t => t.Side == "BUY")
                    .Sum(t => t.Quantity);

                // Get current price for valuation
                var currentPrice = await GetCurrentPriceAsync(trades.First().Symbol);
                var totalValue = totalQuantity * currentPrice;

                // Calculate win rate (simplified - percentage of profitable positions)
                var profitableTrades = 0;
                foreach (var trade in trades.Where(t => t.Side == "BUY"))
                {
                    var valueAtCurrentPrice = trade.Quantity * currentPrice;
                    if (valueAtCurrentPrice > trade.Value)
                    {
                        profitableTrades++;
                    }
                }

                var winRate = trades.Count > 0 ? (decimal)profitableTrades / trades.Count(t => t.Side == "BUY") * 100 : 0;

                // Update metrics
                await _tradingBotService.UpdatePerformanceMetricsAsync(
                    sessionId,
                    totalInvested,
                    totalValue,
                    trades.Count,
                    winRate
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating performance metrics for session {sessionId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Run historical backtest for a user bot over a specific date range
        /// </summary>
        public async Task<long> RunHistoricalBacktestAsync(long userBotId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var userBot = await _tradingBotService.GetUserBotByIdAsync(userBotId);
                if (userBot == null)
                {
                    Console.WriteLine($"UserBot {userBotId} not found");
                    return 0;
                }

                var configJson = await _tradingBotService.GetBotConfigurationAsync(userBotId);
                if (configJson == null)
                {
                    Console.WriteLine($"Config for UserBot {userBotId} not found");
                    return 0;
                }

                // Convert JSON config to legacy format for existing code compatibility
                var config = LegacyBotConfiguration.FromJson(_botConfigService, configJson, userBot.TradingBot?.Strategy ?? "DCA");

                // For historical backtests, use a large initial balance
                // The actual budget limit (if any) is controlled by config.InvestmentAmount
                var initialBalance = 1000000m; // $1M virtual balance
                
                // Create backtest session
                var sessionId = await _tradingBotService.CreateTradingSessionAsync(userBotId, "Historical Backtest", initialBalance);
                Console.WriteLine($"Created backtest session {sessionId} for period {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                
                if (config.InvestmentAmount > 0)
                {
                    Console.WriteLine($"Budget limit: ${config.InvestmentAmount:N2} (will stop when reached)");
                }
                else
                {
                    Console.WriteLine($"Budget limit: Unlimited (will trade for entire period)");
                }
                
                // Parse DCA frequency from parameters
                var dcaFrequency = GetDCAFrequencyFromConfig(configJson);
                var currentDate = startDate;
                var tradesExecuted = 0;
                var daysChecked = 0;

                while (currentDate <= endDate)
                {
                    daysChecked++;
                    
                    // Check if it's time to execute DCA based on frequency and start day
                    if (ShouldExecuteDCAOnDate(currentDate, config.StartDay, dcaFrequency))
                    {
                        // Find the price for this specific date
                        var historicalPrice = await GetHistoricalPriceAsync("BTC", currentDate);
                        
                        if (historicalPrice > 0)
                        {
                            Console.WriteLine($"Executing trade on {currentDate:yyyy-MM-dd} at price ${historicalPrice}");
                            
                            // Execute trade at historical price
                            await SimulateTradeAsync(sessionId, "BTCUSDT", historicalPrice, currentDate);
                            tradesExecuted++;
                            
                            // Check if we've reached investment limit (only if budget is set)
                            if (config.InvestmentAmount > 0)
                            {
                                var trades = await _tradingBotService.GetTradesAsync(sessionId);
                                var totalInvested = trades.Sum(t => t.Value + t.Fee);
                                
                                if (totalInvested >= config.InvestmentAmount)
                                {
                                    Console.WriteLine($"Investment limit reached: ${totalInvested:F2} / ${config.InvestmentAmount:F2}");
                                    break; // Stop backtesting if budget is exhausted
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"No price data found for {currentDate:yyyy-MM-dd}");
                        }
                    }
                    
                    // Move to next day
                    currentDate = currentDate.AddDays(1);
                }
                
                Console.WriteLine($"Backtest completed: {daysChecked} days checked, {tradesExecuted} trades executed");
                
                // Update final performance metrics
                await UpdatePerformanceMetricsAsync(sessionId);
                await _tradingBotService.UpdateTradingSessionAsync(sessionId, "Completed");
                
                return sessionId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running historical backtest for user bot {userBotId}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Get historical price for a specific date
        /// </summary>
        public async Task<decimal> GetHistoricalPriceAsync(string symbol, DateTime targetDate)
        {
            try
            {
                // Find market data for the exact target date first
                var symbolName = symbol.Replace("USDT", "");
                var targetDateOnly = targetDate.Date; // Ensure we only compare dates, not time
                
                Console.WriteLine($"Looking for {symbolName} data on {targetDateOnly:yyyy-MM-dd}");
                
                // Try to find data for the exact target date
                var exactDateData = await _context.MarketData
                    .Where(m => m.Symbol == symbolName && m.OpenTime.Date == targetDateOnly)
                    .OrderByDescending(m => m.OpenTime)
                    .FirstOrDefaultAsync();
                
                if (exactDateData != null)
                {
                    Console.WriteLine($"Found data: OpenTime={exactDateData.OpenTime:yyyy-MM-dd HH:mm:ss}, HighPrice={exactDateData.HighPrice}");
                }
                
                return exactDateData?.HighPrice ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting historical price for {symbol} on {targetDate:yyyy-MM-dd}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Get the available data range for backtesting
        /// </summary>
        public async Task<(DateTime? earliestDate, DateTime? latestDate)> GetAvailableDataRangeAsync(string symbol)
        {
            try
            {
                var symbolName = symbol.Replace("USDT", "");
                
                var dateRange = await _context.MarketData
                    .Where(m => m.Symbol == symbolName)
                    .GroupBy(m => 1)
                    .Select(g => new { 
                        EarliestDate = g.Min(m => m.OpenTime.Date),
                        LatestDate = g.Max(m => m.OpenTime.Date)
                    })
                    .FirstOrDefaultAsync();
                
                return (dateRange?.EarliestDate, dateRange?.LatestDate);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting available data range for {symbol}: {ex.Message}");
                return (null, null);
            }
        }

        #region Private Helper Methods

        private bool ShouldExecuteNow(UserBot userBot, BotConfiguration configJson, LegacyBotConfiguration config)
        {
            // For DCA strategy, check timing based on frequency
            if (userBot.TradingBot!.Strategy == "DollarCostAverage")
            {
                var dcaFrequency = GetDCAFrequencyFromConfig(configJson);
                var today = DateTime.UtcNow;
                
                return ShouldExecuteDCAOnDate(today, config.StartDay, dcaFrequency) && 
                       HasEnoughTimePassed(userBot.LastRun, dcaFrequency);
            }

            return false;
        }

        private string GetDCAFrequencyFromConfig(BotConfiguration config)
        {
            // Parse frequency from Parameters JSON, default to "Weekly"
            try
            {
                if (!string.IsNullOrEmpty(config.Parameters))
                {
                    var parameters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(config.Parameters);
                    if (parameters?.ContainsKey("frequency") == true)
                    {
                        return parameters["frequency"].ToString() ?? "Weekly";
                    }
                }
            }
            catch
            {
                // Ignore JSON parsing errors
            }
            
            return "Weekly"; // Default frequency
        }

        private bool ShouldExecuteDCAOnDate(DateTime date, int startDay, string frequency)
        {
            switch (frequency.ToLower())
            {
                case "daily":
                    return true; // Execute every day

                case "weekly":
                    return (int)date.DayOfWeek == startDay; // Execute on specific day of week

                case "monthly":
                    // Execute on the same day of month as startDay (1-31)
                    // If startDay > days in month, execute on last day of month
                    var targetDay = Math.Min(startDay + 1, DateTime.DaysInMonth(date.Year, date.Month)); // +1 because startDay is 0-based
                    return date.Day == targetDay;

                default:
                    return (int)date.DayOfWeek == startDay; // Default to weekly
            }
        }

        private bool HasEnoughTimePassed(DateTime? lastRun, string frequency)
        {
            if (lastRun == null) return true;

            var daysSinceLastRun = (DateTime.UtcNow - lastRun.Value).TotalDays;

            return frequency.ToLower() switch
            {
                "daily" => daysSinceLastRun >= 1,
                "weekly" => daysSinceLastRun >= 7,
                "monthly" => daysSinceLastRun >= 30,
                _ => daysSinceLastRun >= 7 // Default to weekly
            };
        }

        private async Task<TradingSession?> GetOrCreateActiveSessionAsync(long userBotId, decimal initialBalance)
        {
            // Check for existing active session
            var activeSession = await _context.TradingSessions
                .Where(s => s.UserBotId == userBotId && s.Status == "Running")
                .FirstOrDefaultAsync();

            if (activeSession != null)
            {
                return activeSession;
            }

            // Create new session
            var sessionId = await _tradingBotService.CreateTradingSessionAsync(userBotId, "Backtest", initialBalance);
            return await _tradingBotService.GetTradingSessionByIdAsync(sessionId);
        }

        private async Task<bool> ExecuteTradeByStrategyAsync(long sessionId, string strategy, LegacyBotConfiguration config)
        {
            switch (strategy)
            {
                case "DollarCostAverage":
                    return await ExecuteDollarCostAverageAsync(sessionId, config);
                
                // Add more strategies here in the future
                default:
                    return false;
            }
        }

        private async Task<bool> ExecuteDollarCostAverageAsync(long sessionId, LegacyBotConfiguration config)
        {
            try
            {
                // Get current BTC price
                var currentPrice = await GetCurrentPriceAsync("BTCUSDT");
                
                // Execute the DCA trade
                var success = await SimulateTradeAsync(sessionId, "BTCUSDT", currentPrice, DateTime.UtcNow);
                
                if (success)
                {
                    // Check if we've reached the investment limit
                    var trades = await _tradingBotService.GetTradesAsync(sessionId);
                    var totalInvested = trades.Sum(t => t.Value + t.Fee);
                    
                    if (totalInvested >= config.InvestmentAmount)
                    {
                        // Stop the session as we've reached the limit
                        await _tradingBotService.UpdateTradingSessionAsync(sessionId, "Completed", totalInvested);
                        
                        // Stop the user bot
                        var session = await _tradingBotService.GetTradingSessionByIdAsync(sessionId);
                        if (session != null)
                        {
                            await _tradingBotService.UpdateUserBotStatusAsync(session.UserBotId, "Completed");
                        }
                    }
                }

                return success;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}