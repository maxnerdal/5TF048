@echo off
REM =============================================
REM CryptoBot Database Setup Script (Windows)
REM Run this script to automatically set up the database on a new Windows computer
REM =============================================

echo 🚀 CryptoBot Database Migration Script (Windows)
echo ======================================

REM Check if Docker is running
docker info >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Error: Docker is not running. Please start Docker Desktop first.
    pause
    exit /b 1
)

echo ✅ Docker is running

REM Check if SQL Server container exists and is running
docker ps --format "table {{.Names}}" | findstr /C:"sqlserver-crypto" >nul
if %errorlevel% equ 0 (
    echo ✅ SQL Server container is already running
) else (
    docker ps -a --format "table {{.Names}}" | findstr /C:"sqlserver-crypto" >nul
    if %errorlevel% equ 0 (
        echo ⚠️  SQL Server container exists but is stopped. Starting...
        docker start sqlserver-crypto
        timeout /t 10 /nobreak >nul
    ) else (
        echo ⚠️  SQL Server container doesn't exist. Creating with docker-compose...
        if exist "docker-compose.yml" (
            docker-compose up -d sqlserver
            echo ✅ SQL Server container started
            echo ⏳ Waiting 30 seconds for SQL Server to initialize...
            timeout /t 30 /nobreak >nul
        ) else (
            echo ❌ Error: docker-compose.yml not found. Make sure you're in the project directory.
            pause
            exit /b 1
        )
    )
)

REM Wait for SQL Server to be ready
echo ⏳ Checking if SQL Server is ready...
set /a attempts=0
:check_sql
set /a attempts+=1
docker exec sqlserver-crypto /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "MyPassword123#" -Q "SELECT 1" >nul 2>&1
if %errorlevel% equ 0 (
    echo ✅ SQL Server is ready
    goto sql_ready
)

echo    Attempt %attempts%/10: SQL Server not ready yet, waiting...
timeout /t 5 /nobreak >nul

if %attempts% geq 10 (
    echo ❌ Error: SQL Server failed to start properly
    echo Check container logs with: docker logs sqlserver-crypto
    pause
    exit /b 1
)
goto check_sql

:sql_ready

REM Run the database migration script
echo 🗄️  Running database migration script...
if exist "DatabaseScripts\00_CompleteDbMigration.sql" (
    docker exec -i sqlserver-crypto /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "MyPassword123#" < DatabaseScripts\00_CompleteDbMigration.sql
    
    if %errorlevel% equ 0 (
        echo ✅ Database migration completed successfully!
    ) else (
        echo ❌ Error: Database migration failed
        pause
        exit /b 1
    )
) else (
    echo ❌ Error: Migration script not found at DatabaseScripts\00_CompleteDbMigration.sql
    pause
    exit /b 1
)

REM Test the application build
echo 🔨 Testing application build...
dotnet build >nul 2>&1
if %errorlevel% equ 0 (
    echo ✅ Application builds successfully
) else (
    echo ⚠️  Application build failed. You may need to run 'dotnet restore' first
)

echo.
echo 🎉 Setup Complete!
echo ======================================
echo ✅ SQL Server is running on localhost:1433
echo ✅ Database 'MyFirstDatabase' is created and configured
echo ✅ All tables, indexes, and sample data are set up
echo.
echo 📋 Next steps:
echo 1. Run your application: dotnet run
echo 2. Open browser at: http://localhost:5275
echo 3. Connect Azure Data Studio to: localhost,1433
echo    Username: sa
echo    Password: MyPassword123#
echo.
echo 🧪 Test login credentials:
echo    Username: testuser
echo    Password: password123
echo.

pause