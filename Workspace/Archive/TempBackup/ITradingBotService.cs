using WebApp.Models;

namespace WebApp.Services
{
    /// <summary>
    /// Interface for trading bot service operations
    /// Defines all CRUD operations for trading bot entities
    /// </summary>
    public interface ITradingBotService
    {
        // TradingBot operations
        Task<List<TradingBot>> GetAllTradingBotsAsync();
        Task<TradingBot?> GetTradingBotByIdAsync(long botId);

        // UserBot operations
        Task<List<UserBot>> GetUserBotsAsync(long userId);
        Task<UserBot?> GetUserBotByIdAsync(long userBotId);
        Task<bool> CreateUserBotAsync(CreateUserBotViewModel model, long userId);
        Task<bool> UpdateUserBotStatusAsync(long userBotId, string status);
        Task<bool> DeleteUserBotAsync(long userBotId);

        // BotConfiguration operations
        Task<BotConfiguration?> GetBotConfigurationAsync(long userBotId);
        Task<bool> UpdateBotConfigurationAsync(long userBotId, CreateUserBotViewModel model);

        // TradingSession operations
        Task<List<TradingSession>> GetTradingSessionsAsync(long userId);
        Task<List<TradingSession>> GetTradingSessionsByUserBotAsync(long userBotId);
        Task<TradingSession?> GetTradingSessionByIdAsync(long sessionId);
        Task<long> CreateTradingSessionAsync(long userBotId, string mode, decimal initialBalance);
        Task<bool> UpdateTradingSessionAsync(long sessionId, string status, decimal? finalBalance = null);

        // Trade operations
        Task<List<Trade>> GetTradesAsync(long sessionId);
        Task<bool> CreateTradeAsync(long sessionId, string symbol, string side, decimal price, decimal quantity, decimal fee = 0, DateTime? timestamp = null);

        // PerformanceMetrics operations
        Task<PerformanceMetric?> GetPerformanceMetricsAsync(long sessionId);
        Task<bool> UpdatePerformanceMetricsAsync(long sessionId, decimal totalInvested, decimal totalValue, int totalTrades, decimal? winRate = null);

        // Trading bot control operations
        Task<bool> StartUserBotAsync(long userBotId);
        Task<bool> StopUserBotAsync(long userBotId);
        Task<bool> PauseUserBotAsync(long userBotId);
        
        // Universal bot execution
        Task<bool> ExecuteBotStrategyAsync(long userBotId, DateTime? executionTime = null);
    }
}