using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.Models;
using WebApp.Services;
using System.Security.Claims;

namespace WebApp.Controllers
{
    /// <summary>
    /// PortfolioController handles all operations related to managing a cryptocurrency portfolio.
    /// This controller allows users to view, create, edit, and delete portfolio items.
    /// It integrates with Bitcoin price service and database storage for user portfolios.
    /// [Authorize] attribute ensures only authenticated users can access this controller.
    /// </summary>
    [Authorize]
    public class PortfolioController : Controller
    {
        // Dependency injection: These services are automatically provided by ASP.NET Core
        private readonly IBtcPriceService _btcPriceService;  // Service to get Bitcoin prices from external API
        private readonly IPortfolioService _portfolioService;  // Service to manage portfolio database operations
        
        /// <summary>
        /// Helper method to get the current user's ID from the authentication claims
        /// </summary>
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Constructor - called when ASP.NET Core creates an instance of this controller.
        /// The framework automatically provides the required services through dependency injection.
        /// </summary>
        /// <param name="btcPriceService">Service to fetch Bitcoin prices from external API</param>
        /// <param name="portfolioService">Service to manage portfolio database operations</param>
        public PortfolioController(IBtcPriceService btcPriceService, IPortfolioService portfolioService)
        {
            _btcPriceService = btcPriceService;
            _portfolioService = portfolioService;
        }

        /// <summary>
        /// GET: Portfolio
        /// Main portfolio page that displays all portfolio items with current prices and calculations.
        /// This method handles the URL: /Portfolio or /Portfolio/Index
        /// </summary>
        /// <param name="message">Optional success message to display (e.g., "Item added successfully")</param>
        /// <returns>View with list of all portfolio items and summary calculations</returns>
        public async Task<IActionResult> Index(string? message = null)
        {
            // Get current user ID
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized();
            }

            // Get user's portfolio from database
            var portfolio = await _portfolioService.GetUserPortfolioAsync(userId);

            // Step 1: Try to get current Bitcoin price from external API
            try
            {
                // Call the Bitcoin price service to get latest price
                var btcPrice = await _btcPriceService.GetBtcPriceAsync();
                
                // Step 2: Update the current price for all Bitcoin items in the portfolio
                foreach (var item in portfolio.Where(p => p.AssetTicker.ToUpper() == "BTC"))
                {
                    item.CurrentPrice = btcPrice.Price;  // Set real-time Bitcoin price
                }
                
                // Step 3: For non-Bitcoin coins, we simulate prices (real app would call APIs for each coin)
                // This gives a mock 10% gain for demonstration purposes
                foreach (var item in portfolio.Where(p => p.AssetTicker.ToUpper() != "BTC"))
                {
                    item.CurrentPrice = item.BuyPrice * 1.1m; // Mock 10% gain (multiply by 1.1)
                }
            }
            catch
            {
                // Step 4: If the API call fails (network issues, API down, etc.)
                // we fall back to using the buy price as current price (no gain/loss shown)
                foreach (var item in portfolio)
                {
                    item.CurrentPrice = item.BuyPrice;  // Fallback: assume no price change
                }
            }

            // Step 5: Calculate portfolio summary totals using LINQ Sum() method
            // ViewBag is a way to pass data from controller to the view (HTML page)
            
            // Total amount invested = sum of (quantity × buy price) for each item
            ViewBag.TotalInvestment = portfolio.Sum(p => p.TotalInvestment);
            
            // Current total value = sum of (quantity × current price) for each item  
            ViewBag.TotalCurrentValue = portfolio.Sum(p => p.CurrentValue);
            
            // Total profit/loss = current value - total investment
            ViewBag.TotalProfitLoss = portfolio.Sum(p => p.ProfitLoss);
            
            // Profit/loss percentage = (profit/loss ÷ investment) × 100
            // We check > 0 to avoid division by zero error
            ViewBag.TotalProfitLossPercentage = ViewBag.TotalInvestment > 0 
                ? (decimal)ViewBag.TotalProfitLoss / (decimal)ViewBag.TotalInvestment 
                : 0m;  // 'm' suffix indicates decimal literal
            
