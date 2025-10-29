using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services
{
    public class TradingBotService : ITradingBotService
    {
        private readonly ApplicationDbContext _context;
        private readonly IBotConfigurationService _botConfigService;
        private readonly ITradingBotDAL _tradingBotDAL;
        private readonly IBtcPriceService _btcPriceService;

        public TradingBotService(
            ApplicationDbContext context,
            IBotConfigurationService botConfigService,
            ITradingBotDAL tradingBotDAL,
            IBtcPriceService btcPriceService)
        {
            _context = context;
            _botConfigService = botConfigService;
            _tradingBotDAL = tradingBotDAL;
            _btcPriceService = btcPriceService;
        }

        public async Task<bool> ExecuteBotStrategyAsync(long userBotId, DateTime? executionTime = null)
        {
            try
            {
                var userBot = await _tradingBotDAL.GetUserBotByIdAsync(userBotId);
                if (userBot == null || userBot.Status != "Active") 
                    return false;

                var config = await _tradingBotDAL.GetBotConfigurationAsync(userBotId);
                if (config == null || string.IsNullOrEmpty(config.Parameters)) 
                    return false;

                var strategy = userBot.TradingBot.Strategy;

                if (strategy != "DCA")
                {
                    return false;
                }

                var botConfig = _botConfigService.DeserializeConfiguration<DcaBotConfiguration>(config.Parameters);
                if (botConfig == null) return false;

                var targetAsset = botConfig.TargetAsset;
                var marketData = await GetRecentMarketDataAsync(targetAsset);

                var activeSessions = await _context.TradingSessions
                    .Where(s => s.UserBotId == userBotId && s.Status == "Running")
                    .ToListAsync();

                var sessionId = activeSessions.FirstOrDefault()?.SessionId ?? 0;
                if (sessionId == 0)
                {
                    sessionId = await _tradingBotDAL.CreateTradingSessionAsync(userBotId, "Paper Trade", 1000m);
                }

                var existingTrades = await _tradingBotDAL.GetTradesAsync(sessionId);

                var result = await ExecuteDcaStrategyAsync(userBotId, sessionId, botConfig, marketData, existingTrades, executionTime ?? DateTime.UtcNow);

                if (result)
                {
                    var currentTime = DateTime.UtcNow;
                    userBot.LastRun = currentTime;
                    userBot.NextExecutionTime = CalculateNextExecutionTime(botConfig, currentTime);
                    await _context.SaveChangesAsync();
                    
                    // Check budget and auto-pause if needed
                    await CheckBudgetAndPauseAsync(userBotId, sessionId, botConfig);
                }

                return result;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<MarketData>> GetRecentMarketDataAsync(string symbol, int count = 100)
        {
            return await _context.MarketData
                .Where(md => md.Symbol == symbol)
                .OrderByDescending(md => md.OpenTime)
                .Take(count)
                .ToListAsync();
        }

        private async Task<bool> ExecuteDcaStrategyAsync(long userBotId, long sessionId, DcaBotConfiguration config, List<MarketData> marketData, List<Trade> existingTrades, DateTime executionTime)
        {
            try
            {
                // Get LIVE price from CoinGecko API
                var btcPriceModel = await _btcPriceService.GetBtcPriceAsync();
                var livePrice = btcPriceModel.Price;
                
                if (livePrice <= 0)
                {
                    // Fallback to latest MarketData if API fails
                    livePrice = marketData.OrderByDescending(md => md.OpenTime).FirstOrDefault()?.ClosePrice ?? 0;
                }
                
                if (livePrice <= 0)
                {
                    return false;
                }

                var tradeAmount = config.InvestmentAmount;
                var quantity = tradeAmount / livePrice;
                var fee = tradeAmount * 0.001m;

                var success = await _tradingBotDAL.CreateTradeAsync(
                    sessionId,
                    config.TargetAsset,
                    "BUY",
                    livePrice,
                    quantity,
                    fee,
                    executionTime
                );

                // Update performance metrics after trade
                if (success)
                {
                    var trades = await _tradingBotDAL.GetTradesAsync(sessionId);
                    var totalInvested = trades.Sum(t => t.Value + t.Fee);
                    var totalQuantity = trades.Sum(t => t.Quantity);
                    var totalValue = totalQuantity * livePrice;
                    
                    await _tradingBotDAL.UpdatePerformanceMetricsAsync(
                        sessionId,
                        totalInvested,
                        totalValue,
                        trades.Count,
                        null // winRate can be calculated later if needed
                    );
                }

                return success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private DateTime CalculateNextExecutionTime(DcaBotConfiguration config, DateTime fromTime)
        {
            // Adjust based on frequency
            switch (config.Frequency)
            {
                case DcaFrequency.Daily:
                    var dailyExecution = new DateTime(
                        fromTime.Year, fromTime.Month, fromTime.Day,
                        config.ExecutionHour, config.ExecutionMinute, 0, DateTimeKind.Utc);
                    
                    // If the execution time today has already passed, move to tomorrow
                    if (dailyExecution <= fromTime)
                    {
                        dailyExecution = dailyExecution.AddDays(1);
                    }
                    return dailyExecution;

                case DcaFrequency.Weekly:
                    // Start from today at the execution time
                    var weeklyExecution = new DateTime(
                        fromTime.Year, fromTime.Month, fromTime.Day,
                        config.ExecutionHour, config.ExecutionMinute, 0, DateTimeKind.Utc);
                    
                    // Find next occurrence of the target day of week
                    if (config.DayOfWeek.HasValue)
                    {
                        // If today is the target day and time hasn't passed yet, use today
                        if (weeklyExecution.DayOfWeek == config.DayOfWeek.Value && weeklyExecution > fromTime)
                        {
                            return weeklyExecution;
                        }
                        
                        // Otherwise, find next occurrence of target day
                        weeklyExecution = weeklyExecution.AddDays(1);
                        while (weeklyExecution.DayOfWeek != config.DayOfWeek.Value)
                        {
                            weeklyExecution = weeklyExecution.AddDays(1);
                        }
                    }
                    return weeklyExecution;

                case DcaFrequency.Monthly:
                    var targetDay = Math.Min(config.DayOfMonth ?? 1, DateTime.DaysInMonth(fromTime.Year, fromTime.Month));
                    
                    var monthlyExecution = new DateTime(
                        fromTime.Year, fromTime.Month, targetDay,
                        config.ExecutionHour, config.ExecutionMinute, 0, DateTimeKind.Utc);
                    
                    // If this month's execution has passed, move to next month
                    if (monthlyExecution <= fromTime)
                    {
                        monthlyExecution = monthlyExecution.AddMonths(1);
                        targetDay = Math.Min(config.DayOfMonth ?? 1, DateTime.DaysInMonth(monthlyExecution.Year, monthlyExecution.Month));
                        monthlyExecution = new DateTime(
                            monthlyExecution.Year, monthlyExecution.Month, targetDay,
                            config.ExecutionHour, config.ExecutionMinute, 0, DateTimeKind.Utc);
                    }
                    
                    return monthlyExecution;

                default:
                    // Default to daily
                    var defaultExecution = new DateTime(
                        fromTime.Year, fromTime.Month, fromTime.Day,
                        config.ExecutionHour, config.ExecutionMinute, 0, DateTimeKind.Utc);
                    
                    if (defaultExecution <= fromTime)
                    {
                        defaultExecution = defaultExecution.AddDays(1);
                    }
                    return defaultExecution;
            }
        }

        public async Task SetNextExecutionTimeAsync(long userBotId)
        {
            var userBot = await _tradingBotDAL.GetUserBotByIdAsync(userBotId);
            if (userBot == null) return;

            var config = await _tradingBotDAL.GetBotConfigurationAsync(userBotId);
            if (config == null || string.IsNullOrEmpty(config.Parameters)) return;

            var botConfig = _botConfigService.DeserializeConfiguration<DcaBotConfiguration>(config.Parameters);
            if (botConfig == null) return;

            userBot.NextExecutionTime = CalculateNextExecutionTime(botConfig, DateTime.UtcNow);
            await _context.SaveChangesAsync();
        }

        private async Task CheckBudgetAndPauseAsync(long userBotId, long sessionId, DcaBotConfiguration config)
        {
            if (!config.MaxTotalInvestment.HasValue)
            {
                return; // No budget limit
            }

            var trades = await _tradingBotDAL.GetTradesAsync(sessionId);
            var totalInvested = trades.Sum(t => t.Value + t.Fee);

            if (totalInvested >= config.MaxTotalInvestment.Value)
            {
                await _tradingBotDAL.PauseUserBotAsync(userBotId);
                await _tradingBotDAL.UpdateTradingSessionAsync(sessionId, "Completed", totalInvested);
            }
        }
    }
}