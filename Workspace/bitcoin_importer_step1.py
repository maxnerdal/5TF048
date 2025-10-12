#!/usr/bin/env python3
"""
Bitcoin Market Data Importer - Step 1: Basic Database Connection
"""

import pyodbc
import sys
from datetime import datetime

def test_database_connection():
    """Test connection to SQL Server database with multiple driver attempts"""
    
    print("🔍 Testing database connection...")
    
    # First, let's see what ODBC drivers are available
    print("\n📋 Available ODBC drivers:")
    try:
        drivers = pyodbc.drivers()
        if drivers:
            for driver in drivers:
                print(f"   - {driver}")
        else:
            print("   - No ODBC drivers found")
    except Exception as e:
        print(f"   - Could not list drivers: {e}")
    
    # List of connection strings to try (in order of preference)
    # Using the same credentials as your ASP.NET Core app
    connection_configs = [
        {
            "name": "ODBC Driver 18 for SQL Server",
            "connection_string": (
                "DRIVER={ODBC Driver 18 for SQL Server};"
                "SERVER=localhost,1433;"
                "DATABASE=MyFirstDatabase;"
                "UID=sa;"
                "PWD=MyPassword123#;"
                "TrustServerCertificate=yes;"
            )
        },
        {
            "name": "ODBC Driver 17 for SQL Server",
            "connection_string": (
                "DRIVER={ODBC Driver 17 for SQL Server};"
                "SERVER=localhost,1433;"
                "DATABASE=MyFirstDatabase;"
                "UID=sa;"
                "PWD=MyPassword123#;"
                "TrustServerCertificate=yes;"
            )
        },
        {
            "name": "SQL Server",
            "connection_string": (
                "DRIVER={SQL Server};"
                "SERVER=localhost,1433;"
                "DATABASE=MyFirstDatabase;"
                "UID=sa;"
                "PWD=MyPassword123#;"
            )
        },
        {
            "name": "FreeTDS",
            "connection_string": (
                "DRIVER={FreeTDS};"
                "SERVERNAME=localhost;"
                "DATABASE=MyFirstDatabase;"
                "UID=sa;"
                "PWD=MyPassword123#;"
                "TDS_Version=8.0;"
            )
        },
        {
            "name": "FreeTDS (Alternative)",
            "connection_string": (
                "DRIVER={FreeTDS};"
                "SERVER=localhost,1433;"
                "DATABASE=MyFirstDatabase;"
                "UID=sa;"
                "PWD=MyPassword123#;"
            )
        },
        {
            "name": "FreeTDS (DSN Style)",
            "connection_string": (
                "DSN=localhost;"
                "UID=sa;"
                "PWD=MyPassword123#;"
                "DATABASE=MyFirstDatabase;"
            )
        }
    ]
    
    print("\n🔄 Attempting connections...")
    
    for config in connection_configs:
        try:
            print(f"\n🔧 Trying {config['name']} driver...")
            connection = pyodbc.connect(config['connection_string'])
            print("✅ Database connection successful!")
            
            # Test a simple query
            cursor = connection.cursor()
            cursor.execute("SELECT GETDATE()")
            current_time = cursor.fetchone()[0]
            print(f"📅 Database time: {current_time}")
            
            # Check if MarketData table exists
            cursor.execute("""
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_NAME = 'MarketData'
            """)
            table_exists = cursor.fetchone()[0] > 0
            
            if table_exists:
                print("✅ MarketData table found")
                
                # Check existing records
                cursor.execute("SELECT COUNT(*) FROM MarketData")
                record_count = cursor.fetchone()[0]
                print(f"📊 Existing records: {record_count}")
                
                # Show table structure
                cursor.execute("""
                    SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'MarketData'
                    ORDER BY ORDINAL_POSITION
                """)
                columns = cursor.fetchall()
                print("📊 MarketData table structure:")
                for column in columns:
                    nullable = "NULL" if column[2] == "YES" else "NOT NULL"
                    print(f"   - {column[0]} ({column[1]}) {nullable}")
            else:
                print("⚠️  MarketData table not found")
            
            connection.close()
            print(f"🎉 Successfully connected using {config['name']} driver!")
            return True
            
        except Exception as e:
            print(f"❌ {config['name']} failed: {e}")
            continue
    
    print("\n💔 All connection attempts failed!")
    print("\n💡 To fix this issue:")
    print("   1. Install Microsoft ODBC Driver for SQL Server:")
    print("      sudo xcode-select --install")
    print("      brew install msodbcsql18 mssql-tools18")
    print("   2. Or install FreeTDS:")
    print("      brew install freetds")
    print("   3. Make sure SQL Server is running on localhost")
    print("   4. Verify MyFirstDatabase exists")
    print("   5. Check authentication settings")
    
    return False

if __name__ == "__main__":
    print("🚀 Bitcoin Data Importer - Step 1: Database Test")
    print("=" * 50)
    
    success = test_database_connection()
    
    if success:
        print("\n🎉 Ready for next step!")
    else:
        print("\n❌ Please fix database connection first")
        sys.exit(1)