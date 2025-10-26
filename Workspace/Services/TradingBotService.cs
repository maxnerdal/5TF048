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

        public TradingBotService(
            ApplicationDbContext context,
            IBotConfigurationService botConfigService,
            ITradingBotDAL tradingBotDAL)
        {
            _context = context;
            _botConfigService = botConfigService;
            _tradingBotDAL = tradingBotDAL;
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
                    sessionId = await _tradingBotDAL.CreateTradingSessionAsync(userBotId, "Live", 1000m);
                }

                var existingTrades = await _tradingBotDAL.GetTradesAsync(sessionId);

                var result = await ExecuteDcaStrategyAsync(userBotId, sessionId, botConfig, marketData, existingTrades, executionTime ?? DateTime.UtcNow);

                if (result)
                {
                    userBot.LastRun = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
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
                var latestPrice = marketData.OrderByDescending(md => md.OpenTime).FirstOrDefault()?.ClosePrice ?? 0;
                if (latestPrice <= 0)
                {
                    return false;
                }

                var tradeAmount = config.InvestmentAmount;
                var quantity = tradeAmount / latestPrice;
                var fee = tradeAmount * 0.001m;

                var success = await _tradingBotDAL.CreateTradeAsync(
                    sessionId,
                    config.TargetAsset,
                    "BUY",
                    latestPrice,
                    quantity,
                    fee,
                    executionTime
                );

                return success;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}