using Microsoft.AspNetCore.Mvc;
using WebApp.Services;

namespace WebApp.Controllers
{
    public class CryptoMarketController : Controller
    {
        private readonly IBtcPriceService _btcPriceService;
        private readonly ILogger<CryptoMarketController> _logger;

        public CryptoMarketController(IBtcPriceService btcPriceService, ILogger<CryptoMarketController> logger)
        {
            _btcPriceService = btcPriceService;
            _logger = logger;
        }

        // Displays top 100 cryptocurrencies with sorting, filtering and search
        public async Task<IActionResult> Top100(string sortBy = "marketcap", string category = "", string search = "")
        {
            try
            {
                var cryptos = await _btcPriceService.GetTop100CryptosAsync();
                
                // Filter by search term if specified
                if (!string.IsNullOrEmpty(search))
                {
                    cryptos = cryptos.Where(c => 
                        c.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        c.Symbol.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        c.Id.Contains(search, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }
                
                // Filter by category if specified
                if (!string.IsNullOrEmpty(category))
                {
                    cryptos = cryptos.Where(c => c.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)).ToList();
                }
                
                // Sort the data
                cryptos = sortBy.ToLower() switch
                {
                    "price" => cryptos.OrderByDescending(c => c.CurrentPrice).ToList(),
                    "change" => cryptos.OrderByDescending(c => c.PriceChangePercentage24h).ToList(),
                    "volume" => cryptos.OrderByDescending(c => c.TotalVolume).ToList(),
                    "marketcap" or _ => cryptos.OrderByDescending(c => c.MarketCap).ToList()
                };
                
                // Pass sorting, filtering and search info to view
                ViewBag.CurrentSort = sortBy;
                ViewBag.CurrentCategory = category;
                ViewBag.CurrentSearch = search;
                ViewBag.AllCategories = GetAllCategories(await _btcPriceService.GetTop100CryptosAsync());
                
                return View(cryptos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading top 100 cryptocurrencies");
                TempData["Error"] = "Unable to load cryptocurrency data. Please try again later.";
                return View(new List<WebApp.Models.CryptoMarketModel>());
            }
        }
        
        private List<string> GetAllCategories(List<WebApp.Models.CryptoMarketModel> cryptos)
        {
            return cryptos
                .SelectMany(c => c.Categories)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }
    }
}