            // Step 6: If there's a success message (from redirects after add/edit/delete), 
            // pass it to the view to display to user
            if (!string.IsNullOrEmpty(message))
            {
                ViewBag.SuccessMessage = message;
            }
            
            // Return the view with the portfolio list as the model
            return View(portfolio);
        }

        /// <summary>
        /// GET: Portfolio/Create
        /// Shows the form to add a new portfolio item.
        /// </summary>
        /// <returns>Create view with empty form</returns>
        public async Task<IActionResult> Create()
        {
            try
            {
                // Get available digital assets for dropdown
                var assets = await _portfolioService.GetAvailableAssetsAsync();
                ViewBag.DigitalAssets = new SelectList(assets, "Id", "Name");
                
                // Create a new empty portfolio item to use as form model
                var portfolioItem = new PortfolioItemViewModel();
                
                // Return the create view with the empty model
                return View(portfolioItem);
            }
            catch (Exception)
            {
                // Log the error and provide a fallback
                ViewBag.DigitalAssets = new SelectList(new List<DigitalAsset>(), "Id", "Name");
                ViewBag.Error = "Unable to load digital assets. Please try again later.";
                return View(new PortfolioItemViewModel());
            }
        }

        /// <summary>
        /// POST: Portfolio/Create
        /// Processes the submitted form data to create a new portfolio item.
        /// This method is called when user clicks "Submit" on the create form.
        /// </summary>
        /// <param name="item">The portfolio item data from the form</param>
        /// <returns>Redirect to portfolio list if successful, or back to form if validation fails</returns>
        [HttpPost]  // This attribute means this method only responds to POST requests (form submissions)
        public async Task<IActionResult> Create(PortfolioItemViewModel item)
        {
            // Get current user ID
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized();
            }

