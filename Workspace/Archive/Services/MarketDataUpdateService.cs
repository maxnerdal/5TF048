using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebApp.Models;

namespace WebApp.Services
{
    /// <summary>
    /// Background service that automatically updates historical market data daily
    /// Runs once per day to fetch the latest BTC data and fill any gaps
    /// </summary>
    public class MarketDataUpdateService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MarketDataUpdateService> _logger;
        private readonly TimeSpan _dailyInterval = TimeSpan.FromHours(24);
        private readonly TimeSpan _startupDelay = TimeSpan.FromMinutes(2); // Wait 2 minutes after startup

        public MarketDataUpdateService(
            IServiceProvider serviceProvider,
            ILogger<MarketDataUpdateService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MarketDataUpdateService starting up...");

            // Wait a bit after startup to let the application fully initialize
            await Task.Delay(_startupDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateMarketDataAsync();
                    
                    _logger.LogInformation("Next market data update scheduled in {Hours} hours", _dailyInterval.TotalHours);
                    await Task.Delay(_dailyInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("MarketDataUpdateService is being cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during market data update");
                    
                    // Wait 1 hour before retrying on error
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        private async Task UpdateMarketDataAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var binanceService = scope.ServiceProvider.GetRequiredService<IBinanceService>();

            _logger.LogInformation("Starting daily market data update for BTC...");

            try
            {
                const string symbol = "BTCUSDT";
                const string timeFrame = "1m";

                // Get the latest stored data to know where to start
                var latestData = await binanceService.GetLatestStoredDataAsync(symbol, timeFrame);
                
                DateTime startDate;
                if (latestData != null)
                {
                    // Start from the last stored data point
                    startDate = latestData.CloseTime.AddMinutes(1);
                    _logger.LogInformation("Latest stored data: {Date}, updating from {StartDate}", 
                        latestData.CloseTime, startDate);
                }
                else
                {
                    // If no data exists, start from 30 days ago (reasonable initial dataset)
                    startDate = DateTime.UtcNow.AddDays(-30);
                    _logger.LogInformation("No existing data found, starting from {StartDate}", startDate);
                }

                var endDate = DateTime.UtcNow;

                // Only update if there's actually new data to fetch
                if (startDate >= endDate)
                {
                    _logger.LogInformation("No new data to fetch - already up to date");
                    return;
                }

                // Fetch and store the data
                int recordsAdded = await binanceService.FillDataGapsAsync(symbol, timeFrame, startDate, endDate);
                
                _logger.LogInformation("Market data update completed. Added {RecordsAdded} new records for {Symbol} {TimeFrame}", 
                    recordsAdded, symbol, timeFrame);

                // Log some statistics
                var totalRecords = await GetTotalRecordsAsync(binanceService, symbol, timeFrame);
                _logger.LogInformation("Total {Symbol} {TimeFrame} records in database: {TotalRecords}", 
                    symbol, timeFrame, totalRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update market data");
                throw;
            }
        }

        private async Task<int> GetTotalRecordsAsync(IBinanceService binanceService, string symbol, string timeFrame)
        {
            try
            {
                // Get data from a very old date to count all records
                var allData = await binanceService.GetStoredDataAsync(symbol, timeFrame, 
                    DateTime.UtcNow.AddYears(-10), DateTime.UtcNow);
                return allData.Count;
            }
            catch
            {
                return 0;
            }
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MarketDataUpdateService is starting");
            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MarketDataUpdateService is stopping");
            await base.StopAsync(cancellationToken);
        }
    }
}