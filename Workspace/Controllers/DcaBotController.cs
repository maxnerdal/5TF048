using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers
{
    /// <summary>
    /// Controller for managing DCA (Dollar Cost Average) trading bots
    /// </summary>
    public class DcaBotController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBotConfigurationService _botConfigService;
        private readonly IAuthenticationService _authService;

        public DcaBotController(
            ApplicationDbContext context,
            IBotConfigurationService botConfigService,
            IAuthenticationService authService)
        {
            _context = context;
            _botConfigService = botConfigService;
            _authService = authService;
        }

        /// <summary>
        /// Display list of user's DCA bots
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var currentUser = await _authService.GetCurrentUserAsync(HttpContext);
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var dcaBots = await _context.UserBots
                .Include(ub => ub.TradingBot)
                .Include(ub => ub.BotConfigurations)
                .Where(ub => ub.UserId == currentUser.Id && ub.TradingBot.Strategy == "DCA")
                .ToListAsync();

            var summaryList = new List<DcaBotSummaryViewModel>();

            foreach (var userBot in dcaBots)
            {
                var config = userBot.BotConfigurations.FirstOrDefault();
                var dcaConfig = config != null 
                    ? _botConfigService.DeserializeConfiguration<DcaBotConfiguration>(config.Parameters ?? "")
                    : new DcaBotConfiguration();

                var summary = new DcaBotSummaryViewModel
                {
                    UserBotId = userBot.UserBotId,
                    BotName = userBot.Name,
                    Status = userBot.Status,
                    Configuration = dcaConfig ?? new DcaBotConfiguration(),
                    LastRun = userBot.LastRun,
                    NextRun = dcaConfig?.GetNextExecutionTime(),
                    // TODO: Calculate performance metrics from trading sessions
                    TotalInvested = 0,
                    TotalPurchases = 0,
                    AveragePrice = 0,
                    CurrentValue = 0,
                    UnrealizedPnL = 0,
                    ROIPercentage = 0
                };

                summaryList.Add(summary);
            }

            return View(summaryList);
        }

        /// <summary>
        /// Show form to create a new DCA bot
        /// </summary>
        public async Task<IActionResult> Create()
        {
            var currentUser = await _authService.GetCurrentUserAsync(HttpContext);
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

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
        /// Process creation of a new DCA bot
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DcaBotConfigurationViewModel model)
        {
            var currentUser = await _authService.GetCurrentUserAsync(HttpContext);
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

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
                // Reload available assets for the form
                model.AvailableAssets = await _context.DigitalAssets
                    .OrderBy(da => da.Symbol)
                    .ToListAsync();
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get the DCA bot template
                var dcaBotTemplate = await _context.TradingBots
                    .FirstOrDefaultAsync(tb => tb.Strategy == "DCA");

                if (dcaBotTemplate == null)
                {
                    ModelState.AddModelError("", "DCA bot template not found. Please run the database migration.");
                    model.AvailableAssets = await _context.DigitalAssets.OrderBy(da => da.Symbol).ToListAsync();
                    return View(model);
                }

                // Create the UserBot
                var userBot = new UserBot
                {
                    UserId = currentUser.Id,
                    BotId = dcaBotTemplate.BotId,
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

                TempData["SuccessMessage"] = $"DCA bot '{model.BotName}' created successfully!";
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
        /// Show form to edit an existing DCA bot
        /// </summary>
        public async Task<IActionResult> Edit(long id)
        {
            var currentUser = await _authService.GetCurrentUserAsync(HttpContext);
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var userBot = await _context.UserBots
                .Include(ub => ub.BotConfigurations)
                .FirstOrDefaultAsync(ub => ub.UserBotId == id && ub.UserId == currentUser.Id);

            if (userBot == null)
                return NotFound();

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

        /// <summary>
        /// Process editing of an existing DCA bot
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, DcaBotConfigurationViewModel model)
        {
            var currentUser = await _authService.GetCurrentUserAsync(HttpContext);
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var userBot = await _context.UserBots
                .Include(ub => ub.BotConfigurations)
                .FirstOrDefaultAsync(ub => ub.UserBotId == id && ub.UserId == currentUser.Id);

            if (userBot == null)
                return NotFound();

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

            try
            {
                // Update bot name
                userBot.Name = model.BotName;

                // Update configuration
                var configJson = _botConfigService.SerializeConfiguration(model.Configuration);
                var botConfig = userBot.BotConfigurations.FirstOrDefault();

                if (botConfig == null)
                {
                    botConfig = new BotConfiguration
                    {
                        UserBotId = userBot.UserBotId,
                        Parameters = configJson
                    };
                    _context.BotConfigurations.Add(botConfig);
                }
                else
                {
                    botConfig.Parameters = configJson;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"DCA bot '{model.BotName}' updated successfully!";
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
        /// Start/stop a DCA bot
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            var currentUser = await _authService.GetCurrentUserAsync(HttpContext);
            if (currentUser == null)
                return Json(new { success = false, message = "User not authenticated" });

            var userBot = await _context.UserBots
                .FirstOrDefaultAsync(ub => ub.UserBotId == id && ub.UserId == currentUser.Id);

            if (userBot == null)
                return Json(new { success = false, message = "Bot not found" });

            try
            {
                userBot.Status = userBot.Status == "Active" ? "Inactive" : "Active";
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    newStatus = userBot.Status,
                    message = $"Bot {userBot.Status.ToLower()}" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Delete a DCA bot
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(long id)
        {
            var currentUser = await _authService.GetCurrentUserAsync(HttpContext);
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var userBot = await _context.UserBots
                .Include(ub => ub.BotConfigurations)
                .FirstOrDefaultAsync(ub => ub.UserBotId == id && ub.UserId == currentUser.Id);

            if (userBot == null)
                return NotFound();

            try
            {
                _context.UserBots.Remove(userBot);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"DCA bot '{userBot.Name}' deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting bot: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Get bot configuration details as JSON for AJAX calls
        /// </summary>
        public async Task<IActionResult> GetConfiguration(long id)
        {
            var currentUser = await _authService.GetCurrentUserAsync(HttpContext);
            if (currentUser == null)
                return Json(new { success = false, message = "User not authenticated" });

            var userBot = await _context.UserBots
                .Include(ub => ub.BotConfigurations)
                .FirstOrDefaultAsync(ub => ub.UserBotId == id && ub.UserId == currentUser.Id);

            if (userBot == null)
                return Json(new { success = false, message = "Bot not found" });

            var config = userBot.BotConfigurations.FirstOrDefault();
            var dcaConfig = config != null
                ? _botConfigService.DeserializeConfiguration<DcaBotConfiguration>(config.Parameters ?? "")
                : new DcaBotConfiguration();

            return Json(new { 
                success = true, 
                configuration = dcaConfig,
                description = BotConfigurationService.GetConfigurationDescription(dcaConfig ?? new DcaBotConfiguration())
            });
        }
    }
}