            // Step 1: Check if the submitted data passes validation rules
            if (ModelState.IsValid)
            {
                // Debug: Log the values being submitted
                Console.WriteLine($"Submitting: AssetId={item.AssetId}, Quantity={item.Quantity}, BuyPrice={item.BuyPrice}, DatePurchased={item.DatePurchased}");
                
                // Step 2: Add the new item to the database
                var success = await _portfolioService.AddPortfolioItemAsync(item, userId);
                
                if (success)
                {
                    // Get asset name for success message
                    var assets = await _portfolioService.GetAvailableAssetsAsync();
                    var asset = assets.FirstOrDefault(a => a.Id == item.AssetId);
                    var assetName = asset?.Name ?? "Asset";
                    
                    // Step 3: Redirect to the portfolio list with a success message
                    return RedirectToAction(nameof(Index), new { message = $"Successfully added {assetName} to your portfolio!" });
                }
                else
                {
                    ModelState.AddModelError("", "Failed to add item to portfolio. Check the server logs for details.");
                }
            }
            else
            {
                // Debug: Log validation errors
                foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation error: {modelError.ErrorMessage}");
                }
            }
            
            // If validation failed or save failed, return to the create view with the submitted data
            // Reload the assets dropdown
            var assetsForDropdown = await _portfolioService.GetAvailableAssetsAsync();
            ViewBag.DigitalAssets = new SelectList(assetsForDropdown ?? new List<DigitalAsset>(), "Id", "Name", item.AssetId);
            
            return View(item);
        }

        /// <summary>
        /// GET: Portfolio/Edit/5
        /// Shows the edit form for an existing portfolio item.
        /// The number in the URL (like /Portfolio/Edit/5) becomes the 'id' parameter.
        /// </summary>
        /// <param name="id">The unique ID of the portfolio item to edit</param>
        /// <returns>Edit view with the item data, or NotFound if item doesn't exist</returns>
        public async Task<IActionResult> Edit(int id)
        {
            // Get current user ID
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized();
            }

            // Step 1: Search for the portfolio item with matching ID for this user
            var item = await _portfolioService.GetPortfolioItemAsync(id, userId);
            
            // Step 2: If item not found, return HTTP 404 Not Found response
            if (item == null)
            {
                return NotFound();  // This shows a "404 - Page Not Found" error
            }

            // Get available digital assets for dropdown
            var assets = await _portfolioService.GetAvailableAssetsAsync();
            ViewBag.DigitalAssets = new SelectList(assets ?? new List<DigitalAsset>(), "Id", "Name", item.AssetId);
            
            // Step 3: Return the edit view with the found item as the model
            return View(item);
        }

        /// <summary>
        /// POST: Portfolio/Edit/5
        /// Processes the submitted edit form to update an existing portfolio item.
        /// Called when user clicks "Save" on the edit form.
        /// </summary>
        /// <param name="id">The ID from the URL (should match the item being edited)</param>
        /// <param name="item">The updated portfolio item data from the form</param>
        /// <returns>Redirect to portfolio list if successful, or back to edit form if validation fails</returns>
        [HttpPost]  // Only responds to form submissions
        public async Task<IActionResult> Edit(int id, PortfolioItemViewModel item)
        {
            // Get current user ID
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized();
            }

            // Step 1: Security check - ensure URL ID matches the item ID
            if (id != item.Id)
            {
                return NotFound();  // Return 404 if IDs don't match
            }

            // Step 2: Validate the submitted form data
            if (ModelState.IsValid)
            {
                // Step 3: Update the item in the database
                var success = await _portfolioService.UpdatePortfolioItemAsync(item, userId);
                
                if (success)
                {
                    // Get asset name for success message
                    var assets = await _portfolioService.GetAvailableAssetsAsync();
                    var asset = assets.FirstOrDefault(a => a.Id == item.AssetId);
                    var assetName = asset?.Name ?? "Asset";
                    
                    // Step 4: Redirect to portfolio list with success message
                    return RedirectToAction(nameof(Index), new { message = $"Successfully updated {assetName}!" });
                }
                else
                {
                    ModelState.AddModelError("", "Failed to update item. Please try again.");
                }
            }
            
            // If validation failed or save failed, return to edit form with validation errors
            // Reload the assets dropdown
            var assetsForDropdown = await _portfolioService.GetAvailableAssetsAsync();
            ViewBag.DigitalAssets = new SelectList(assetsForDropdown ?? new List<DigitalAsset>(), "Id", "Name", item.AssetId);
            
            return View(item);
        }

        /// <summary>
        /// GET: Portfolio/Delete/5
        /// Shows a confirmation page before deleting a portfolio item.
        /// This is a safety measure to prevent accidental deletions.
        /// </summary>
        /// <param name="id">The unique ID of the portfolio item to delete</param>
        /// <returns>Delete confirmation view with item details, or NotFound if item doesn't exist</returns>
        public async Task<IActionResult> Delete(int id)
        {
            // Get current user ID
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized();
            }

            // Step 1: Find the portfolio item to delete for this user
            var item = await _portfolioService.GetPortfolioItemAsync(id, userId);
            
            // Step 2: If item not found, return 404 error
            if (item == null)
            {
                return NotFound();
            }
            
            // Step 3: Show confirmation page with item details
            return View(item);
        }

        /// <summary>
        /// POST: Portfolio/Delete/5
        /// Actually performs the deletion after user confirms.
        /// ActionName attribute allows us to have two methods with same name but different HTTP verbs.
        /// </summary>
        /// <param name="id">The unique ID of the portfolio item to delete</param>
        /// <returns>Redirect to portfolio list with success message</returns>
        [HttpPost, ActionName("Delete")]  // This method handles POST to /Portfolio/Delete/5
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Get current user ID
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized();
            }

            // Step 1: Get the item details for the success message before deleting
            var item = await _portfolioService.GetPortfolioItemAsync(id, userId);
            
            // Step 2: Delete the item from the database
            var success = await _portfolioService.DeletePortfolioItemAsync(id, userId);
            
            if (success && item != null)
            {
                // Step 3: Redirect with success message showing what was deleted
                return RedirectToAction(nameof(Index), new { message = $"Successfully removed {item.AssetName} from your portfolio!" });
            }
            
            // If deletion failed or item wasn't found, just redirect to portfolio list
            return RedirectToAction(nameof(Index));
        }
    }
}
