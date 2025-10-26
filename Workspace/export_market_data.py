#!/usr/bin/env python3
"""
Export MarketData table to CSV file for backup and fast re-import
"""

import pymssql
import csv
import os
from datetime import datetime

class MarketDataExporter:
    def __init__(self):
        self.server = "localhost"
        self.port = 1433
        self.database = "MyFirstDatabase"
        self.username = "sa"
        self.password = "MyPassword123#"
        
    def connect(self):
        """Connect to SQL Server database"""
        try:
            conn = pymssql.connect(
                server=f"{self.server}:{self.port}",
                user=self.username,
                password=self.password,
                database=self.database
            )
            print(f"✅ Connected to database: {self.database}")
            return conn
        except Exception as e:
            print(f"❌ Database connection failed: {e}")
            return None

    def export_to_csv(self, output_file="market_data_backup.csv"):
        """Export all MarketData to CSV file"""
        conn = self.connect()
        if not conn:
            return False
            
        try:
            cursor = conn.cursor()
            
            # Get total count first
            cursor.execute("SELECT COUNT(*) FROM MarketData")
            total_rows = cursor.fetchone()[0]
            print(f"📊 Total rows to export: {total_rows:,}")
            
            # Export data with progress tracking (excluding Id as it's IDENTITY)
            cursor.execute("""
                SELECT Symbol, TimeFrame, OpenTime, OpenPrice, HighPrice, LowPrice, ClosePrice, Volume, CloseTime, CreatedAt
                FROM MarketData 
                ORDER BY OpenTime
            """)
            
            with open(output_file, 'w', newline='', encoding='utf-8') as csvfile:
                writer = csv.writer(csvfile)
                
                # Write header (all columns except Id which is IDENTITY)
                writer.writerow(['Symbol', 'TimeFrame', 'OpenTime', 'OpenPrice', 'HighPrice', 'LowPrice', 'ClosePrice', 'Volume', 'CloseTime', 'CreatedAt'])
                
                # Write data in batches for progress tracking
                batch_size = 10000
                rows_written = 0
                
                while True:
                    rows = cursor.fetchmany(batch_size)
                    if not rows:
                        break
                        
                    for row in rows:
                        # Convert datetime fields to ISO format strings
                        open_time_str = row[2].strftime("%Y-%m-%d %H:%M:%S") if row[2] else ""
                        close_time_str = row[8].strftime("%Y-%m-%d %H:%M:%S") if row[8] else ""
                        created_at_str = row[9].strftime("%Y-%m-%d %H:%M:%S") if row[9] else ""
                        
                        writer.writerow([
                            row[0],  # Symbol
                            row[1],  # TimeFrame
                            open_time_str,  # OpenTime
                            float(row[3]) if row[3] else 0,  # OpenPrice
                            float(row[4]) if row[4] else 0,  # HighPrice
                            float(row[5]) if row[5] else 0,  # LowPrice
                            float(row[6]) if row[6] else 0,  # ClosePrice
                            float(row[7]) if row[7] else 0,  # Volume
                            close_time_str,  # CloseTime
                            created_at_str   # CreatedAt
                        ])
                    
                    rows_written += len(rows)
                    progress = (rows_written / total_rows) * 100
                    print(f"📝 Progress: {rows_written:,}/{total_rows:,} ({progress:.1f}%)")
            
            file_size = os.path.getsize(output_file) / (1024 * 1024)  # MB
            print(f"✅ Export completed!")
            print(f"📁 File: {output_file}")
            print(f"📏 Size: {file_size:.1f} MB")
            print(f"📊 Rows: {rows_written:,}")
            
            return True
            
        except Exception as e:
            print(f"❌ Export failed: {e}")
            return False
        finally:
            conn.close()

    def get_data_info(self):
        """Get information about the current data"""
        conn = self.connect()
        if not conn:
            return
            
        try:
            cursor = conn.cursor()
            
            # Basic stats
            cursor.execute("""
                SELECT 
                    COUNT(*) as total_rows,
                    MIN(OpenTime) as earliest_date,
                    MAX(OpenTime) as latest_date,
                    COUNT(DISTINCT Symbol) as unique_symbols
                FROM MarketData
            """)
            
            stats = cursor.fetchone()
            print(f"\n📊 Current MarketData Statistics:")
            print(f"   Total rows: {stats[0]:,}")
            print(f"   Date range: {stats[1]} to {stats[2]}")
            print(f"   Symbols: {stats[3]}")
            
            # Data per symbol
            cursor.execute("""
                SELECT Symbol, TimeFrame, COUNT(*) as row_count, MIN(OpenTime), MAX(OpenTime)
                FROM MarketData 
                GROUP BY Symbol, TimeFrame
                ORDER BY row_count DESC
            """)
            
            print(f"\n📈 Data by Symbol and TimeFrame:")
            for row in cursor.fetchall():
                print(f"   {row[0]} ({row[1]}): {row[2]:,} rows ({row[3]} to {row[4]})")
                
        except Exception as e:
            print(f"❌ Failed to get data info: {e}")
        finally:
            conn.close()

def main():
    exporter = MarketDataExporter()
    
    print("🚀 MarketData Export Tool")
    print("=" * 50)
    
    # Show current data info
    exporter.get_data_info()
    
    # Ask for confirmation
    print(f"\n❓ Export all data to CSV file? (y/n): ", end="")
    if input().lower() != 'y':
        print("❌ Export cancelled")
        return
    
    # Generate filename with timestamp
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    filename = f"market_data_backup_{timestamp}.csv"
    
    print(f"\n🔄 Starting export to: {filename}")
    start_time = datetime.now()
    
    if exporter.export_to_csv(filename):
        duration = datetime.now() - start_time
        print(f"⏱️  Export completed in: {duration}")
        print(f"\n💡 Next steps:")
        print(f"   1. Keep this file safe as your data backup")
        print(f"   2. Use the import script to restore data quickly")
        print(f"   3. Consider compressing the file (.zip/.gz) to save space")
    else:
        print("❌ Export failed")

if __name__ == "__main__":
    main()