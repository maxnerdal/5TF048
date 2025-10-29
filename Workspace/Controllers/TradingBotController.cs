using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Services;
using System.Security.Claims;

namespace WebApp.Controllers
{
    /// <summary>
    /// Controller for managing trading bots, user bot instances, and trading sessions
    /// Handles all UI interactions for the trading bot functionality
    /// </summary>
    [Authorize]
    public class TradingBotController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITradingBotDAL _tradingBotService;
        private readonly IBacktestingService _backtestingService;
        private readonly IBotConfigurationService _botConfigService;
        private readonly ITradingBotService _tradingBotExecutionService;

        public TradingBotController(
            ApplicationDbContext context,
            ITradingBotDAL tradingBotService,
            IBacktestingService backtestingService,
            IBotConfigurationService botConfigService,
            ITradingBotService tradingBotExecutionService)
        {
            _context = context;
            _tradingBotService = tradingBotService;
            _backtestingService = backtestingService;
            _botConfigService = botConfigService;
            _tradingBotExecutionService = tradingBotExecutionService;
        }

        /// <summary>
        /// Main trading bots page showing available bots, user bots, and trading sessions
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new TradingBotsPageViewModel();

            try
            {
                // Get available trading bot templates
                var availableBots = await _tradingBotService.GetAllTradingBotsAsync();
                viewModel.AvailableBots = availableBots.Select(bot => new TradingBotViewModel
                {
                    BotId = bot.BotId,
                    Name = bot.Name,
                    Strategy = bot.Strategy,
                    Description = bot.Description,
                    Created = bot.Created
                }).ToList();

                // Get user's bot instances with configurations
                var userBots = await _tradingBotService.GetUserBotsAsync(userId);
                viewModel.UserBots = userBots.Select(userBot => new UserBotViewModel
                {
                    UserBotId = userBot.UserBotId,
                    Name = userBot.Name,
                    Status = userBot.Status,
                    TradingBotName = userBot.TradingBot?.Name ?? "Unknown",
                    Strategy = userBot.TradingBot?.Strategy ?? "Unknown",
                    Created = userBot.Created,
                    LastRun = userBot.LastRun,
                    WeeklyBuyAmount = GetWeeklyBuyAmountFromJson(userBot),
                    InvestmentAmount = GetInvestmentAmountFromJson(userBot),
                    RiskLevel = GetRiskLevelFromJson(userBot)
                }).ToList();

                // Get available data range for backtesting
                var dataRange = await _backtestingService.GetAvailableDataRangeAsync("BTC");
                ViewBag.EarliestDate = dataRange.earliestDate?.ToString("yyyy-MM-dd") ?? "No data";
                ViewBag.LatestDate = dataRange.latestDate?.ToString("yyyy-MM-dd") ?? "No data";

                // Get recent trading sessions
                var tradingSessions = await _tradingBotService.GetTradingSessionsAsync(userId);
                viewModel.TradingSessions = tradingSessions.Take(10).Select(session => new TradingSessionViewModel
                {
                    SessionId = session.SessionId,
                    UserBotName = session.UserBot?.Name ?? "Unknown",
                    StartTime = session.StartTime,
                    EndTime = session.EndTime,
                    Mode = session.Mode,
                    InitialBalance = session.InitialBalance,
                    FinalBalance = session.FinalBalance,
                    Status = session.Status,
                    TotalTrades = session.PerformanceMetrics?.FirstOrDefault()?.TotalTrades ?? 0,
                    ROI = session.PerformanceMetrics?.FirstOrDefault()?.ROI
                }).ToList();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading trading bots: {ex.Message}";
            }

