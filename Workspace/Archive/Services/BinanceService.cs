using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Services
{
    /// <summary>
    /// Interface for Binance API service - read-only historical data fetching
    /// </summary>
    public interface IBinanceService
    {
        /// <summary>
        /// Fetch historical candlestick data from Binance API
        /// </summary>
        Task<List<MarketData>> GetHistoricalDataAsync(string symbol, string timeFrame, DateTime startDate, DateTime endDate, int limit = 1000);

        /// <summary>
        /// Store market data in the database
        /// </summary>
        Task<int> StoreMarketDataAsync(List<MarketData> marketData);

        /// <summary>
        /// Get the latest stored data for a symbol and timeframe
        /// </summary>
        Task<MarketData?> GetLatestStoredDataAsync(string symbol, string timeFrame);

        /// <summary>
        /// Get historical data from database for backtesting
        /// </summary>
        Task<List<MarketData>> GetStoredDataAsync(string symbol, string timeFrame, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Update database with missing data gaps
        /// </summary>
        Task<int> FillDataGapsAsync(string symbol, string timeFrame, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get available symbols from the database
        /// </summary>
        Task<List<string>> GetAvailableSymbolsAsync();
    }

    /// <summary>
    /// Binance API service for fetching historical market data (read-only, no trading)
    /// Uses Binance public API endpoints that don't require authentication
    /// </summary>
    public class BinanceService : IBinanceService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BinanceService> _logger;
        private readonly IConfiguration _configuration;

        // Binance API endpoints
        private readonly string _baseUrl = "https://api.binance.com/api/v3";

        public BinanceService(HttpClient httpClient, ApplicationDbContext context, ILogger<BinanceService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _context = context;
            _logger = logger;
            _configuration = configuration;

            // Configure HttpClient
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "CryptoTradingBot/1.0");
        }

        /// <summary>
        /// Fetch historical candlestick data from Binance API
        /// </summary>
        public async Task<List<MarketData>> GetHistoricalDataAsync(string symbol, string timeFrame, DateTime startDate, DateTime endDate, int limit = 1000)
        {
            try
            {
                _logger.LogInformation("Fetching historical data for {Symbol} {TimeFrame} from {Start} to {End}", 
                    symbol, timeFrame, startDate, endDate);

                var marketDataList = new List<MarketData>();
                var currentStart = startDate;

                // Binance API has a limit of 1000 candles per request, so we may need multiple requests
                while (currentStart < endDate)
                {
                    var startTimestamp = ((DateTimeOffset)currentStart).ToUnixTimeMilliseconds();
                    var endTimestamp = ((DateTimeOffset)endDate).ToUnixTimeMilliseconds();

                    var url = $"{_baseUrl}/klines?symbol={symbol}&interval={timeFrame}&startTime={startTimestamp}&endTime={endTimestamp}&limit={limit}";
                    
                    _logger.LogDebug("Binance API request: {Url}", url);

                    var response = await _httpClient.GetStringAsync(url);
                    var klines = JsonSerializer.Deserialize<object[][]>(response);

                    if (klines == null || klines.Length == 0)
                    {
                        _logger.LogWarning("No data returned from Binance API for {Symbol} {TimeFrame}", symbol, timeFrame);
                        break;
                    }

                    // Convert Binance kline data to MarketData objects
                    foreach (var kline in klines)
                    {
                        try
                        {
                            var marketData = MarketData.FromBinanceKline(kline, symbol, timeFrame);
                            
                            // Only add data within our requested range
                            if (marketData.OpenTime >= startDate && marketData.OpenTime <= endDate)
                            {
                                marketDataList.Add(marketData);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse kline data: {Kline}", JsonSerializer.Serialize(kline));
                        }
                    }

                    // If we got less than the limit, we've reached the end
                    if (klines.Length < limit)
                        break;

                    // Move to the next batch - use the last candle's close time + 1ms
                    var lastCandle = klines.Last();
                    var lastCloseTime = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(lastCandle[6])).DateTime;
                    currentStart = lastCloseTime.AddMilliseconds(1);

                    // Add a small delay to respect API rate limits
                    await Task.Delay(100);
                }

                _logger.LogInformation("Fetched {Count} candles for {Symbol} {TimeFrame}", marketDataList.Count, symbol, timeFrame);
                return marketDataList.OrderBy(m => m.OpenTime).ToList();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error fetching data from Binance API");
                throw new Exception($"Failed to fetch data from Binance: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching historical data from Binance");
                throw;
            }
        }

        /// <summary>
        /// Store market data in the database
        /// </summary>
        public async Task<int> StoreMarketDataAsync(List<MarketData> marketData)
        {
            if (!marketData.Any())
                return 0;

            try
            {
                var symbol = marketData.First().Symbol;
                var timeFrame = marketData.First().TimeFrame;

                _logger.LogInformation("Storing {Count} candles for {Symbol} {TimeFrame}", marketData.Count, symbol, timeFrame);

                // Get existing data to avoid duplicates
                var startTime = marketData.Min(m => m.OpenTime);
                var endTime = marketData.Max(m => m.OpenTime);

                var existingData = await _context.MarketData
                    .Where(m => m.Symbol == symbol && 
                               m.TimeFrame == timeFrame && 
                               m.OpenTime >= startTime && 
                               m.OpenTime <= endTime)
                    .Select(m => m.OpenTime)
                    .ToListAsync();

                // Filter out duplicates
                var newData = marketData
                    .Where(m => !existingData.Contains(m.OpenTime))
                    .ToList();

                if (!newData.Any())
                {
                    _logger.LogInformation("No new data to store - all candles already exist");
                    return 0;
                }

                // Add new data to the database
                await _context.MarketData.AddRangeAsync(newData);
                var saved = await _context.SaveChangesAsync();

                _logger.LogInformation("Stored {Count} new candles for {Symbol} {TimeFrame}", saved, symbol, timeFrame);
                return saved;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing market data");
                throw;
            }
        }

        /// <summary>
        /// Get the latest stored data for a symbol and timeframe
        /// </summary>
        public async Task<MarketData?> GetLatestStoredDataAsync(string symbol, string timeFrame)
        {
            return await _context.MarketData
                .Where(m => m.Symbol == symbol && m.TimeFrame == timeFrame)
                .OrderByDescending(m => m.OpenTime)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Get historical data from database for backtesting
        /// </summary>
        public async Task<List<MarketData>> GetStoredDataAsync(string symbol, string timeFrame, DateTime startDate, DateTime endDate)
        {
            return await _context.MarketData
                .Where(m => m.Symbol == symbol && 
                           m.TimeFrame == timeFrame && 
                           m.OpenTime >= startDate && 
                           m.OpenTime <= endDate)
                .OrderBy(m => m.OpenTime)
                .ToListAsync();
        }

        /// <summary>
        /// Update database with missing data gaps
        /// </summary>
        public async Task<int> FillDataGapsAsync(string symbol, string timeFrame, DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Filling data gaps for {Symbol} {TimeFrame} from {Start} to {End}", 
                    symbol, timeFrame, startDate, endDate);

                // Get existing data in the range
                var existingData = await GetStoredDataAsync(symbol, timeFrame, startDate, endDate);
                
                if (!existingData.Any())
                {
                    // No existing data, fetch everything
                    var allData = await GetHistoricalDataAsync(symbol, timeFrame, startDate, endDate);
                    return await StoreMarketDataAsync(allData);
                }

                var totalStored = 0;

                // Check for gap at the beginning
                var firstExisting = existingData.First().OpenTime;
                if (startDate < firstExisting)
                {
                    var earlyData = await GetHistoricalDataAsync(symbol, timeFrame, startDate, firstExisting.AddSeconds(-1));
                    totalStored += await StoreMarketDataAsync(earlyData);
                }

                // Check for gap at the end
                var lastExisting = existingData.Last().OpenTime;
                if (endDate > lastExisting)
                {
                    var lateData = await GetHistoricalDataAsync(symbol, timeFrame, lastExisting.AddSeconds(1), endDate);
                    totalStored += await StoreMarketDataAsync(lateData);
                }

                // TODO: Check for gaps in the middle (more complex logic needed)

                _logger.LogInformation("Filled gaps: stored {Count} new candles", totalStored);
                return totalStored;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filling data gaps");
                throw;
            }
        }

        /// <summary>
        /// Get available symbols from the database
        /// </summary>
        public async Task<List<string>> GetAvailableSymbolsAsync()
        {
            return await _context.MarketData
                .Select(m => m.Symbol)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();
        }
    }
}