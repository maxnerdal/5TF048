# 📋 Project TODO List

## 🎯 Current Status: Setting Up Multi-Laptop Development

### ✅ Completed Tasks
- [x] ASP.NET Core MVC project setup with Visual Studio Code
- [x] Docker Compose configuration for SQL Server
- [x] User authentication system (AccountController)
- [x] Portfolio management (CRUD operations)
- [x] Real-time crypto market data (CoinGecko API integration)
- [x] Bitcoin price display
- [x] MarketData model and database structure
- [x] Repository cloned on new laptop

### 🚀 Next Steps - New Laptop Setup

#### Phase 1: Environment Setup
- [ ] Install Docker Desktop on new laptop
- [ ] Install Visual Studio Code on new laptop  
- [ ] Install Azure Data Studio on new laptop
- [ ] Install .NET SDK on new laptop

#### Phase 2: Database Initialization
- [ ] Navigate to project directory: `cd 5TF048/Workspace`
- [ ] Start Docker services: `docker-compose up -d`
- [ ] Verify SQL Server container is running: `docker ps`
- [ ] Create database: `CREATE DATABASE MyFirstDatabase`
- [ ] Run database scripts:
  - [ ] Execute `DatabaseScripts/01_CreateTables.sql`
  - [ ] Execute `DatabaseScripts/02_CreateIndexes.sql`

#### Phase 3: Application Testing
- [ ] Test application build: `dotnet build`
- [ ] Run application: `dotnet run`
- [ ] Verify application at: `http://localhost:5275`
- [ ] Test login/registration functionality
- [ ] Verify portfolio features work
- [ ] Check crypto market data display

#### Phase 4: Database Access Verification
- [ ] Connect Azure Data Studio to `localhost,1433`
- [ ] Verify database structure matches original setup
- [ ] Test CRUD operations through application

### 🎯 Bitcoin Trading Bot Development (Future)

#### Phase 1: Historical Data Foundation
- [ ] Complete Python virtual environment setup: `python3 -m venv bitcoin_env`
- [ ] Install Python dependencies: `pip install pyodbc pandas requests`
- [ ] Configure FreeTDS ODBC driver for macOS
- [ ] Test database connection from Python
- [ ] Create Step 2: Bitcoin API data fetching script
- [ ] Implement historical data import functionality

#### Phase 2: Trading Strategy Engine
- [ ] Design backtesting framework
- [ ] Implement basic trading strategies (Moving Average, RSI)
- [ ] Create paper trading simulation
- [ ] Build performance analytics dashboard

#### Phase 3: Advanced Features  
- [ ] Real-time data streaming
- [ ] Multiple trading strategies
- [ ] Risk management system
- [ ] Email notifications for trades

### 🐛 Known Issues & Fixes
- [x] Fixed: Edit/Delete portfolio items (model binding issue)
- [x] Fixed: Password change functionality (UpdatePasswordAsync implementation)
- [x] Fixed: CoinGecko API integration (Demo API key configuration)
- [x] Fixed: Removed unstable background services

### 📝 Development Notes
- **Database**: SQL Server 2022 Express in Docker container
- **Authentication**: Custom implementation with SHA256 hashing
- **APIs**: CoinGecko for market data (Demo tier, 10K calls/month)
- **Architecture**: ASP.NET Core MVC with Entity Framework
- **Deployment**: Docker Compose for local development

### 🔧 Quick Reference Commands

#### Docker Management
```bash
# Start services
docker-compose up -d

# Stop services  
docker-compose down

# View logs
docker logs sqlserver-crypto

# Connect to SQL Server
docker exec -it sqlserver-crypto /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'MyPassword123#'
```

#### Application Management
```bash
# Build application
dotnet build

# Run application
dotnet run

# Restore packages
dotnet restore
```

#### Database Connection (Azure Data Studio)
- **Server**: `localhost,1433`
- **Authentication**: SQL Server Authentication
- **Username**: `sa`
- **Password**: `MyPassword123#`
- **Database**: `MyFirstDatabase`

---
**Last Updated**: October 2024  
**Current Branch**: `main`  
**Setup Status**: Ready for new laptop deployment