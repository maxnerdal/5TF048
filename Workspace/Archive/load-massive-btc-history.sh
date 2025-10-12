#!/bin/bash

# Bitcoin Historical Data Loader Script
# Loads massive amounts of historical Bitcoin data efficiently

echo "🚀 === Bitcoin Historical Data Loader === 🚀"
echo "This script will load historical Bitcoin minute data into your database."
echo ""

# Check if application database is ready
echo "📋 Pre-flight checks:"

# Check if SQL Server is running
if ! command -v sqlcmd &> /dev/null; then
    echo "❌ SQL Server tools not found. Please install SQL Server."
    exit 1
fi

echo "✅ SQL Server tools found"

# Configuration
DATABASE="MyFirstDatabase"
APP_URL="http://localhost:5275"

# Default to 1 year if no argument provided
YEARS=${1:-1}

echo "📊 Configuration:"
echo "   Years to load: $YEARS"
echo "   Database: $DATABASE"
echo "   App URL: $APP_URL"
echo ""

# Estimate the data size
case $YEARS in
    1)
        RECORDS="~525,000"
        TIME="5-15 minutes"
        SIZE="~50 MB"
        ;;
    2)
        RECORDS="~1,000,000"
        TIME="15-30 minutes"
        SIZE="~100 MB"
        ;;
    3)
        RECORDS="~1,500,000"
        TIME="30-45 minutes"
        SIZE="~150 MB"
        ;;
    5)
        RECORDS="~2,600,000"
        TIME="1-2 hours"
        SIZE="~250 MB"
        ;;
    *)
        RECORDS="~${YEARS}00,000+"
        TIME="2+ hours"
        SIZE="~$((YEARS * 50)) MB"
        ;;
esac

echo "📈 Estimated load:"
echo "   Records: $RECORDS"
echo "   Time: $TIME"
echo "   Database size: $SIZE"
echo ""

# Confirmation for large datasets
if [ $YEARS -ge 2 ]; then
    echo "⚠️  WARNING: This is a large dataset that will take significant time and bandwidth."
    read -p "Continue? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "❌ Operation cancelled."
        exit 0
    fi
fi

echo "🔍 Checking if your application is running..."

# Check if app is running
if curl -s --head --request GET "$APP_URL" | grep "200 OK" > /dev/null; then
    echo "✅ Application is running - using web interface method"
    
    # Use the web interface (fastest method)
    echo "🌐 Triggering historical data load via web interface..."
    
    # Determine which endpoint to call based on years
    if [ $YEARS -le 1 ]; then
        ENDPOINT="$APP_URL/MarketData/LoadCompleteHistory"
        DAYS=$((YEARS * 365))
        CURL_DATA="daysBack=$DAYS"
    else
        ENDPOINT="$APP_URL/MarketData/LoadMassiveHistory"
        CURL_DATA="yearsBack=$YEARS"
    fi
    
    echo "📞 Calling: $ENDPOINT"
    echo "📦 Parameters: $CURL_DATA"
    
    # Make the request
    RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$ENDPOINT" \
        -H "Content-Type: application/x-www-form-urlencoded" \
        -d "$CURL_DATA")
    
    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
    
    if [ "$HTTP_CODE" -eq 200 ] || [ "$HTTP_CODE" -eq 302 ]; then
        echo "✅ Historical data load started successfully!"
        echo "🔍 Monitor progress at: $APP_URL/MarketData"
        echo ""
        echo "📊 The load is now running in the background."
        echo "   You can close this script and check progress in your browser."
        echo "   Or watch your application logs for detailed progress."
    else
        echo "❌ Failed to start historical data load (HTTP $HTTP_CODE)"
        exit 1
    fi
    
else
    echo "⚠️  Application is not running. Starting direct database method..."
    
    # Check if we have the C# script
    if [ -f "Scripts/load-bitcoin-history.csx" ]; then
        echo "🔧 Using C# script method..."
        
        # Run the C# script
        dotnet script Scripts/load-bitcoin-history.csx $YEARS
        
    else
        echo "❌ Could not find loading script and application is not running."
        echo ""
        echo "💡 To load historical data, you have these options:"
        echo ""
        echo "   Option 1 (Recommended): Start your application and try again"
        echo "   $ dotnet run &"
        echo "   $ $0 $YEARS"
        echo ""
        echo "   Option 2: Use the web interface manually"
        echo "   $ dotnet run"
        echo "   Then visit: http://localhost:5275/MarketData"
        echo ""
        exit 1
    fi
fi

echo ""
echo "🏁 Script completed at: $(date)"
echo ""
echo "💡 Next steps:"
echo "   1. Check your data at: $APP_URL/MarketData"
echo "   2. Start building trading strategies with your historical data!"
echo "   3. The background service will keep your data updated daily."