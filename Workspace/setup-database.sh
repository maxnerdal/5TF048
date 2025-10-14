#!/bin/bash

# =============================================
# CryptoBot Database Setup Script
# Run this script to automatically set up the database on a new computer
# =============================================

echo "🚀 CryptoBot Database Migration Script"
echo "======================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}❌ Error: Docker is not running. Please start Docker Desktop first.${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Docker is running${NC}"

# Check if SQL Server container exists and is running
if docker ps --format 'table {{.Names}}' | grep -q "sqlserver-crypto"; then
    echo -e "${GREEN}✅ SQL Server container is already running${NC}"
elif docker ps -a --format 'table {{.Names}}' | grep -q "sqlserver-crypto"; then
    echo -e "${YELLOW}⚠️  SQL Server container exists but is stopped. Starting...${NC}"
    docker start sqlserver-crypto
    sleep 10
else
    echo -e "${YELLOW}⚠️  SQL Server container doesn't exist. Creating with docker-compose...${NC}"
    if [ -f "docker-compose.yml" ]; then
        docker-compose up -d sqlserver
        echo -e "${GREEN}✅ SQL Server container started${NC}"
        echo "⏳ Waiting 30 seconds for SQL Server to initialize..."
        sleep 30
    else
        echo -e "${RED}❌ Error: docker-compose.yml not found. Make sure you're in the project directory.${NC}"
        exit 1
    fi
fi

# Wait for SQL Server to be ready
echo "⏳ Checking if SQL Server is ready..."
for i in {1..10}; do
    if docker exec sqlserver-crypto /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'MyPassword123#' -Q "SELECT 1" > /dev/null 2>&1; then
        echo -e "${GREEN}✅ SQL Server is ready${NC}"
        break
    else
        echo "   Attempt $i/10: SQL Server not ready yet, waiting..."
        sleep 5
    fi
    
    if [ $i -eq 10 ]; then
        echo -e "${RED}❌ Error: SQL Server failed to start properly${NC}"
        echo "Check container logs with: docker logs sqlserver-crypto"
        exit 1
    fi
done

# Run the database migration script
echo "🗄️  Running database migration script..."
if [ -f "DatabaseScripts/00_CompleteDbMigration.sql" ]; then
    docker exec -i sqlserver-crypto /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'MyPassword123#' < DatabaseScripts/00_CompleteDbMigration.sql
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Database migration completed successfully!${NC}"
    else
        echo -e "${RED}❌ Error: Database migration failed${NC}"
        exit 1
    fi
else
    echo -e "${RED}❌ Error: Migration script not found at DatabaseScripts/00_CompleteDbMigration.sql${NC}"
    exit 1
fi

# Test the application build
echo "🔨 Testing application build..."
if dotnet build > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Application builds successfully${NC}"
else
    echo -e "${YELLOW}⚠️  Application build failed. You may need to run 'dotnet restore' first${NC}"
fi

echo ""
echo -e "${GREEN}🎉 Setup Complete!${NC}"
echo "======================================"
echo "✅ SQL Server is running on localhost:1433"
echo "✅ Database 'MyFirstDatabase' is created and configured"
echo "✅ All tables, indexes, and sample data are set up"
echo ""
echo "📋 Next steps:"
echo "1. Run your application: ${YELLOW}dotnet run${NC}"
echo "2. Open browser at: ${YELLOW}http://localhost:5275${NC}"
echo "3. Connect Azure Data Studio to: ${YELLOW}localhost,1433${NC}"
echo "   Username: ${YELLOW}sa${NC}"
echo "   Password: ${YELLOW}MyPassword123#${NC}"
echo ""
echo "🧪 Test login credentials:"
echo "   Username: ${YELLOW}testuser${NC}"
echo "   Password: ${YELLOW}password123${NC}"
echo ""