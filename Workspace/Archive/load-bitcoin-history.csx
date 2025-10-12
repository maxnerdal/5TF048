#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.Extensions.DependencyInjection, 8.0.0"
#r "nuget: Microsoft.Extensions.Logging, 8.0.0"
#r "nuget: Microsoft.Extensions.Logging.Console, 8.0.0"
#r "nuget: Microsoft.EntityFrameworkCore.SqlServer, 8.0.0"

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Diagnostics;

// Bitcoin Historical Data Loader - Optimized for massive datasets
// Usage: dotnet script load-bitcoin-history.csx [years]
// Example: dotnet script load-bitcoin-history.csx 3

Console.WriteLine("🚀 === Bitcoin Historical Data Loader === 🚀");
Console.WriteLine($"Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

// Parse command line arguments
var yearsBack = 1; // Default to 1 year
if (Args.Length > 0 && int.TryParse(Args[0], out int years))
{
    yearsBack = years;
}

try
{
    // Configuration
    const string connectionString = "Server=localhost;Database=MyFirstDatabase;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true";
    const string symbol = "BTCUSDT";
    const string timeFrame = "1m";
    
    // Calculate date range
    var binanceLaunchDate = new DateTime(2017, 8, 17, 0, 0, 0, DateTimeKind.Utc);
    var requestedStartDate = DateTime.UtcNow.AddYears(-yearsBack);
    var startDate = requestedStartDate > binanceLaunchDate ? requestedStartDate : binanceLaunchDate;
    var endDate = DateTime.UtcNow;
    
    var totalDays = (endDate - startDate).TotalDays;
    var estimatedRecords = (int)(totalDays * 24 * 60);
    
    Console.WriteLine($"📊 Loading {yearsBack} years of Bitcoin data");
    Console.WriteLine($"📅 Date range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
    Console.WriteLine($"⏰ Total days: {totalDays:N0}");
    Console.WriteLine($"📈 Estimated records: {estimatedRecords:N0}");
    Console.WriteLine($"💾 Estimated size: ~{estimatedRecords * 100 / 1024 / 1024:N0} MB");
    Console.WriteLine();
    
    // Ask for confirmation for large datasets
    if (yearsBack >= 2)
    {
        Console.Write($"⚠️  This will download {estimatedRecords:N0} records and may take {(estimatedRecords / 10000):N0}+ minutes. Continue? (y/N): ");
        var response = Console.ReadLine();
        if (response?.ToUpper() != "Y")
        {
            Console.WriteLine("❌ Operation cancelled by user");
            return;
        }
    }
    
    // Setup services
    var services = new ServiceCollection();
    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
    services.AddHttpClient();
    services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
    
    var provider = services.BuildServiceProvider();
    var logger = provider.GetRequiredService<ILogger<Program>>();
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    
    using var context = provider.GetRequiredService<ApplicationDbContext>();
    
    // Create optimized service for massive loads
    var binanceService = new OptimizedBinanceService(httpClientFactory.CreateClient(), context, logger);
    
    Console.WriteLine("🔍 Checking existing data in database...");
    
    // Get existing data info
    var existingCount = await context.MarketData
        .Where(m => m.Symbol == symbol && m.TimeFrame == timeFrame)
        .CountAsync();
    
    var latestData = await binanceService.GetLatestStoredDataAsync(symbol, timeFrame);
    var oldestData = await context.MarketData
        .Where(m => m.Symbol == symbol && m.TimeFrame == timeFrame)
        .OrderBy(m => m.OpenTime)
        .FirstOrDefaultAsync();
    
    if (existingCount > 0)
    {
        Console.WriteLine($"📊 Existing records: {existingCount:N0}");
        Console.WriteLine($"📅 Existing range: {oldestData?.OpenTime:yyyy-MM-dd} to {latestData?.CloseTime:yyyy-MM-dd}");
    }
    else
    {
        Console.WriteLine("📊 No existing data found - will load complete dataset");
    }
    
    Console.WriteLine();
    Console.WriteLine("🚀 Starting massive historical data load...");
    
    var stopwatch = Stopwatch.StartNew();
    var progressTimer = new Timer(ShowProgress, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    
    // Load the data with progress tracking
    int recordsAdded = await binanceService.LoadMassiveHistoricalDataAsync(symbol, timeFrame, startDate, endDate);
    
    progressTimer.Dispose();
    stopwatch.Stop();
    
    Console.WriteLine();
    Console.WriteLine($"✅ Massive load completed!");
    Console.WriteLine($"📊 Records added: {recordsAdded:N0}");
    Console.WriteLine($"⏰ Time taken: {stopwatch.Elapsed:hh\\:mm\\:ss}");
    Console.WriteLine($"🚀 Rate: {(recordsAdded / stopwatch.Elapsed.TotalMinutes):N0} records/minute");
    
    // Show final statistics
    var finalCount = await context.MarketData
        .Where(m => m.Symbol == symbol && m.TimeFrame == timeFrame)
        .CountAsync();
    
    var finalOldest = await context.MarketData
        .Where(m => m.Symbol == symbol && m.TimeFrame == timeFrame)
        .OrderBy(m => m.OpenTime)
        .FirstOrDefaultAsync();
    
    var finalLatest = await context.MarketData
        .Where(m => m.Symbol == symbol && m.TimeFrame == timeFrame)
        .OrderByDescending(m => m.CloseTime)
        .FirstOrDefaultAsync();
    
    Console.WriteLine();
    Console.WriteLine($"📈 Final database statistics:");
    Console.WriteLine($"📊 Total records: {finalCount:N0}");
    Console.WriteLine($"📅 Complete range: {finalOldest?.OpenTime:yyyy-MM-dd HH:mm} to {finalLatest?.CloseTime:yyyy-MM-dd HH:mm}");
    Console.WriteLine($"💰 Latest BTC price: ${finalLatest?.ClosePrice:N2}");
    Console.WriteLine($"💾 Estimated database size: ~{finalCount * 100 / 1024 / 1024:N0} MB");
    
    static void ShowProgress(object? state)
    {
        Console.WriteLine($"⏳ Still loading... {DateTime.Now:HH:mm:ss}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    Console.WriteLine($"Details: {ex}");
    Environment.Exit(1);
}

Console.WriteLine($"🏁 Completed at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine("=== Bitcoin Historical Data Loader Complete ===");

// Optimized Binance service for massive data loads
public class OptimizedBinanceService
{
    private readonly HttpClient _httpClient;
    private readonly ApplicationDbContext _context;
    private readonly ILogger _logger;
    private const string BaseUrl = "https://api.binance.com";
    private int _totalProcessed = 0;

    public OptimizedBinanceService(HttpClient httpClient, ApplicationDbContext context, ILogger logger)
    {
        _httpClient = httpClient;
        _context = context;
        _logger = logger;
    }

    public async Task<MarketData?> GetLatestStoredDataAsync(string symbol, string timeFrame)
    {
        return await _context.MarketData
            .Where(m => m.Symbol == symbol && m.TimeFrame == timeFrame)
            .OrderByDescending(m => m.OpenTime)
            .FirstOrDefaultAsync();
    }

    public async Task<int> LoadMassiveHistoricalDataAsync(string symbol, string timeFrame, DateTime startDate, DateTime endDate)
    {
        var totalStored = 0;
        var currentStart = startDate;
        const int batchSize = 1000;
        const int batchDelayMs = 50; // Faster for massive loads, but still respectful

        Console.WriteLine($"📦 Processing in batches of {batchSize} records with {batchDelayMs}ms delays");

        while (currentStart < endDate)
        {
            var currentEnd = currentStart.AddMinutes(batchSize - 1);
            if (currentEnd > endDate) currentEnd = endDate;

            try
            {
                var data = await GetHistoricalDataAsync(symbol, timeFrame, currentStart, currentEnd, batchSize);
                if (data.Any())
                {
                    var added = await StoreMarketDataBatchAsync(data);
                    totalStored += added;
                    _totalProcessed += data.Count;

                    // Progress indicator
                    if (_totalProcessed % 10000 == 0)
                    {
                        var progress = (currentStart - startDate).TotalDays / (endDate - startDate).TotalDays * 100;
                        Console.WriteLine($"📈 Progress: {progress:F1}% | Processed: {_totalProcessed:N0} | Added: {totalStored:N0} | Current: {currentStart:yyyy-MM-dd}");
                    }
                }

                currentStart = currentEnd.AddMinutes(1);
                
                // Rate limiting
                if (batchDelayMs > 0)
                    await Task.Delay(batchDelayMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch from {Start} to {End}", currentStart, currentEnd);
                
                // On error, wait longer and continue
                await Task.Delay(5000);
                currentStart = currentStart.AddMinutes(batchSize);
            }
        }

        return totalStored;
    }

    private async Task<List<MarketData>> GetHistoricalDataAsync(string symbol, string timeFrame, DateTime startDate, DateTime endDate, int limit)
    {
        var startTimestamp = ((DateTimeOffset)startDate).ToUnixTimeMilliseconds();
        var endTimestamp = ((DateTimeOffset)endDate).ToUnixTimeMilliseconds();
        
        var url = $"{BaseUrl}/api/v3/klines?symbol={symbol}&interval={timeFrame}&startTime={startTimestamp}&endTime={endTimestamp}&limit={limit}";
        
        var response = await _httpClient.GetStringAsync(url);
        var jsonData = JsonDocument.Parse(response);
        
        var marketDataList = new List<MarketData>();
        
        foreach (var kline in jsonData.RootElement.EnumerateArray())
        {
            var marketData = MarketData.FromBinanceKline(kline, symbol, timeFrame);
            marketDataList.Add(marketData);
        }
        
        return marketDataList;
    }

    private async Task<int> StoreMarketDataBatchAsync(List<MarketData> marketData)
    {
        if (!marketData.Any()) return 0;

        // Optimized batch insert - check for duplicates in larger batches
        var startTime = marketData.Min(m => m.OpenTime);
        var endTime = marketData.Max(m => m.OpenTime);

        var existingTimes = await _context.MarketData
            .Where(m => m.Symbol == marketData.First().Symbol && 
                       m.TimeFrame == marketData.First().TimeFrame && 
                       m.OpenTime >= startTime && 
                       m.OpenTime <= endTime)
            .Select(m => m.OpenTime)
            .ToListAsync();

        var newData = marketData
            .Where(m => !existingTimes.Contains(m.OpenTime))
            .ToList();

        if (newData.Any())
        {
            await _context.MarketData.AddRangeAsync(newData);
            await _context.SaveChangesAsync();
        }

        return newData.Count;
    }
}

// You'll need to reference your MarketData and ApplicationDbContext classes
// This script assumes they are available in your project assembly