            return View(viewModel);
        }

        /// <summary>
        /// Show form to create a new trading bot
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            var availableAssets = await _context.DigitalAssets
                .OrderBy(da => da.Symbol)
                .ToListAsync();

            var defaultConfig = await _botConfigService.CreateDefaultDcaConfigurationAsync();

            var viewModel = new DcaBotConfigurationViewModel
            {
                Configuration = defaultConfig,
                AvailableAssets = availableAssets
            };

            return View(viewModel);
        }

        /// <summary>
        /// Process creation of a new trading bot
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DcaBotConfigurationViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            // Validate the configuration
            if (!_botConfigService.ValidateConfiguration(model.Configuration, out var errors))
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError("Configuration", error);
                }
            }

            if (!ModelState.IsValid)
            {
                model.AvailableAssets = await _context.DigitalAssets.OrderBy(da => da.Symbol).ToListAsync();
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get the DCA bot template (or any trading bot template)
                var botTemplate = await _context.TradingBots
                    .FirstOrDefaultAsync(tb => tb.Strategy == "DCA");

                if (botTemplate == null)
                {
                    ModelState.AddModelError("", "Trading bot template not found. Please run the database migration.");
                    model.AvailableAssets = await _context.DigitalAssets.OrderBy(da => da.Symbol).ToListAsync();
                    return View(model);
                }

                // Create the UserBot
                var userBot = new UserBot
                {
                    UserId = userId,
                    BotId = botTemplate.BotId,
                    Name = model.BotName,
                    Status = model.Configuration.AutoStart ? "Active" : "Inactive",
                    Created = DateTime.UtcNow
                };

                _context.UserBots.Add(userBot);
                await _context.SaveChangesAsync();

                // Serialize and save the configuration
                var configJson = _botConfigService.SerializeConfiguration(model.Configuration);
                var botConfig = new BotConfiguration
                {
                    UserBotId = userBot.UserBotId,
                    Parameters = configJson
                };

                _context.BotConfigurations.Add(botConfig);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Trading bot '{model.BotName}' created successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", $"Error creating bot: {ex.Message}");
                model.AvailableAssets = await _context.DigitalAssets.OrderBy(da => da.Symbol).ToListAsync();
                return View(model);
            }
        }

        /// <summary>
        /// Show form to edit an existing trading bot
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var userBot = await _context.UserBots
                    .Include(ub => ub.BotConfigurations)
                    .FirstOrDefaultAsync(ub => ub.UserBotId == id && ub.UserId == userId);

                if (userBot == null)
                {
                    return NotFound();
                }

                var config = userBot.BotConfigurations.FirstOrDefault();
                var dcaConfig = config != null
                    ? _botConfigService.DeserializeConfiguration<DcaBotConfiguration>(config.Parameters ?? "")
                    : await _botConfigService.CreateDefaultDcaConfigurationAsync();

                var availableAssets = await _context.DigitalAssets
                    .OrderBy(da => da.Symbol)
                    .ToListAsync();

                var viewModel = new DcaBotConfigurationViewModel
                {
                    UserBotId = userBot.UserBotId,
                    BotName = userBot.Name,
                    Configuration = dcaConfig ?? new DcaBotConfiguration(),
                    AvailableAssets = availableAssets
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading bot: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Process editing of an existing trading bot
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, DcaBotConfigurationViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var userBot = await _context.UserBots
                    .Include(ub => ub.BotConfigurations)
                    .FirstOrDefaultAsync(ub => ub.UserBotId == id && ub.UserId == userId);

                if (userBot == null)
                {
                    return NotFound();
                }

                // Validate the configuration
                if (!_botConfigService.ValidateConfiguration(model.Configuration, out var errors))
                {
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError("Configuration", error);
                    }
                }

                if (!ModelState.IsValid)
                {
                    model.AvailableAssets = await _context.DigitalAssets.OrderBy(da => da.Symbol).ToListAsync();
                    return View(model);
                }

                // Update the user bot name
                userBot.Name = model.BotName;

                // Update bot status based on AutoStart
                if (model.Configuration.AutoStart && userBot.Status != "Active")
                {
                    userBot.Status = "Active";
                }

                // Update or create configuration
                var existingConfig = userBot.BotConfigurations.FirstOrDefault();
                var configJson = _botConfigService.SerializeConfiguration(model.Configuration);

                if (existingConfig != null)
                {
                    existingConfig.Parameters = configJson;
                }
                else
                {
                    var newConfig = new BotConfiguration
                    {
                        UserBotId = userBot.UserBotId,
                        Parameters = configJson
                    };
                    _context.BotConfigurations.Add(newConfig);
                }

                await _context.SaveChangesAsync();

                // Set NextExecutionTime for active bots (especially DCA bots)
                if (userBot.Status == "Active")
                {
                    await _tradingBotExecutionService.SetNextExecutionTimeAsync(userBot.UserBotId);
                }

                TempData["SuccessMessage"] = $"Trading bot '{model.BotName}' updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating bot: {ex.Message}");
                model.AvailableAssets = await _context.DigitalAssets.OrderBy(da => da.Symbol).ToListAsync();
                return View(model);
            }
        }

        /// <summary>
        /// Create a new user bot instance from a template
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUserBot(TradingBotsPageViewModel pageModel)
        {
            var model = pageModel.CreateUserBot;
            
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => $"{x.Key}: {string.Join(", ", x.Value?.Errors.Select(e => e.ErrorMessage) ?? new List<string>())}")
                    .ToList();
                
                TempData["ErrorMessage"] = $"Validation errors: {string.Join("; ", errors)}";
                return RedirectToAction("Index");
            }

            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Add DCA frequency to parameters
                var parameters = new Dictionary<string, object>
                {
                    { "frequency", model.DCAFrequency }
                };
                
                if (!string.IsNullOrEmpty(model.Parameters))
                {
                    try
                    {
                        var existingParams = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(model.Parameters);
                        if (existingParams != null)
                        {
                            foreach (var param in existingParams)
                            {
                                parameters[param.Key] = param.Value;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore JSON parsing errors, use default parameters
                    }
                }
                
                model.Parameters = System.Text.Json.JsonSerializer.Serialize(parameters);

                var success = await _tradingBotService.CreateUserBotAsync(model, userId);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Trading bot '{model.Name}' created successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create trading bot. Please try again.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating trading bot: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Start a user bot (activate and begin trading)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> StartBot(long userBotId)
        {
            try
            {
                var success = await _tradingBotService.StartUserBotAsync(userBotId);
                if (success)
                {
                    // Set NextExecutionTime for scheduled bots (e.g., DCA)
                    await _tradingBotExecutionService.SetNextExecutionTimeAsync(userBotId);
                    
                    TempData["SuccessMessage"] = "Trading bot started successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to start trading bot.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error starting bot: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Stop a user bot (deactivate and stop trading)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> StopBot(long userBotId)
        {
            try
            {
                var success = await _tradingBotService.StopUserBotAsync(userBotId);
                if (success)
                {
                    TempData["SuccessMessage"] = "Trading bot stopped successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to stop trading bot.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error stopping bot: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Pause a user bot (temporarily suspend trading)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> PauseBot(long userBotId)
        {
            try
            {
                var success = await _tradingBotService.PauseUserBotAsync(userBotId);
                if (success)
                {
                    TempData["SuccessMessage"] = "Trading bot paused successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to pause trading bot.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error pausing bot: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Delete a user bot and all associated data
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DeleteBot(long userBotId)
        {
            try
            {
                var success = await _tradingBotService.DeleteUserBotAsync(userBotId);
                if (success)
                {
                    TempData["SuccessMessage"] = "Trading bot deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete trading bot.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting bot: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// View detailed information about a specific trading session
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SessionDetails(long sessionId)
        {
            try
            {
                var session = await _tradingBotService.GetTradingSessionByIdAsync(sessionId);
                if (session == null)
                {
                    TempData["ErrorMessage"] = "Trading session not found.";
                    return RedirectToAction("Index");
                }

                // Verify the session belongs to the current user
                var userId = GetCurrentUserId();
                if (session.UserBot?.UserId != userId)
                {
                    return Forbid();
                }

                var trades = await _tradingBotService.GetTradesAsync(sessionId);
                var metrics = await _tradingBotService.GetPerformanceMetricsAsync(sessionId);

                var viewModel = new SessionDetailsViewModel
                {
                    Session = session,
                    Trades = trades,
                    Metrics = metrics
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading session details: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// API endpoint to get bot configuration data for AJAX calls
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetBotConfiguration(long userBotId)
        {
            try
            {
                var config = await _tradingBotService.GetBotConfigurationAsync(userBotId);
                if (config == null)
                {
                    return NotFound();
                }

                // Get user bot to determine strategy
                var userBot = await _tradingBotService.GetUserBotByIdAsync(userBotId);
                if (userBot?.TradingBot?.Strategy == "DCA" && config.Parameters != null)
                {
                    var dcaConfig = _botConfigService.DeserializeConfiguration<DcaBotConfiguration>(config.Parameters);
                    return Json(new
                    {
                        weeklyBuyAmount = dcaConfig?.InvestmentAmount ?? 0,
                        investmentAmount = dcaConfig?.MaxTotalInvestment ?? (dcaConfig?.InvestmentAmount * 52) ?? 0,
                        startDay = (int)(dcaConfig?.DayOfWeek ?? DayOfWeek.Monday),
                        riskLevel = "Medium", // Default for DCA
                        parameters = config.Parameters
                    });
                }
                
                // Fallback for unknown strategies
                return Json(new
                {
                    weeklyBuyAmount = 0,
                    investmentAmount = 0,
                    startDay = 0,
                    riskLevel = "Medium",
                    parameters = config.Parameters ?? "{}"
                });
            }
            catch
            {
                return BadRequest();
            }
        }

        /// <summary>
        /// Run historical backtest for a user bot
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RunBacktest(BacktestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please check your backtest parameters and try again.";
                return RedirectToAction("Index");
            }

            try
            {
                // Validate date range
                if (model.StartDate >= model.EndDate)
                {
                    TempData["ErrorMessage"] = "Start date must be before end date.";
                    return RedirectToAction("Index");
                }

                if (model.EndDate > DateTime.UtcNow.AddDays(-1))
                {
                    TempData["ErrorMessage"] = "End date cannot be in the future.";
                    return RedirectToAction("Index");
                }

                // Run the backtest
                var sessionId = await _backtestingService.RunHistoricalBacktestAsync(model.UserBotId, model.StartDate, model.EndDate);
                
                if (sessionId > 0)
                {
                    TempData["SuccessMessage"] = $"Backtest completed successfully! View results in session #{sessionId}.";
                    return RedirectToAction("SessionDetails", new { sessionId });
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to run backtest. Please try again.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error running backtest: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Helper method to get the current user's ID from claims
        /// </summary>
        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out long userId))
            {
                return userId;
            }
            return 0;
        }

        /// <summary>
        /// Helper method to extract WeeklyBuyAmount from JSON configuration
        /// </summary>
        private decimal GetWeeklyBuyAmountFromJson(UserBot userBot)
        {
            try
            {
                var config = userBot.BotConfigurations?.FirstOrDefault();
                if (config?.Parameters != null && userBot.TradingBot?.Strategy == "DCA")
                {
                    var dcaConfig = _botConfigService.DeserializeConfiguration<DcaBotConfiguration>(config.Parameters);
                    return dcaConfig?.InvestmentAmount ?? 0; // InvestmentAmount in DCA is per-execution amount
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Helper method to extract InvestmentAmount from JSON configuration
        /// </summary>
        private decimal GetInvestmentAmountFromJson(UserBot userBot)
        {
            try
            {
                var config = userBot.BotConfigurations?.FirstOrDefault();
                if (config?.Parameters != null && userBot.TradingBot?.Strategy == "DCA")
                {
                    var dcaConfig = _botConfigService.DeserializeConfiguration<DcaBotConfiguration>(config.Parameters);
                    return dcaConfig?.MaxTotalInvestment ?? (dcaConfig?.InvestmentAmount * 52) ?? 0; // Estimate yearly amount
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Helper method to extract RiskLevel from JSON configuration
        /// </summary>
        private string GetRiskLevelFromJson(UserBot userBot)
        {
            try
            {
                // For now, just return default as DCA bots don't have risk level concept
                return "Medium";
            }
            catch
            {
                return "Medium";
            }
        }
    }

    /// <summary>
    /// ViewModel for session details page
    /// </summary>
    public class SessionDetailsViewModel
    {
        public TradingSession Session { get; set; } = null!;
        public List<Trade> Trades { get; set; } = new();
        public PerformanceMetric? Metrics { get; set; }
    }
}