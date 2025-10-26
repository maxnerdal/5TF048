using WebApp.Models;

namespace WebApp.Services
{
    /// <summary>
    /// Factory service to create strategy executors for different bot types
    /// This allows one TradingBotService to handle all bot strategies
    /// </summary>
    public interface IBotStrategyFactory
    {
        IBotStrategyExecutor CreateExecutor(string strategy);
        List<string> GetSupportedStrategies();
    }

    /// <summary>
    /// Interface for executing specific bot strategies
    /// Each bot type (DCA, Grid, Momentum) implements this interface
    /// </summary>
    public interface IBotStrategyExecutor
    {
        string StrategyName { get; }
        
        /// <summary>
        /// Execute one iteration of the bot strategy
        /// </summary>
        Task<BotExecutionResult> ExecuteAsync(BotExecutionContext context);
        
        /// <summary>
        /// Validate the bot configuration for this strategy
        /// </summary>
        bool ValidateConfiguration(string jsonParameters, out List<string> errors);
        
        /// <summary>
        /// Get strategy-specific performance metrics
        /// </summary>
        Dictionary<string, object> GetPerformanceMetrics(List<Trade> trades, BaseBotConfiguration config);
    }

    /// <summary>
    /// Context passed to bot strategy executors
    /// </summary>
    public class BotExecutionContext
    {
        public long UserBotId { get; set; }
        public long SessionId { get; set; }
        public BaseBotConfiguration Configuration { get; set; } = new DcaBotConfiguration();
        public List<MarketData> MarketData { get; set; } = new();
        public List<Trade> ExistingTrades { get; set; } = new();
        public decimal CurrentBalance { get; set; }
        public DateTime ExecutionTime { get; set; } = DateTime.UtcNow;
        public bool IsBacktest { get; set; }
    }

    /// <summary>
    /// Result from bot strategy execution
    /// </summary>
    public class BotExecutionResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<TradeAction> TradeActions { get; set; } = new();
        public Dictionary<string, object> Metrics { get; set; } = new();
        public DateTime NextExecutionTime { get; set; }
    }

    /// <summary>
    /// Trade action to be executed
    /// </summary>
    public class TradeAction
    {
        public string Symbol { get; set; } = string.Empty;
        public string Side { get; set; } = string.Empty; // BUY or SELL
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}

namespace WebApp.Services.BotStrategies
{
    /// <summary>
    /// DCA (Dollar Cost Average) strategy executor
    /// Implements the IBotStrategyExecutor interface for DCA logic
    /// </summary>
    public class DcaStrategyExecutor : IBotStrategyExecutor
    {
        private readonly IBotConfigurationService _configService;

        public string StrategyName => "DCA";

        public DcaStrategyExecutor(IBotConfigurationService configService)
        {
            _configService = configService;
        }

