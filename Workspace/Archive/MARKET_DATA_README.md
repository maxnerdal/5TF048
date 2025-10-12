# Market Data Update System

This system provides both automated and manual ways to update historical BTC price data for trading analysis.

## Features

### 🤖 Automated Daily Updates
- **Background Service**: Automatically runs daily to fetch latest BTC data
- **Gap Filling**: Intelligently fills missing data periods
- **Rate Limited**: Respects Binance API limits (100ms between requests)
- **Duplicate Prevention**: Won't store the same data twice

### 🖱️ Manual Web Interface
- **Web Dashboard**: Visit `/MarketData` to manage data manually
- **Real-time Statistics**: Shows current data status and coverage
- **One-click Updates**: Buttons for different update scenarios
- **Progress Tracking**: Visual feedback on update operations

### 📜 Command Line Scripts
- **Shell Script**: `./Scripts/update-btc-data.sh` (calls web API)
- **Direct Database**: Uses your existing application and database

## Quick Start

### 1. First-time Setup
```bash
# 1. Make sure your database tables exist
# Run the SQL scripts in DatabaseScripts/ if you haven't already

# 2. Start your application
dotnet run

# 3. Visit the management page
# Open http://localhost:5275/MarketData in your browser

# 4. Click "Load Initial Data (30 days)" to get started
```

### 2. Regular Updates

#### Option A: Automatic (Recommended)
- The background service runs automatically when your application starts
- Updates happen daily without any action needed
- Check logs for update status

#### Option B: Manual Web Interface
```bash
# 1. Start your application
dotnet run

# 2. Open browser to http://localhost:5275/MarketData

# 3. Click "Update Latest Data" to get newest data
```

#### Option C: Command Line Script
```bash
# Make sure your application is running first
dotnet run &

# Then run the update script
./Scripts/update-btc-data.sh
```

## Data Storage

### Database Schema
- **Table**: `MarketData`
- **Data**: BTC minute-level OHLCV candlesticks
- **Source**: Binance Public API (BTCUSDT)
- **Timezone**: UTC
- **Precision**: 8 decimal places

### Storage Estimates
- **1 day**: ~1,440 records (~144 KB)
- **1 month**: ~43,200 records (~4 MB)
- **1 year**: ~525,600 records (~50 MB)

## Configuration

### Timeframes Supported
- Currently configured for **1-minute** data
- Easy to extend to other timeframes (5m, 15m, 1h, 4h, 1d)

### API Limits
- **Rate Limit**: 100ms between requests (well under Binance limits)
- **Batch Size**: 1,000 records per request (Binance maximum)
- **No Authentication**: Uses public endpoints only (read-only)

### Background Service Settings
- **Update Frequency**: Every 24 hours
- **Startup Delay**: 2 minutes (allows app to fully initialize)
- **Error Retry**: 1 hour delay on failures

## Monitoring

### Logs
The system provides detailed logging:
```
[12:00:00 INF] MarketDataUpdateService starting up...
[12:02:00 INF] Starting daily market data update for BTC...
[12:02:01 INF] Latest stored data: 2024-10-09 11:59:00, updating from 2024-10-09 12:00:00
[12:02:15 INF] Market data update completed. Added 1440 new records for BTCUSDT 1m
[12:02:15 INF] Total BTCUSDT 1m records in database: 43,200
```

### Web Dashboard
Visit `/MarketData` to see:
- ✅ Current data status and coverage
- 📊 Total records and date ranges  
- 💰 Latest BTC price
- ⏰ How recent your data is
- 🔧 Manual update controls

## Troubleshooting

### "No data available"
1. Run SQL scripts in `DatabaseScripts/` to create tables
2. Use "Load Initial Data" button to fetch starting dataset

### Background service not running
1. Check application logs for errors
2. Verify database connection string
3. Ensure MarketDataUpdateService is registered in Program.cs

### API rate limit errors
- The system includes built-in rate limiting
- If issues persist, increase delay in BinanceService.cs

### Database connection issues
1. Check connection string in appsettings.json
2. Verify SQL Server is running
3. Ensure database exists and tables are created

## Files Created

- `Services/MarketDataUpdateService.cs` - Background service
- `Controllers/MarketDataController.cs` - Web interface  
- `Views/MarketData/Index.cshtml` - Management dashboard
- `Models/MarketDataManagementViewModel.cs` - View model
- `Scripts/update-btc-data.sh` - Command line script

## Next Steps

1. **Test the system**: Load initial data and verify storage
2. **Monitor logs**: Watch the background service in action
3. **Extend symbols**: Add ETH, ADA, or other cryptocurrencies
4. **Add timeframes**: Include 5m, 1h, 4h, 1d data for different analysis
5. **Build trading strategies**: Use the stored data for backtesting