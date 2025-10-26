using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        private readonly ITradingBotService _tradingBotService;
        private readonly IBacktestingService _backtestingService;

        public TradingBotController(ITradingBotService tradingBotService, IBacktestingService backtestingService)
        {
            _tradingBotService = tradingBotService;
            _backtestingService = backtestingService;
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
                    WeeklyBuyAmount = userBot.BotConfigurations?.FirstOrDefault()?.WeeklyBuyAmount ?? 0,
                    InvestmentAmount = userBot.BotConfigurations?.FirstOrDefault()?.InvestmentAmount ?? 0,
                    RiskLevel = userBot.BotConfigurations?.FirstOrDefault()?.RiskLevel ?? "Medium"
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

                return Json(new
                {
                    weeklyBuyAmount = config.WeeklyBuyAmount,
                    investmentAmount = config.InvestmentAmount,
                    startDay = config.StartDay,
                    riskLevel = config.RiskLevel,
                    parameters = config.Parameters
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