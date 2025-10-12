using Microsoft.AspNetCore.Mvc;
using WebApp.Services;
using WebApp.Models;

namespace WebApp.Controllers
{
    /// <summary>
    /// Controller for manual market data management and updates
    /// Provides endpoints for administrators to manually trigger data updates
    /// </summary>
    public class MarketDataController : Controller
    {
        private readonly IBinanceService _binanceService;
        private readonly ILogger<MarketDataController> _logger;

        public MarketDataController(IBinanceService binanceService, ILogger<MarketDataController> logger)
        {
            _binanceService = binanceService;
            _logger = logger;
        }

        /// <summary>
        /// Display the market data management page
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var viewModel = new MarketDataManagementViewModel();

            try
            {
                // Get current data statistics
                var latestBtcData = await _binanceService.GetLatestStoredDataAsync("BTCUSDT", "1m");
                viewModel.LatestBtcData = latestBtcData;

                if (latestBtcData != null)
                {
                    var allBtcData = await _binanceService.GetStoredDataAsync("BTCUSDT", "1m", 
                        DateTime.UtcNow.AddYears(-10), DateTime.UtcNow);
                    viewModel.TotalBtcRecords = allBtcData.Count;
                    
                    if (allBtcData.Any())
                    {
                        viewModel.OldestBtcData = allBtcData.OrderBy(x => x.OpenTime).First();
                    }
                }

                viewModel.AvailableSymbols = await _binanceService.GetAvailableSymbolsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading market data statistics");
                ViewBag.Error = "Error loading data statistics: " + ex.Message;
            }

            return View(viewModel);
        }

        /// <summary>
        /// Manually trigger a full BTC historical data update
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateBtcHistoricalData()
        {
            try
            {
                _logger.LogInformation("Manual BTC historical data update started");

                const string symbol = "BTCUSDT";
                const string timeFrame = "1m";

                // Get latest data to determine where to start
                var latestData = await _binanceService.GetLatestStoredDataAsync(symbol, timeFrame);
                
                DateTime startDate;
                if (latestData != null)
                {
                    startDate = latestData.CloseTime.AddMinutes(1);
                    TempData["Info"] = $"Updating from latest data point: {latestData.CloseTime:yyyy-MM-dd HH:mm}";
                }
                else
                {
                    // Start from 90 days ago for initial load (reasonable amount)
                    startDate = DateTime.UtcNow.AddDays(-90);
                    TempData["Info"] = $"No existing data found. Loading initial dataset from: {startDate:yyyy-MM-dd HH:mm}";
                }

                var endDate = DateTime.UtcNow;
                
                if (startDate >= endDate)
                {
                    TempData["Success"] = "Data is already up to date!";
                    return RedirectToAction("Index");
                }

                // Perform the update
                int recordsAdded = await _binanceService.FillDataGapsAsync(symbol, timeFrame, startDate, endDate);

                TempData["Success"] = $"Successfully added {recordsAdded} new BTC minute records!";
                _logger.LogInformation("Manual update completed: {RecordsAdded} records added", recordsAdded);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Manual BTC update failed");
                TempData["Error"] = "Update failed: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Load complete historical data for BTC (careful - this is a lot of data!)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> LoadCompleteHistory(int daysBack = 365)
        {
            try
            {
                _logger.LogInformation("Loading complete BTC history for {DaysBack} days", daysBack);

                const string symbol = "BTCUSDT";
                const string timeFrame = "1m";

                var startDate = DateTime.UtcNow.AddDays(-daysBack);
                var endDate = DateTime.UtcNow;

                // This could take a while!
                int recordsAdded = await _binanceService.FillDataGapsAsync(symbol, timeFrame, startDate, endDate);

                TempData["Success"] = $"Historical data load completed! Added {recordsAdded} records for the last {daysBack} days.";
                _logger.LogInformation("Complete history load finished: {RecordsAdded} records for {DaysBack} days", 
                    recordsAdded, daysBack);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Complete history load failed");
                TempData["Error"] = "History load failed: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Load massive historical dataset (multiple years) - use with caution!
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> LoadMassiveHistory(int yearsBack = 2)
        {
            try
            {
                _logger.LogInformation("Loading MASSIVE BTC history for {YearsBack} years - this will take a long time!", yearsBack);

                const string symbol = "BTCUSDT";
                const string timeFrame = "1m";

                // Start from when Binance launched BTCUSDT (approximately August 2017)
                var binanceLaunchDate = new DateTime(2017, 8, 17, 0, 0, 0, DateTimeKind.Utc);
                var requestedStartDate = DateTime.UtcNow.AddYears(-yearsBack);
                
                // Use the later of the two dates (can't get data before Binance existed)
                var startDate = requestedStartDate > binanceLaunchDate ? requestedStartDate : binanceLaunchDate;
                var endDate = DateTime.UtcNow;

                var totalDays = (endDate - startDate).TotalDays;
                var estimatedRecords = (int)(totalDays * 24 * 60); // minutes per day

                TempData["Info"] = $"Starting massive historical load: ~{estimatedRecords:N0} records over {totalDays:N0} days. This may take 30+ minutes!";

                // This will take a VERY long time!
                int recordsAdded = await _binanceService.FillDataGapsAsync(symbol, timeFrame, startDate, endDate);

                TempData["Success"] = $"🎉 MASSIVE historical data load completed! Added {recordsAdded:N0} records spanning {totalDays:N0} days!";
                _logger.LogInformation("Massive history load finished: {RecordsAdded} records over {TotalDays} days", 
                    recordsAdded, totalDays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Massive history load failed");
                TempData["Error"] = "Massive history load failed: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Delete all market data (for testing/cleanup)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ClearAllData()
        {
            try
            {
                // This would need to be implemented in BinanceService
                // For now, just show a message
                TempData["Warning"] = "Clear data functionality would need to be implemented in BinanceService";
                _logger.LogWarning("Clear data requested - not implemented yet");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Clear data failed");
                TempData["Error"] = "Clear data failed: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}