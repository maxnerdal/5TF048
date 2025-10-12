#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.Extensions.DependencyInjection, 8.0.0"
#r "nuget: Microsoft.Extensions.Logging, 8.0.0"
#r "nuget: Microsoft.Extensions.Logging.Console, 8.0.0"
#r "nuget: Microsoft.EntityFrameworkCore.SqlServer, 8.0.0"

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

// This script can be run with: dotnet script update-market-data.csx

Console.WriteLine("=== BTC Market Data Update Script ===");
Console.WriteLine($"Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

try
{
    // Configuration
    const string connectionString = "Server=localhost;Database=MyFirstDatabase;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true";
    const string symbol = "BTCUSDT";
    const string timeFrame = "1m";
    
    // Setup services
    var services = new ServiceCollection();
    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
    services.AddHttpClient();
    services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
    
    var provider = services.BuildServiceProvider();
    var logger = provider.GetRequiredService<ILogger<Program>>();
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    
    using var context = provider.GetRequiredService<ApplicationDbContext>();
    
    // Simple BinanceService implementation for script
    var binanceService = new SimpleBinanceService(httpClientFactory.CreateClient(), context, logger);
    
    Console.WriteLine($"Checking for latest {symbol} {timeFrame} data in database...");
    
    // Get latest data to know where to start
    var latestData = await binanceService.GetLatestStoredDataAsync(symbol, timeFrame);
    
    DateTime startDate;
    if (latestData != null)
    {
        startDate = latestData.CloseTime.AddMinutes(1);
        Console.WriteLine($"Latest data found: {latestData.CloseTime:yyyy-MM-dd HH:mm} UTC");
        Console.WriteLine($"Will update from: {startDate:yyyy-MM-dd HH:mm} UTC");
    }
    else
    {
        // Start from 30 days ago for initial load
        startDate = DateTime.UtcNow.AddDays(-30);
        Console.WriteLine("No existing data found");
        Console.WriteLine($"Will load initial data from: {startDate:yyyy-MM-dd HH:mm} UTC");
    }
    
    var endDate = DateTime.UtcNow;
    
    if (startDate >= endDate)
    {
        Console.WriteLine("✅ Data is already up to date!");
        return;
    }
    
    Console.WriteLine($"Fetching data from {startDate:yyyy-MM-dd HH:mm} to {endDate:yyyy-MM-dd HH:mm}...");
    
    // Fetch and store the data
    int recordsAdded = await binanceService.FillDataGapsAsync(symbol, timeFrame, startDate, endDate);
    
    Console.WriteLine($"✅ Update completed! Added {recordsAdded} new records");
    
    // Show statistics
    var allData = await binanceService.GetStoredDataAsync(symbol, timeFrame, DateTime.UtcNow.AddYears(-10), DateTime.UtcNow);
    Console.WriteLine($"📊 Total {symbol} {timeFrame} records in database: {allData.Count:N0}");
    
    if (allData.Any())
    {
        var oldest = allData.OrderBy(x => x.OpenTime).First();
        var newest = allData.OrderByDescending(x => x.CloseTime).First();
        Console.WriteLine($"📅 Data range: {oldest.OpenTime:yyyy-MM-dd HH:mm} to {newest.CloseTime:yyyy-MM-dd HH:mm}");
        Console.WriteLine($"💰 Latest BTC price: ${newest.ClosePrice:N2}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    Console.WriteLine($"Details: {ex}");
    Environment.Exit(1);
}

Console.WriteLine($"Completed at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine("=== Script finished ===");

// Simplified BinanceService for the script
public class SimpleBinanceService
{
    private readonly HttpClient _httpClient;
    private readonly ApplicationDbContext _context;
    private readonly ILogger _logger;
    private const string BaseUrl = "https://api.binance.com";

    public SimpleBinanceService(HttpClient httpClient, ApplicationDbContext context, ILogger logger)
    {
        _httpClient = httpClient;
        _context = context;
        _logger = logger;
    }

    public async Task<MarketData?> GetLatestStoredDataAsync(string symbol, string timeFrame)
    {
        return await _context.MarketData
            .Where(m => m.Symbol == symbol && m.TimeFrame == timeFrame)
            .OrderByDescending(m => m.CloseTime)
            .FirstOrDefaultAsync();
    }

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

    public async Task<int> FillDataGapsAsync(string symbol, string timeFrame, DateTime startDate, DateTime endDate)
    {
        var totalAdded = 0;
        var currentStart = startDate;
        const int maxLimit = 1000;

        while (currentStart < endDate)
        {
            var currentEnd = currentStart.AddMinutes(maxLimit);
            if (currentEnd > endDate) currentEnd = endDate;

            var data = await GetHistoricalDataAsync(symbol, timeFrame, currentStart, currentEnd, maxLimit);
            if (data.Any())
            {
                var added = await StoreMarketDataAsync(data);
                totalAdded += added;
                Console.WriteLine($"Added {added} records for period {currentStart:MM-dd HH:mm} to {currentEnd:MM-dd HH:mm}");
            }

            currentStart = currentEnd.AddMinutes(1);
            
            // Rate limiting
            await Task.Delay(100);
        }

        return totalAdded;
    }

    public async Task<List<MarketData>> GetHistoricalDataAsync(string symbol, string timeFrame, DateTime startDate, DateTime endDate, int limit = 1000)
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

    public async Task<int> StoreMarketDataAsync(List<MarketData> marketData)
    {
        if (!marketData.Any()) return 0;

        var addedCount = 0;
        foreach (var data in marketData)
        {
            var exists = await _context.MarketData
                .AnyAsync(m => m.Symbol == data.Symbol && 
                              m.TimeFrame == data.TimeFrame && 
                              m.OpenTime == data.OpenTime);
            
            if (!exists)
            {
                _context.MarketData.Add(data);
                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            await _context.SaveChangesAsync();
        }

        return addedCount;
    }
}

// You'll need to include your MarketData and ApplicationDbContext classes here
// or reference the main project assembly