#!/usr/bin/env python3
import sqlite3
import os

# Check if we can find any database files
print("Looking for database files...")

# Check for SQLite database files in current directory
for file in os.listdir('.'):
    if file.endswith('.db') or file.endswith('.sqlite') or file.endswith('.sqlite3'):
        print(f"Found SQLite database: {file}")
        try:
            conn = sqlite3.connect(file)
            cursor = conn.cursor()
            
            # Check if MarketData table exists
            cursor.execute("SELECT name FROM sqlite_master WHERE type='table' AND name='MarketData'")
            if cursor.fetchone():
                print(f"MarketData table found in {file}")
                
                # Get table info
                cursor.execute("SELECT COUNT(*) FROM MarketData")
                count = cursor.fetchone()[0]
                print(f"Total records: {count}")
                
                if count > 0:
                    # Get date range
                    cursor.execute("SELECT MIN(OpenTime), MAX(OpenTime) FROM MarketData WHERE Symbol = 'BTC'")
                    result = cursor.fetchone()
                    print(f"BTC Date range: {result[0]} to {result[1]}")
                    
                    # Get sample records
                    cursor.execute("SELECT * FROM MarketData WHERE Symbol = 'BTC' ORDER BY OpenTime LIMIT 3")
                    records = cursor.fetchall()
                    print("Sample records:")
                    for record in records:
                        print(f"  {record}")
            
            conn.close()
        except Exception as e:
            print(f"Error reading {file}: {e}")

print("\nChecking SQL Server connection...")
try:
    import pyodbc
    
    # Try different connection strings
    connection_strings = [
        "DRIVER={ODBC Driver 17 for SQL Server};SERVER=localhost,1433;DATABASE=MyFirstDatabase;UID=sa;PWD=MyPassword123#;TrustServerCertificate=yes",
        "DRIVER={ODBC Driver 18 for SQL Server};SERVER=localhost,1433;DATABASE=MyFirstDatabase;UID=sa;PWD=MyPassword123#;TrustServerCertificate=yes",
        "DRIVER={SQL Server};SERVER=localhost,1433;DATABASE=MyFirstDatabase;UID=sa;PWD=MyPassword123#"
    ]
    
    for conn_str in connection_strings:
        try:
            print(f"Trying: {conn_str.split(';')[0]}...")
            conn = pyodbc.connect(conn_str)
            cursor = conn.cursor()
            
            print("✅ SQL Server connection successful!")
            
            # Check MarketData table
            cursor.execute("SELECT COUNT(*) FROM MarketData")
            total_count = cursor.fetchone()[0]
            print(f"Total MarketData records: {total_count}")
            
            # Check BTC specifically
            cursor.execute("SELECT COUNT(*) FROM MarketData WHERE Symbol = 'BTC'")
            btc_count = cursor.fetchone()[0]
            print(f"BTC records: {btc_count}")
            
            if btc_count > 0:
                # Get BTC date range
                cursor.execute("SELECT MIN(OpenTime), MAX(OpenTime) FROM MarketData WHERE Symbol = 'BTC'")
                result = cursor.fetchone()
                print(f"BTC Date range: {result[0]} to {result[1]}")
                
                # Check what symbols exist
                cursor.execute("SELECT DISTINCT Symbol FROM MarketData")
                symbols = [row[0] for row in cursor.fetchall()]
                print(f"Available symbols: {symbols}")
                
                # Sample recent data
                cursor.execute("SELECT TOP 3 Symbol, OpenTime, ClosePrice FROM MarketData WHERE Symbol = 'BTC' ORDER BY OpenTime DESC")
                recent_data = cursor.fetchall()
                print("Most recent BTC data:")
                for row in recent_data:
                    print(f"  {row[0]} - {row[1]} - ${row[2]:,.2f}")
            else:
                print("⚠️  No BTC data found!")
                
                # Check what data exists
                cursor.execute("SELECT DISTINCT Symbol, COUNT(*) FROM MarketData GROUP BY Symbol")
                data = cursor.fetchall()
                if data:
                    print("Available data by symbol:")
                    for symbol, count in data:
                        print(f"  {symbol}: {count} records")
                else:
                    print("❌ MarketData table is empty!")
            
            conn.close()
            break
            
        except Exception as e:
            print(f"❌ Failed: {e}")
            continue
    else:
        print("❌ Could not connect to SQL Server with any connection string")
        
except ImportError:
    print("❌ pyodbc not available")
except Exception as e:
    print(f"❌ SQL Server error: {e}")