using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services
{
    /// <summary>
    /// Data Access Layer for trading bots, user bots, trading sessions, and related operations
    /// Implements CRUD operations for all trading bot entities using Entity Framework
    /// </summary>
    public class TradingBotDAL : ITradingBotDAL
    {
        private readonly ApplicationDbContext _context;
        private readonly IBotConfigurationService _botConfigService;
        // private readonly IBotStrategyFactory _strategyFactory; // TODO: Re-enable when needed

        public TradingBotDAL(
            ApplicationDbContext context, 
            IBotConfigurationService botConfigService)
            // IBotStrategyFactory strategyFactory) // TODO: Re-enable when needed
        {
            _context = context;
            _botConfigService = botConfigService;
            // _strategyFactory = strategyFactory; // TODO: Re-enable when needed
        }

        #region TradingBot Operations

        /// <summary>
        /// Get all available trading bot templates
        /// </summary>
        public async Task<List<TradingBot>> GetAllTradingBotsAsync()
        {
            return await _context.TradingBots
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Get a specific trading bot template by ID
        /// </summary>
        public async Task<TradingBot?> GetTradingBotByIdAsync(long botId)
        {
            return await _context.TradingBots
                .FirstOrDefaultAsync(t => t.BotId == botId);
        }

        #endregion

        #region UserBot Operations

        /// <summary>
        /// Get all user bot instances for a specific user
        /// </summary>
        public async Task<List<UserBot>> GetUserBotsAsync(long userId)
        {
            return await _context.UserBots
                .Include(u => u.TradingBot)
                .Include(u => u.BotConfigurations)
                .Where(u => u.UserId == userId)
                .OrderByDescending(u => u.Created)
                .ToListAsync();
        }

        /// <summary>
        /// Get a specific user bot by ID
        /// </summary>
        public async Task<UserBot?> GetUserBotByIdAsync(long userBotId)
        {
            return await _context.UserBots
                .Include(u => u.TradingBot)
                .Include(u => u.BotConfigurations)
                .FirstOrDefaultAsync(u => u.UserBotId == userBotId);
        }

        /// <summary>
        /// Create a new user bot instance from a template
        /// </summary>
        public async Task<bool> CreateUserBotAsync(CreateUserBotViewModel model, long userId)
        {
            try
            {
                // Create the user bot
                var userBot = new UserBot
                {
                    UserId = userId,
                    BotId = model.BotId,
                    Name = model.Name,
                    Status = "Inactive",
                    Created = DateTime.UtcNow
                };

                _context.UserBots.Add(userBot);
                await _context.SaveChangesAsync();

                // Create the bot configuration with JSON parameters
                var botConfig = new BotConfiguration
                {
                    UserBotId = userBot.UserBotId,
                    Parameters = model.Parameters ?? "{}" // Use JSON parameters or empty JSON
                };

                _context.BotConfigurations.Add(botConfig);
                await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Update the status of a user bot (Active, Inactive, Paused, Stopped)
        /// </summary>
        public async Task<bool> UpdateUserBotStatusAsync(long userBotId, string status)
        {
            try
            {
                var userBot = await _context.UserBots.FindAsync(userBotId);
                if (userBot == null) return false;

                userBot.Status = status;
                if (status == "Active")
                {
                    userBot.LastRun = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Delete a user bot and all related data
        /// </summary>
        public async Task<bool> DeleteUserBotAsync(long userBotId)
        {
            try
            {
                var userBot = await _context.UserBots.FindAsync(userBotId);
                if (userBot == null) return false;

                _context.UserBots.Remove(userBot);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region BotConfiguration Operations

        /// <summary>
        /// Get bot configuration for a user bot
        /// </summary>
        public async Task<BotConfiguration?> GetBotConfigurationAsync(long userBotId)
        {
            return await _context.BotConfigurations
                .FirstOrDefaultAsync(c => c.UserBotId == userBotId);
        }

        /// <summary>
        /// Update bot configuration settings
        /// </summary>
        public async Task<bool> UpdateBotConfigurationAsync(long userBotId, CreateUserBotViewModel model)
        {
            try
            {
                var config = await _context.BotConfigurations
                    .FirstOrDefaultAsync(c => c.UserBotId == userBotId);

                if (config == null) return false;

                config.Parameters = model.Parameters ?? "{}"; // Update only JSON parameters

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region TradingSession Operations

        /// <summary>
        /// Get all trading sessions for a user
        /// </summary>
        public async Task<List<TradingSession>> GetTradingSessionsAsync(long userId)
        {
            return await _context.TradingSessions
                .Include(s => s.UserBot)
                .Include(s => s.PerformanceMetrics)
                .Where(s => s.UserBot.UserId == userId)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }

        /// <summary>
        /// Get trading sessions for a specific user bot
        /// </summary>
        public async Task<List<TradingSession>> GetTradingSessionsByUserBotAsync(long userBotId)
        {
            return await _context.TradingSessions
                .Include(s => s.PerformanceMetrics)
                .Where(s => s.UserBotId == userBotId)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }

        /// <summary>
        /// Get a specific trading session by ID
        /// </summary>
        public async Task<TradingSession?> GetTradingSessionByIdAsync(long sessionId)
        {
            return await _context.TradingSessions
                .Include(s => s.UserBot)
                .Include(s => s.Trades)
                .Include(s => s.PerformanceMetrics)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
        }

        /// <summary>
        /// Create a new trading session
        /// </summary>
        public async Task<long> CreateTradingSessionAsync(long userBotId, string mode, decimal initialBalance)
        {
            try
            {
                var session = new TradingSession
                {
                    UserBotId = userBotId,
                    StartTime = DateTime.UtcNow,
                    Mode = mode,
                    InitialBalance = initialBalance,
                    Status = "Running"
                };

                _context.TradingSessions.Add(session);
                await _context.SaveChangesAsync();

                return session.SessionId;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Update trading session status and final balance
        /// </summary>
        public async Task<bool> UpdateTradingSessionAsync(long sessionId, string status, decimal? finalBalance = null)
        {
            try
            {
                var session = await _context.TradingSessions.FindAsync(sessionId);
                if (session == null) return false;

                session.Status = status;
                if (finalBalance.HasValue)
                {
                    session.FinalBalance = finalBalance.Value;
                }
                if (status == "Completed" || status == "Stopped")
                {
                    session.EndTime = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Trade Operations

        /// <summary>
        /// Get all trades for a trading session
        /// </summary>
        public async Task<List<Trade>> GetTradesAsync(long sessionId)
        {
            return await _context.Trades
                .Where(t => t.SessionId == sessionId)
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Create a new trade record
        /// </summary>
        public async Task<bool> CreateTradeAsync(long sessionId, string symbol, string side, decimal price, decimal quantity, decimal fee = 0, DateTime? timestamp = null)
        {
            try
            {
                var trade = new Trade
                {
                    SessionId = sessionId,
                    Symbol = symbol,
                    Side = side,
                    Price = price,
                    Quantity = quantity,
                    Value = price * quantity,
                    Fee = fee,
                    Timestamp = timestamp ?? DateTime.UtcNow
                };

                _context.Trades.Add(trade);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region PerformanceMetrics Operations

        /// <summary>
        /// Get performance metrics for a trading session
        /// </summary>
        public async Task<PerformanceMetric?> GetPerformanceMetricsAsync(long sessionId)
        {
            return await _context.PerformanceMetrics
                .FirstOrDefaultAsync(p => p.SessionId == sessionId);
        }

        /// <summary>
        /// Update or create performance metrics for a session
        /// </summary>
        public async Task<bool> UpdatePerformanceMetricsAsync(long sessionId, decimal totalInvested, decimal totalValue, int totalTrades, decimal? winRate = null)
        {
            try
            {
                var metrics = await _context.PerformanceMetrics
                    .FirstOrDefaultAsync(p => p.SessionId == sessionId);

                if (metrics == null)
                {
                    // Create new metrics
                    metrics = new PerformanceMetric
                    {
                        SessionId = sessionId,
                        TotalInvested = totalInvested,
                        TotalValue = totalValue,
                        ROI = totalInvested > 0 ? ((totalValue - totalInvested) / totalInvested) * 100 : 0,
                        TotalTrades = totalTrades,
                        WinRate = winRate,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.PerformanceMetrics.Add(metrics);
                }
                else
                {
                    // Update existing metrics
                    metrics.TotalInvested = totalInvested;
                    metrics.TotalValue = totalValue;
                    metrics.ROI = totalInvested > 0 ? ((totalValue - totalInvested) / totalInvested) * 100 : 0;
                    metrics.TotalTrades = totalTrades;
                    metrics.WinRate = winRate;
                    metrics.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Trading Bot Control Operations

        /// <summary>
        /// Start a user bot (set status to Active and create a new trading session)
        /// </summary>
        public async Task<bool> StartUserBotAsync(long userBotId)
        {
            try
            {
                var userBot = await GetUserBotByIdAsync(userBotId);
                if (userBot == null) return false;

                var config = await GetBotConfigurationAsync(userBotId);
                if (config == null) return false;

                // Update user bot status
                await UpdateUserBotStatusAsync(userBotId, "Active");

                // Create a new trading session with default initial balance
                // TODO: Extract initial balance from JSON configuration parameters
                decimal initialBalance = 1000.00m; // Default initial balance
                if (!string.IsNullOrEmpty(config.Parameters))
                {
                    try
                    {
                        // Try to parse as DCA configuration to get investment amount
                        var dcaConfig = _botConfigService.DeserializeConfiguration<DcaBotConfiguration>(config.Parameters);
                        if (dcaConfig?.MaxTotalInvestment.HasValue == true)
                        {
                            initialBalance = dcaConfig.MaxTotalInvestment.Value;
                        }
                        else if (dcaConfig?.InvestmentAmount > 0)
                        {
                            // Use 52 weeks worth of DCA investments as initial balance
                            initialBalance = dcaConfig.InvestmentAmount * 52;
                        }
                    }
                    catch
                    {
                        // Use default if parsing fails
                    }
                }
                
                await CreateTradingSessionAsync(userBotId, "Backtest", initialBalance);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Stop a user bot (set status to Stopped)
        /// </summary>
        public async Task<bool> StopUserBotAsync(long userBotId)
        {
            try
            {
                // Update user bot status
                await UpdateUserBotStatusAsync(userBotId, "Stopped");

                // Find and stop any running sessions
                var runningSessions = await _context.TradingSessions
                    .Where(s => s.UserBotId == userBotId && s.Status == "Running")
                    .ToListAsync();

                foreach (var session in runningSessions)
                {
                    await UpdateTradingSessionAsync(session.SessionId, "Stopped", session.InitialBalance);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Pause a user bot (set status to Paused)
        /// </summary>
        public async Task<bool> PauseUserBotAsync(long userBotId)
        {
            return await UpdateUserBotStatusAsync(userBotId, "Paused");
        }

        #endregion
    }
}