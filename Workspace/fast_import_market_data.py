#!/usr/bin/env python3
"""
Fast import MarketData from CSV backup file
Much faster than downloading from API
"""

import pymssql
import csv
import os
from datetime import datetime

class MarketDataImporter:
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

    def clear_existing_data(self, conn):
        """Clear existing MarketData (optional)"""
        try:
            cursor = conn.cursor()
            cursor.execute("SELECT COUNT(*) FROM MarketData")
            existing_count = cursor.fetchone()[0]
            
            if existing_count > 0:
                print(f"⚠️  Found {existing_count:,} existing rows")
                print("❓ Clear existing data first? (y/n): ", end="")
                if input().lower() == 'y':
                    print("🗑️  Clearing existing data...")
                    cursor.execute("DELETE FROM MarketData")
                    conn.commit()
                    print("✅ Existing data cleared")
                else:
                    print("ℹ️  Keeping existing data (may create duplicates)")
            
        except Exception as e:
            print(f"❌ Failed to check existing data: {e}")

    def import_from_csv(self, csv_file):
        """Import MarketData from CSV file with bulk insert"""
        if not os.path.exists(csv_file):
            print(f"❌ File not found: {csv_file}")
            return False
            
        conn = self.connect()
        if not conn:
            return False
            
        try:
            # Optional: Clear existing data
            self.clear_existing_data(conn)
            
            cursor = conn.cursor()
            
            # Count total rows in CSV
            with open(csv_file, 'r', encoding='utf-8') as f:
                total_rows = sum(1 for line in f) - 1  # Subtract header
            print(f"📊 Total rows to import: {total_rows:,}")
            
            # Import with bulk insert for speed
            batch_size = 1000  # Adjust based on memory/performance
            batch_data = []
            rows_imported = 0
            
            with open(csv_file, 'r', encoding='utf-8') as csvfile:
                reader = csv.DictReader(csvfile)
                
                for row in reader:
                    try:
                        # Convert string timestamps back to datetime
                        open_time = datetime.strptime(row['OpenTime'], "%Y-%m-%d %H:%M:%S")
                        close_time = datetime.strptime(row['CloseTime'], "%Y-%m-%d %H:%M:%S") if row['CloseTime'] else None
                        created_at = datetime.strptime(row['CreatedAt'], "%Y-%m-%d %H:%M:%S") if row['CreatedAt'] else None
                        
                        batch_data.append((
                            row['Symbol'],
                            row['TimeFrame'],
                            open_time,
                            float(row['OpenPrice']),
                            float(row['HighPrice']),
                            float(row['LowPrice']),
                            float(row['ClosePrice']),
                            float(row['Volume']),
                            close_time,
                            created_at
                        ))
                        
                        # Execute batch when full
                        if len(batch_data) >= batch_size:
                            self.execute_batch(cursor, batch_data)
                            conn.commit()
                            rows_imported += len(batch_data)
                            batch_data = []
                            
                            # Progress update
                            progress = (rows_imported / total_rows) * 100
                            print(f"📝 Progress: {rows_imported:,}/{total_rows:,} ({progress:.1f}%)")
                    
                    except ValueError as e:
                        print(f"⚠️  Skipping invalid row: {e}")
                        continue
                
                # Execute remaining batch
                if batch_data:
                    self.execute_batch(cursor, batch_data)
                    conn.commit()
                    rows_imported += len(batch_data)
            
            print(f"✅ Import completed!")
            print(f"📊 Rows imported: {rows_imported:,}")
            
            # Verify import
            cursor.execute("SELECT COUNT(*) FROM MarketData")
            final_count = cursor.fetchone()[0]
            print(f"📈 Total rows in database: {final_count:,}")
            
            return True
            
        except Exception as e:
            print(f"❌ Import failed: {e}")
            conn.rollback()
            return False
        finally:
            conn.close()

    def execute_batch(self, cursor, batch_data):
        """Execute batch insert for performance"""
        if not batch_data:
            return
            
        # Use executemany for better performance (excluding Id as it's IDENTITY)
        cursor.executemany("""
            INSERT INTO MarketData (Symbol, TimeFrame, OpenTime, OpenPrice, HighPrice, LowPrice, ClosePrice, Volume, CloseTime, CreatedAt)
            VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s)
        """, batch_data)

    def verify_data(self, csv_file):
        """Verify imported data matches CSV file"""
        conn = self.connect()
        if not conn:
            return
            
        try:
            cursor = conn.cursor()
            
            # Count rows in database
            cursor.execute("SELECT COUNT(*) FROM MarketData")
            db_count = cursor.fetchone()[0]
            
            # Count rows in CSV
            with open(csv_file, 'r') as f:
                csv_count = sum(1 for line in f) - 1  # Subtract header
            
            print(f"\n🔍 Data Verification:")
            print(f"   CSV file rows: {csv_count:,}")
            print(f"   Database rows: {db_count:,}")
            
            if db_count == csv_count:
                print("✅ Data verification successful!")
            else:
                print("⚠️  Row count mismatch - some data may be missing")
            
            # Sample data check
            cursor.execute("""
                SELECT Symbol, TimeFrame, MIN(OpenTime), MAX(OpenTime), COUNT(*)
                FROM MarketData 
                GROUP BY Symbol, TimeFrame
                ORDER BY COUNT(*) DESC
            """)
            
            print(f"\n📊 Imported Data Summary:")
            for row in cursor.fetchall():
                print(f"   {row[0]} ({row[1]}): {row[4]:,} rows ({row[2]} to {row[3]})")
                
        except Exception as e:
            print(f"❌ Verification failed: {e}")
        finally:
            conn.close()

def main():
    importer = MarketDataImporter()
    
    print("🚀 MarketData Fast Import Tool")
    print("=" * 50)
    
    # List available backup files
    backup_files = [f for f in os.listdir('.') if f.startswith('market_data_backup_') and f.endswith('.csv')]
    
    if not backup_files:
        print("❌ No backup files found")
        print("💡 Run export_market_data.py first to create a backup file")
        return
    
    print("📁 Available backup files:")
    for i, file in enumerate(backup_files, 1):
        size = os.path.getsize(file) / (1024 * 1024)  # MB
        print(f"   {i}. {file} ({size:.1f} MB)")
    
    # Get user choice
    try:
        choice = int(input(f"\n❓ Select file to import (1-{len(backup_files)}): "))
        if choice < 1 or choice > len(backup_files):
            print("❌ Invalid selection")
            return
        
        selected_file = backup_files[choice - 1]
    except ValueError:
        print("❌ Invalid input")
        return
    
    print(f"\n🔄 Starting import from: {selected_file}")
    start_time = datetime.now()
    
    if importer.import_from_csv(selected_file):
        duration = datetime.now() - start_time
        print(f"⏱️  Import completed in: {duration}")
        
        # Verify the import
        importer.verify_data(selected_file)
        
        print(f"\n🎉 Your database is ready!")
        print(f"💡 This was much faster than downloading from Binance API!")
    else:
        print("❌ Import failed")

if __name__ == "__main__":
    main()