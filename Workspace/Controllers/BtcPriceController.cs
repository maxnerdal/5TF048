using Microsoft.AspNetCore.Mvc;
using WebApp.Services;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class BtcPriceController : Controller
    {
        private readonly IBtcPriceService _btcPriceService;

        public BtcPriceController(IBtcPriceService btcPriceService)
        {
            _btcPriceService = btcPriceService;
        }

        public async Task<IActionResult> Index()
        {
            var btcPrice = await _btcPriceService.GetBtcPriceAsync(); // Returns BtcPriceModel
            
            // 🎯 GREAT VIEWBAG USE CASE: Additional crypto info without creating new models
            ViewBag.PageTitle = "Bitcoin Dashboard";
            ViewBag.MarketStatus = GetMarketStatus();
            ViewBag.PriceColor = btcPrice.ChangePercent24h >= 0 ? "text-success" : "text-danger";
            ViewBag.PriceIcon = btcPrice.ChangePercent24h >= 0 ? "⬆️" : "⬇️";
            ViewBag.AlertMessage = GetPriceAlert(btcPrice.Price);
            ViewBag.NextUpdateTime = DateTime.Now.AddMinutes(5).ToString("HH:mm");
            ViewBag.TradingTip = GetRandomTradingTip();
            
            return View(btcPrice); // Main model: BtcPriceModel
        }
        
        private string GetMarketStatus()
        {
            var hour = DateTime.Now.Hour;
            return hour >= 9 && hour <= 16 ? "Markets Open 🟢" : "Markets Closed 🔴";
        }
        
        private string GetPriceAlert(decimal price)
        {
            if (price > 100000) return "🚀 BTC above $100K!";
            if (price > 75000) return "📈 BTC trending high!";
            if (price < 20000) return "📉 BTC at discount price!";
            return "💰 Good time to check the market!";
        }
        
        private string GetRandomTradingTip()
        {
            var tips = new[]
            {
                "💡 Never invest more than you can afford to lose",
                "⏰ Dollar-cost averaging reduces risk",
                "📊 Always do your own research (DYOR)",
                "🎯 Set stop-losses to protect your investment",
                "🧘 Stay calm during market volatility"
            };
            var random = new Random();
            return tips[random.Next(tips.Length)];
        }

        // API endpoint for AJAX calls
        [HttpGet]
        public async Task<IActionResult> GetPrice()
        {
            var btcPrice = await _btcPriceService.GetBtcPriceAsync();
            return Json(btcPrice);
        }
    }
}
