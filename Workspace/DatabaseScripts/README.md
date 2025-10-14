# 🗄️ Database Migration Guide

This guide helps you recreate your complete database structure on any new computer.

## 📋 What's Included

- **Complete database schema** (Users, Portfolios, DigitalAssets, MarketData)
- **All indexes** for optimal performance  
- **Foreign key constraints** for data integrity
- **Sample data** including test user and digital assets
- **Automated setup scripts** for easy deployment

## 🚀 Quick Setup (Automated)

### For macOS/Linux:
```bash
chmod +x setup-database.sh
./setup-database.sh
```

### For Windows:
```cmd
setup-database.bat
```

## 🔧 Manual Setup

If you prefer to run steps manually:

### 1. Start SQL Server Container
```bash
docker-compose up -d
```

### 2. Run Migration Script
```bash
# Using Docker exec
docker exec -i sqlserver-crypto /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'MyPassword123#' \
  < DatabaseScripts/00_CompleteDbMigration.sql

# Or connect with Azure Data Studio and run the script
```

### 3. Verify Setup
```bash
dotnet build
dotnet run
```

## 📊 Database Structure

### Tables Created:
- **Users** - User authentication and profiles
- **DigitalAssets** - Cryptocurrency definitions (BTC, ETH, etc.)
- **Portfolios** - User portfolios
- **PortfolioItems** - Individual holdings in portfolios
- **MarketData** - Historical price data (OHLCV)

### Sample Data:
- **10 popular cryptocurrencies** (BTC, ETH, ADA, DOT, LINK, SOL, AVAX, MATIC, UNI, AAVE)
- **Test user account**:
  - Username: `testuser`
  - Password: `password123`
  - Email: `test@example.com`

## 🔗 Database Connection

After setup, connect using:
- **Server**: `localhost,1433`
- **Authentication**: SQL Server Authentication
- **Username**: `sa`
- **Password**: `MyPassword123#`
- **Database**: `MyFirstDatabase`

## ✅ Verification

The script will show table record counts at the end:
```
Users: 1 record (test user)
DigitalAssets: 10 records (cryptocurrencies)
Portfolios: 1 record (sample portfolio)
PortfolioItems: 0 records (empty initially)
MarketData: 0 records (populated by Bitcoin importer)
```

## 🐛 Troubleshooting

### SQL Server won't start:
```bash
# Check container logs
docker logs sqlserver-crypto

# Restart container
docker-compose restart sqlserver
```

### Migration script fails:
- Ensure SQL Server is fully started (wait 30+ seconds)
- Check if database already exists
- Verify file path: `DatabaseScripts/00_CompleteDbMigration.sql`

### Application won't connect:
- Verify connection string in `appsettings.json`
- Check if port 1433 is accessible
- Ensure container is running: `docker ps`

## 📁 Files

- `DatabaseScripts/00_CompleteDbMigration.sql` - Main migration script
- `setup-database.sh` - Automated setup for macOS/Linux
- `setup-database.bat` - Automated setup for Windows
- `docker-compose.yml` - Container configuration

## 🎯 Next Steps

After successful migration:
1. **Run application**: `dotnet run`
2. **Test login** with test user credentials
3. **Connect Azure Data Studio** for database management
4. **Run Bitcoin data importer** to populate MarketData table