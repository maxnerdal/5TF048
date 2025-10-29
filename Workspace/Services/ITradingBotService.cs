using WebApp.Models;

namespace WebApp.Services
{
    public interface ITradingBotService
    {
        Task<bool> ExecuteBotStrategyAsync(long userBotId, DateTime? executionTime = null);
        Task<List<MarketData>> GetRecentMarketDataAsync(string symbol, int count = 100);
        Task SetNextExecutionTimeAsync(long userBotId);
    }
}