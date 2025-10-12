using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApp.Models
{
    /// <summary>
    /// Represents historical market data (OHLCV candlestick) for cryptocurrency trading
    /// Maps directly to the MarketData table in the database
    /// </summary>
    [Table("MarketData")]
    public class MarketData
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Symbol { get; set; } = "";

        [Required]
        [MaxLength(10)]
        public string TimeFrame { get; set; } = "";

        [Required]
        public DateTime OpenTime { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,8)")]
        public decimal OpenPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,8)")]
        public decimal HighPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,8)")]
        public decimal LowPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,8)")]
        public decimal ClosePrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,8)")]
        public decimal Volume { get; set; }

        [Required]
        public DateTime CloseTime { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Helper method to create MarketData from Binance API response
        /// </summary>
        public static MarketData FromBinanceKline(object[] klineData, string symbol, string timeFrame)
        {
            // Binance kline format: [OpenTime, Open, High, Low, Close, Volume, CloseTime, ...]
            return new MarketData
            {
                Symbol = symbol,
                TimeFrame = timeFrame,
                OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(klineData[0])).DateTime,
                OpenPrice = Convert.ToDecimal(klineData[1]),
                HighPrice = Convert.ToDecimal(klineData[2]),
                LowPrice = Convert.ToDecimal(klineData[3]),
                ClosePrice = Convert.ToDecimal(klineData[4]),
                Volume = Convert.ToDecimal(klineData[5]),
                CloseTime = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(klineData[6])).DateTime,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Check if this candle represents a bullish (green) or bearish (red) movement
        /// </summary>
        public bool IsBullish => ClosePrice > OpenPrice;

        /// <summary>
        /// Calculate the price change for this candle
        /// </summary>
        public decimal PriceChange => ClosePrice - OpenPrice;

        /// <summary>
        /// Calculate the percentage change for this candle
        /// </summary>
        public decimal PriceChangePercent => OpenPrice != 0 ? (PriceChange / OpenPrice) * 100 : 0;

        /// <summary>
        /// Calculate the body size (difference between open and close)
        /// </summary>
        public decimal BodySize => Math.Abs(ClosePrice - OpenPrice);

        /// <summary>
        /// Calculate the upper shadow (wick) size
        /// </summary>
        public decimal UpperShadow => HighPrice - Math.Max(OpenPrice, ClosePrice);

        /// <summary>
        /// Calculate the lower shadow (wick) size
        /// </summary>
        public decimal LowerShadow => Math.Min(OpenPrice, ClosePrice) - LowPrice;

        /// <summary>
        /// Calculate the total price range for this candle
        /// </summary>
        public decimal Range => HighPrice - LowPrice;

        /// <summary>
        /// Calculate the typical price (average of high, low, close)
        /// </summary>
        public decimal TypicalPrice => (HighPrice + LowPrice + ClosePrice) / 3;

        /// <summary>
        /// Override ToString for easy debugging
        /// </summary>
        public override string ToString()
        {
            return $"{Symbol} {TimeFrame} {OpenTime:yyyy-MM-dd HH:mm} O:{OpenPrice:F4} H:{HighPrice:F4} L:{LowPrice:F4} C:{ClosePrice:F4} V:{Volume:F2}";
        }
    }

    /// <summary>
    /// Helper class for Binance API responses
    /// </summary>
    public class BinanceKlineResponse
    {
        public List<object[]> Klines { get; set; } = new();
    }

    /// <summary>
    /// Available timeframe constants
    /// </summary>
    public static class TimeFrames
    {
        public const string OneMinute = "1m";
        public const string FiveMinutes = "5m";
        public const string FifteenMinutes = "15m";
        public const string ThirtyMinutes = "30m";
        public const string OneHour = "1h";
        public const string FourHours = "4h";
        public const string OneDay = "1d";
        public const string OneWeek = "1w";
        public const string OneMonth = "1M";

        public static readonly List<string> All = new()
        {
            OneMinute, FiveMinutes, FifteenMinutes, ThirtyMinutes,
            OneHour, FourHours, OneDay, OneWeek, OneMonth
        };
    }

    /// <summary>
    /// Common cryptocurrency symbols
    /// </summary>
    public static class Symbols
    {
        public const string BTCUSDT = "BTCUSDT";
        public const string ETHUSDT = "ETHUSDT";
        public const string ADAUSDT = "ADAUSDT";
        public const string SOLUSDT = "SOLUSDT";
        public const string BNBUSDT = "BNBUSDT";
        public const string XRPUSDT = "XRPUSDT";

        public static readonly List<string> Popular = new()
        {
            BTCUSDT, ETHUSDT, ADAUSDT, SOLUSDT, BNBUSDT, XRPUSDT
        };
    }
}