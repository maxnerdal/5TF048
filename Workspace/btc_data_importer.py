#!/usr/bin/env python3
"""
BTC/USDT Historical Data Importer from Binance API
Imports 1-minute candlestick data starting from Binance launch (August 2017)
Only imports new data that doesn't exist in the database.

Features:
- 📊 Progress tracking with percentage completion  
- 🔄 Batch processing for efficiency
- 🛡️ Error recovery - can restart where it left off
- ⏰ Rate limiting to respect Binance API limits
- 🚫 Duplicate prevention
- 📈 Real-time statistics and ETA
"""

import pymssql
import requests
import time
from datetime import datetime, timezone, timedelta
from typing import List, Tuple, Optional
import sys

class BTCDataImporter:
    def __init__(self):
        self.binance_api_url = "https://api.binance.com/api/v3/klines"
        self.symbol = "BTCUSDT"
        self.interval = "1m"
        self.limit = 1000  # Max records per API call
        
        # Binance BTC/USDT trading started August 17, 2017
        self.start_timestamp = int(datetime(2017, 8, 17, tzinfo=timezone.utc).timestamp() * 1000)
        
    def get_database_connection(self):
        """Get database connection"""
        return pymssql.connect(
            server='localhost',
            port=1433,
            user='sa',
            password='MyPassword123#',
            database='MyFirstDatabase'
        )
    
    def get_latest_timestamp(self) -> Optional[int]:
        """Get the latest OpenTime timestamp from database for BTC 1m data"""
        conn = self.get_database_connection()
        cursor = conn.cursor()
        
        try:
            cursor.execute("""
                SELECT MAX(OpenTime) 
                FROM MarketData 
                WHERE Symbol = %s AND TimeFrame = %s
            """, ('BTC', '1m'))
            
            result = cursor.fetchone()
            latest_time = result[0] if result[0] else None
            
            if latest_time:
                # Convert to timestamp (milliseconds) and add 1 minute to avoid duplicates
                timestamp = int(latest_time.timestamp() * 1000) + 60000
                print(f"📅 Latest data in DB: {latest_time}")
                return timestamp
            else:
                print("📅 No existing data found, starting from Binance launch (Aug 17, 2017)")
                return self.start_timestamp
                
        except Exception as e:
            print(f"❌ Error checking latest timestamp: {e}")
            return self.start_timestamp
        finally:
            cursor.close()
            conn.close()
    
    def fetch_klines(self, start_time: int, end_time: Optional[int] = None) -> List:
        """Fetch kline data from Binance API"""
        params = {
            'symbol': self.symbol,
            'interval': self.interval,
            'startTime': start_time,
            'limit': self.limit
        }
        
        if end_time:
            params['endTime'] = end_time
        
        try:
            response = requests.get(self.binance_api_url, params=params)
            response.raise_for_status()
            return response.json()
        except requests.RequestException as e:
            print(f"❌ API Error: {e}")
            return []
    
    def convert_kline_to_db_format(self, kline: List) -> Tuple:
        """Convert Binance kline format to database format"""
        # Binance kline format: [open_time, open, high, low, close, volume, close_time, ...]
        open_time = datetime.fromtimestamp(kline[0] / 1000, tz=timezone.utc)
        close_time = datetime.fromtimestamp(kline[6] / 1000, tz=timezone.utc)
        
        return (
            'BTC',                    # Symbol
            '1m',                     # TimeFrame
            open_time,                # OpenTime
            float(kline[1]),          # OpenPrice
            float(kline[2]),          # HighPrice
            float(kline[3]),          # LowPrice
            float(kline[4]),          # ClosePrice
            float(kline[5]),          # Volume
            close_time                # CloseTime
        )
    
    def bulk_insert_data(self, data_batch: List[Tuple]) -> int:
        """Insert batch of data into database"""
        if not data_batch:
            return 0
        
        conn = self.get_database_connection()
        cursor = conn.cursor()
        
        try:
            # Use executemany for efficient bulk insert
            cursor.executemany("""
                INSERT INTO MarketData 
                (Symbol, TimeFrame, OpenTime, OpenPrice, HighPrice, LowPrice, ClosePrice, Volume, CloseTime)
                VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s)
            """, data_batch)
            
            conn.commit()
            return len(data_batch)
            
        except Exception as e:
            print(f"❌ Database insert error: {e}")
            conn.rollback()
            return 0
        finally:
            cursor.close()
            conn.close()
    
    def calculate_progress_stats(self, current_timestamp: int, start_timestamp: int, end_timestamp: int, 
                               batch_count: int, total_records: int, start_import_time: datetime):
        """Calculate and display detailed progress statistics with examples"""
        
        # PROGRESS BERÄKNING FÖRKLARING:
        # ==============================
        # Progress baseras på tidsintervall, inte antal batches, för mer exakt uppskattning
        
        # 1. Beräkna totalt tidsintervall som ska täckas
        total_time_span = end_timestamp - start_timestamp  # i millisekunder
        
        # 2. Beräkna hur mycket som redan är klart  
        completed_time_span = current_timestamp - start_timestamp  # i millisekunder
        
        # 3. Beräkna progress i procent
        # Exempel: Om vi ska hämta 2017-08-17 till 2025-10-15
        # - total_time_span = hela perioden (8 år i ms)
        # - completed_time_span = från start till nuvarande batch (t.ex. 2019-03-01)
        # - progress = (2 år / 8 år) × 100 = 25%
        progress_percent = (completed_time_span / total_time_span * 100) if total_time_span > 0 else 100
        
        # ETA BERÄKNING FÖRKLARING:
        # =========================
        # Använder faktisk tid vs progress för att uppskatta slutfört tid
        
        elapsed_time = datetime.now() - start_import_time
        elapsed_seconds = elapsed_time.total_seconds()
        
        if progress_percent > 0:
            # Exempel på ETA-beräkning:
            # - Vi har jobbat i 120 sekunder och kommit 25% av vägen
            # - Uppskattad total tid: 120 ÷ 0.25 = 480 sekunder
            # - Kvarvarande tid: 480 - 120 = 360 sekunder (6 minuter)
            estimated_total_seconds = elapsed_seconds / (progress_percent / 100)
            remaining_seconds = estimated_total_seconds - elapsed_seconds
            eta = datetime.now() + timedelta(seconds=remaining_seconds)
        else:
            eta = None
        
        # Current date being processed
        current_date = datetime.fromtimestamp(current_timestamp/1000, tz=timezone.utc)
        
        # Display progress bar
        bar_length = 30
        filled_length = int(bar_length * progress_percent / 100)
        bar = '█' * filled_length + '░' * (bar_length - filled_length)
        
        print(f"\n📊 PROGRESS RAPPORT")
        print(f"├─ Progress: [{bar}] {progress_percent:.1f}%")
        print(f"├─ Current Date: {current_date.strftime('%Y-%m-%d %H:%M:%S')}")
        print(f"├─ Batches: {batch_count}")
        print(f"├─ Records: {total_records:,}")
        print(f"├─ Elapsed: {str(elapsed_time).split('.')[0]}")
        if eta:
            print(f"└─ ETA: {eta.strftime('%H:%M:%S')} ({remaining_seconds/60:.0f} min kvar)")
        else:
            print(f"└─ ETA: Beräknar...")

    def import_historical_data(self):
        """Main function to import all historical data with enhanced progress tracking"""
        print("🚀 BTC/USDT HISTORICAL DATA IMPORTER")
        print("=" * 60)
        
        # Get starting point
        start_time = self.get_latest_timestamp()
        current_time = int(datetime.now(timezone.utc).timestamp() * 1000)
        
        if start_time >= current_time:
            print("✅ Databasen är redan uppdaterad!")
            return
        
        # Calculate scope and batch requirements
        start_date = datetime.fromtimestamp(start_time/1000, tz=timezone.utc)
        end_date = datetime.fromtimestamp(current_time/1000, tz=timezone.utc)
        
        # BATCH-BERÄKNING FÖRKLARING:
        # ============================
        # 1. Beräkna totalt antal minuter mellan start och slut
        total_minutes = int((end_date - start_date).total_seconds() / 60)
        
        # 2. Beräkna antal API-calls som behövs
        # Binance API: Max 1000 records per call (self.limit = 1000)
        # 
        # Exempel: Om vi behöver 2500 minuters data:
        # - 2500 datapunkter (1 per minut)
        # - 2500 ÷ 1000 = 2.5 → avrunda uppåt = 3 batches
        # - Batch 1: 1000 records (minut 0-999)
        # - Batch 2: 1000 records (minut 1000-1999) 
        # - Batch 3: 500 records (minut 2000-2499)
        #
        # Formeln: (total_records + batch_size - 1) // batch_size
        # Detta är "ceiling division" - avrundar alltid uppåt
        estimated_batches = (total_minutes + self.limit - 1) // self.limit
        
        # EXEMPEL PÅ BERÄKNING:
        # =====================
        # Scenario 1: 1 år historisk data
        # - 1 år = 365 dagar × 24 timmar × 60 minuter = 525,600 minuter
        # - 525,600 ÷ 1000 = 525.6 → 526 batches
        # - Tid: 526 × 0.1 sekund = 52.6 sekunder (med rate limiting)
        #
        # Scenario 2: Från Binance start (Aug 2017) till idag (~8 år)
        # - 8 år ≈ 4,200,000 minuter
        # - 4,200,000 ÷ 1000 = 4,200 batches
        # - Tid: 4,200 × 0.1 sekund = 420 sekunder = 7 minuter
        
        print(f"📅 Period: {start_date.strftime('%Y-%m-%d %H:%M')} → {end_date.strftime('%Y-%m-%d %H:%M')}")
        print(f"📊 Uppskattade datapunkter: {total_minutes:,}")
        print(f"📊 Uppskattade batches: {estimated_batches:,}")
        print(f"⏰ Uppskattad tid: {estimated_batches * 0.1 / 60:.0f}-{estimated_batches * 0.5 / 60:.0f} minuter")
        print(f"� Lagrar i: MarketData (Symbol='BTC', TimeFrame='1m')")
        print("=" * 60)
        
        # Import tracking
        total_records = 0
        batch_count = 0
        start_import_time = datetime.now()
        original_start_time = start_time
        
        try:
            while start_time < current_time:
                batch_count += 1
                
                # Fetch data from Binance
                klines = self.fetch_klines(start_time)
                
                if not klines:
                    print("⚠️ Ingen mer data tillgänglig från Binance API")
                    break
                
                # Convert to database format
                db_data = []
                for kline in klines:
                    db_record = self.convert_kline_to_db_format(kline)
                    db_data.append(db_record)
                
                # Insert into database
                inserted = self.bulk_insert_data(db_data)
                total_records += inserted
                
                # Update start_time for next batch
                if klines:
                    start_time = klines[-1][0] + 1
                
                # Show progress every 20 batches or if it's the first few
                if batch_count <= 5 or batch_count % 20 == 0:
                    self.calculate_progress_stats(
                        start_time, original_start_time, current_time,
                        batch_count, total_records, start_import_time
                    )
                elif batch_count % 5 == 0:
                    # Quick update every 5 batches
                    progress = ((start_time - original_start_time) / (current_time - original_start_time) * 100)
                    current_date = datetime.fromtimestamp(start_time/1000, tz=timezone.utc)
                    print(f"📈 {progress:.1f}% - Batch {batch_count} - {current_date.strftime('%Y-%m-%d %H:%M')} - {total_records:,} records")
                
                # Rate limiting - respekterar Binance limits
                time.sleep(0.1)
                
                # Error recovery checkpoint every 100 batches
                if batch_count % 100 == 0:
                    checkpoint_date = datetime.fromtimestamp(start_time/1000, tz=timezone.utc)
                    print(f"� Checkpoint: {checkpoint_date.strftime('%Y-%m-%d %H:%M')} - {total_records:,} records saved")
        
        except KeyboardInterrupt:
            print(f"\n⚠️ Import avbruten av användare vid batch {batch_count}")
            print(f"💾 {total_records:,} records har sparats i databasen")
            checkpoint_date = datetime.fromtimestamp(start_time/1000, tz=timezone.utc)
            print(f"🔄 Nästa start: {checkpoint_date.strftime('%Y-%m-%d %H:%M')}")
            return
        
        except Exception as e:
            print(f"\n❌ Fel under import: {e}")
            print(f"� {total_records:,} records har sparats innan felet")
            return
        
        # Final statistics
        elapsed_time = datetime.now() - start_import_time
        print("\n" + "=" * 60)
        print("🎉 IMPORT SLUTFÖRD!")
        print(f"📊 Totalt antal records: {total_records:,}")
        print(f"📊 Antal batches: {batch_count}")
        print(f"⏰ Total tid: {str(elapsed_time).split('.')[0]}")
        print(f"⚡ Hastighet: {total_records/elapsed_time.total_seconds():.0f} records/sekund")
        print(f"📅 Data täcker nu: {datetime.fromtimestamp(original_start_time/1000, tz=timezone.utc).strftime('%Y-%m-%d')} → {datetime.now().strftime('%Y-%m-%d')}")
        print("=" * 60)

def main():
    """Main execution function"""
    importer = BTCDataImporter()
    
    try:
        importer.import_historical_data()
    except KeyboardInterrupt:
        print("\n⚠️ Import interrupted by user")
    except Exception as e:
        print(f"❌ Unexpected error: {e}")

if __name__ == "__main__":
    main()