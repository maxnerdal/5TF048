using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers
{
    public class PortfolioController : Controller
    {
        private readonly IBtcPriceService _btcPriceService;
        private readonly ISimpleSessionService _sessionService;
        
        // In-memory storage for demo (in real app, you'd use a database)
        private static List<PortfolioItem> _portfolio = new List<PortfolioItem>
        {
            new PortfolioItem 
            { 
                Id = 1, 
                CoinName = "Bitcoin", 
                Symbol = "BTC", 
                Quantity = 0.5m, 
                BuyPrice = 45000m, 
                DatePurchased = DateTime.Today.AddDays(-30) 
            },
            new PortfolioItem 
            { 
                Id = 2, 
                CoinName = "Ethereum", 
                Symbol = "ETH", 
                Quantity = 2.0m, 
                BuyPrice = 3000m, 
                DatePurchased = DateTime.Today.AddDays(-15) 
            }
        };
        
        private static int _nextId = 3;

        public PortfolioController(IBtcPriceService btcPriceService, ISimpleSessionService sessionService)
        {
            _btcPriceService = btcPriceService;
            _sessionService = sessionService;
        }

        // GET: Portfolio
        public async Task<IActionResult> Index(string? message = null)
        {
            // Simple session usage - track visit count and last visit
            var visitCount = _sessionService.GetInt(HttpContext, "VisitCount");
            visitCount++;
            _sessionService.SetInt(HttpContext, "VisitCount", visitCount);
            _sessionService.SetString(HttpContext, "LastVisit", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            
            // Add session info to ViewBag
            ViewBag.VisitCount = visitCount;
            ViewBag.LastVisit = _sessionService.GetString(HttpContext, "LastVisit");
            ViewBag.CoinsAdded = _sessionService.GetInt(HttpContext, "CoinsAdded");
            
            // For demo, we'll only get current price for Bitcoin
            try
            {
                var btcPrice = await _btcPriceService.GetBtcPriceAsync();
                
                // Update Bitcoin prices in portfolio
                foreach (var item in _portfolio.Where(p => p.Symbol.ToUpper() == "BTC"))
                {
                    item.CurrentPrice = btcPrice.Price;
                }
                
                // For other coins, we'll use mock prices (in real app, extend service for multiple coins)
                foreach (var item in _portfolio.Where(p => p.Symbol.ToUpper() != "BTC"))
                {
                    item.CurrentPrice = item.BuyPrice * 1.1m; // Mock 10% gain
                }
            }
            catch
            {
                // If API fails, use buy price as current price
                foreach (var item in _portfolio)
                {
                    item.CurrentPrice = item.BuyPrice;
                }
            }

            // Calculate totals for ViewBag
            ViewBag.TotalInvestment = _portfolio.Sum(p => p.TotalInvestment);
            ViewBag.TotalCurrentValue = _portfolio.Sum(p => p.CurrentValue);
            ViewBag.TotalProfitLoss = _portfolio.Sum(p => p.ProfitLoss);
            ViewBag.TotalProfitLossPercentage = ViewBag.TotalInvestment > 0 
                ? (decimal)ViewBag.TotalProfitLoss / (decimal)ViewBag.TotalInvestment 
                : 0m;
            
            // Add success message to ViewBag if present
            if (!string.IsNullOrEmpty(message))
            {
                ViewBag.SuccessMessage = message;
            }
            
            return View(_portfolio);
        }

        // GET: Portfolio/Create
        public IActionResult Create()
        {
            // Check if there's a draft in session
            var draft = new PortfolioItem();
            
            var draftCoinName = _sessionService.GetString(HttpContext, "DraftCoinName");
            if (!string.IsNullOrEmpty(draftCoinName))
            {
                draft.CoinName = draftCoinName;
                draft.Symbol = _sessionService.GetString(HttpContext, "DraftSymbol") ?? "";
                
                var quantityStr = _sessionService.GetString(HttpContext, "DraftQuantity");
                if (!string.IsNullOrEmpty(quantityStr) && decimal.TryParse(quantityStr, out var quantity))
                {
                    draft.Quantity = quantity;
                }
                
                var priceStr = _sessionService.GetString(HttpContext, "DraftBuyPrice");
                if (!string.IsNullOrEmpty(priceStr) && decimal.TryParse(priceStr, out var price))
                {
                    draft.BuyPrice = price;
                }
                
                var dateStr = _sessionService.GetString(HttpContext, "DraftDatePurchased");
                if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out var date))
                {
                    draft.DatePurchased = date;
                }
                
                ViewBag.HasDraft = true;
                ViewBag.DraftMessage = "Found unsaved draft data. You can continue where you left off!";
            }
            
            return View(draft);
        }

        // POST: Portfolio/Create
        [HttpPost]
        public IActionResult Create(PortfolioItem item)
        {
            if (ModelState.IsValid)
            {
                item.Id = _nextId++;
                item.Symbol = item.Symbol.ToUpper();
                _portfolio.Add(item);
                
                // Clear draft data on successful save
                ClearDraftData();
                
                // Track coins added in session
                var coinsAdded = _sessionService.GetInt(HttpContext, "CoinsAdded");
                coinsAdded++;
                _sessionService.SetInt(HttpContext, "CoinsAdded", coinsAdded);
                
                return RedirectToAction(nameof(Index), new { message = $"Successfully added {item.CoinName} to your portfolio! (Total coins added this session: {coinsAdded})" });
            }
            
            return View(item);
        }

        // POST: Portfolio/SaveDraft - AJAX endpoint to save draft
        [HttpPost]
        public IActionResult SaveDraft([FromBody] PortfolioItem draft)
        {
            // Save form data to session
            _sessionService.SetString(HttpContext, "DraftCoinName", draft.CoinName ?? "");
            _sessionService.SetString(HttpContext, "DraftSymbol", draft.Symbol ?? "");
            _sessionService.SetString(HttpContext, "DraftQuantity", draft.Quantity.ToString());
            _sessionService.SetString(HttpContext, "DraftBuyPrice", draft.BuyPrice.ToString());
            _sessionService.SetString(HttpContext, "DraftDatePurchased", draft.DatePurchased.ToString("yyyy-MM-dd"));
            
            return Json(new { success = true, message = "Draft saved!" });
        }

        // Clear draft data
        private void ClearDraftData()
        {
            // For simplicity, we'll set empty values instead of implementing a Remove method
            _sessionService.SetString(HttpContext, "DraftCoinName", "");
            _sessionService.SetString(HttpContext, "DraftSymbol", "");
            _sessionService.SetString(HttpContext, "DraftQuantity", "");
            _sessionService.SetString(HttpContext, "DraftBuyPrice", "");
            _sessionService.SetString(HttpContext, "DraftDatePurchased", "");
        }

        // GET: Portfolio/Edit/5
        public IActionResult Edit(int id)
        {
            var item = _portfolio.FirstOrDefault(p => p.Id == id);
            if (item == null)
            {
                return NotFound();
            }
            
            return View(item);
        }

        // POST: Portfolio/Edit/5
        [HttpPost]
        public IActionResult Edit(int id, PortfolioItem item)
        {
            if (id != item.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingItem = _portfolio.FirstOrDefault(p => p.Id == id);
                if (existingItem == null)
                {
                    return NotFound();
                }

                existingItem.CoinName = item.CoinName;
                existingItem.Symbol = item.Symbol.ToUpper();
                existingItem.Quantity = item.Quantity;
                existingItem.BuyPrice = item.BuyPrice;
                existingItem.DatePurchased = item.DatePurchased;
                
                return RedirectToAction(nameof(Index), new { message = $"Successfully updated {item.CoinName}!" });
            }
            
            return View(item);
        }

        // GET: Portfolio/Delete/5
        public IActionResult Delete(int id)
        {
            var item = _portfolio.FirstOrDefault(p => p.Id == id);
            if (item == null)
            {
                return NotFound();
            }
            
            return View(item);
        }

        // POST: Portfolio/Delete/5
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var item = _portfolio.FirstOrDefault(p => p.Id == id);
            if (item != null)
            {
                _portfolio.Remove(item);
                return RedirectToAction(nameof(Index), new { message = $"Successfully removed {item.CoinName} from your portfolio!" });
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
}