        public async Task<BotExecutionResult> ExecuteAsync(BotExecutionContext context)
        {
            var dcaConfig = (DcaBotConfiguration)context.Configuration;
            var result = new BotExecutionResult();

            try
            {
                // Check if it's time to execute based on DCA schedule
                if (!ShouldExecuteNow(dcaConfig, context.ExecutionTime))
                {
                    result.Success = true;
                    result.NextExecutionTime = dcaConfig.GetNextExecutionTime();
                    return result;
                }

                // Check if we've reached maximum investment limit
                var totalInvested = context.ExistingTrades
                    .Where(t => t.Side == "BUY")
                    .Sum(t => t.Value);

                if (dcaConfig.MaxTotalInvestment.HasValue && 
                    totalInvested + dcaConfig.InvestmentAmount > dcaConfig.MaxTotalInvestment.Value)
                {
                    result.Success = true;
                    result.ErrorMessage = "Maximum investment limit reached";
                    return result;
                }

                // Get current market price
                var currentPrice = GetCurrentPrice(context.MarketData, dcaConfig.TargetAsset);
                if (currentPrice <= 0)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Unable to get current price for {dcaConfig.TargetAsset}";
                    return result;
                }

                // Calculate quantity to buy
                var quantity = dcaConfig.InvestmentAmount / currentPrice;

                // Create buy order
                result.TradeActions.Add(new TradeAction
                {
                    Symbol = dcaConfig.TargetAsset,
                    Side = "BUY",
                    Quantity = quantity,
                    Price = currentPrice,
                    Reason = $"DCA purchase: {dcaConfig.Currency} {dcaConfig.InvestmentAmount}"
                });

                result.Success = true;
                result.NextExecutionTime = dcaConfig.GetNextExecutionTime();
                result.Metrics["InvestmentAmount"] = dcaConfig.InvestmentAmount;
                result.Metrics["Price"] = currentPrice;
                result.Metrics["Quantity"] = quantity;

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        public bool ValidateConfiguration(string jsonParameters, out List<string> errors)
        {
            errors = new List<string>();
            
            var dcaConfig = _configService.DeserializeConfiguration<DcaBotConfiguration>(jsonParameters);
            if (dcaConfig == null)
            {
                errors.Add("Invalid DCA configuration JSON");
                return false;
            }

            return dcaConfig.IsValid(out errors);
        }

        public Dictionary<string, object> GetPerformanceMetrics(List<Trade> trades, BaseBotConfiguration config)
        {
            var dcaConfig = (DcaBotConfiguration)config;
            var metrics = new Dictionary<string, object>();

            var buyTrades = trades.Where(t => t.Side == "BUY").ToList();
            
            metrics["TotalPurchases"] = buyTrades.Count;
            metrics["TotalInvested"] = buyTrades.Sum(t => t.Value);
            metrics["AveragePrice"] = buyTrades.Any() ? buyTrades.Average(t => t.Price) : 0;
            metrics["TotalQuantity"] = buyTrades.Sum(t => t.Quantity);
            
            if (buyTrades.Any())
            {
                var avgPrice = (decimal)metrics["AveragePrice"];
                var totalQuantity = (decimal)metrics["TotalQuantity"];
                // You would get current price here for unrealized P&L calculation
                // metrics["UnrealizedPnL"] = (currentPrice - avgPrice) * totalQuantity;
            }

            return metrics;
        }

        private bool ShouldExecuteNow(DcaBotConfiguration config, DateTime executionTime)
        {
            var nextExecution = config.GetNextExecutionTime();
            return executionTime >= nextExecution.AddMinutes(-5); // 5-minute tolerance
        }

        private decimal GetCurrentPrice(List<MarketData> marketData, string symbol)
        {
            var latest = marketData
                .Where(md => md.Symbol == symbol)
                .OrderByDescending(md => md.OpenTime)
                .FirstOrDefault();

            return latest?.Close ?? 0;
        }
    }

    /// <summary>
    /// Grid trading strategy executor (placeholder for future implementation)
    /// </summary>
    public class GridStrategyExecutor : IBotStrategyExecutor
    {
        public string StrategyName => "GRID";

        public async Task<BotExecutionResult> ExecuteAsync(BotExecutionContext context)
        {
            // TODO: Implement grid trading logic
            await Task.CompletedTask;
            return new BotExecutionResult 
            { 
                Success = false, 
                ErrorMessage = "Grid strategy not yet implemented" 
            };
        }

        public bool ValidateConfiguration(string jsonParameters, out List<string> errors)
        {
            errors = new List<string> { "Grid strategy not yet implemented" };
            return false;
        }

        public Dictionary<string, object> GetPerformanceMetrics(List<Trade> trades, BaseBotConfiguration config)
        {
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Factory implementation for creating bot strategy executors
    /// </summary>
    public class BotStrategyFactory : IBotStrategyFactory
    {
        private readonly IBotConfigurationService _configService;
        private readonly Dictionary<string, Func<IBotStrategyExecutor>> _executors;

        public BotStrategyFactory(IBotConfigurationService configService)
        {
            _configService = configService;
            _executors = new Dictionary<string, Func<IBotStrategyExecutor>>
            {
                { "DCA", () => new DcaStrategyExecutor(_configService) },
                { "GRID", () => new GridStrategyExecutor() },
                // Add more strategies here as they're implemented
                // { "MOMENTUM", () => new MomentumStrategyExecutor(_configService) },
                // { "ARBITRAGE", () => new ArbitrageStrategyExecutor(_configService) }
            };
        }

        public IBotStrategyExecutor CreateExecutor(string strategy)
        {
            if (_executors.TryGetValue(strategy.ToUpper(), out var factory))
            {
                return factory();
            }
            
            throw new NotSupportedException($"Strategy '{strategy}' is not supported");
        }

        public List<string> GetSupportedStrategies()
        {
            return _executors.Keys.ToList();
        }
    }
}