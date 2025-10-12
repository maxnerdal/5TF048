#!/bin/bash

# BTC Market Data Update Script
# This script calls your web application's API endpoint to trigger data updates

echo "=== BTC Market Data Update Script ==="
echo "Started at: $(date)"

# Configuration
BASE_URL="http://localhost:5275"  # Adjust this to your application's URL
API_ENDPOINT="$BASE_URL/MarketData/UpdateBtcHistoricalData"

# Check if the application is running
echo "Checking if application is running..."
if curl -s --head --request GET "$BASE_URL" | grep "200 OK" > /dev/null; then
    echo "✅ Application is running"
else
    echo "❌ Application is not running. Please start it first with 'dotnet run'"
    echo "   Navigate to your project directory and run: dotnet run"
    exit 1
fi

# Trigger the update
echo "Triggering market data update..."
echo "Calling: $API_ENDPOINT"

# Use curl to make POST request to trigger update
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_ENDPOINT" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "")

# Parse response
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | head -n -1)

if [ "$HTTP_CODE" -eq 200 ] || [ "$HTTP_CODE" -eq 302 ]; then
    echo "✅ Update request sent successfully (HTTP $HTTP_CODE)"
    echo "Check the application logs or visit $BASE_URL/MarketData to see results"
else
    echo "❌ Update request failed (HTTP $HTTP_CODE)"
    echo "Response: $BODY"
    exit 1
fi

echo "Completed at: $(date)"
echo "=== Script finished ==="
echo ""
echo "💡 Tip: You can also visit $BASE_URL/MarketData in your browser"
echo "   to manually trigger updates and view data statistics."