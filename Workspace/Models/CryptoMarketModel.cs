using System.Text.Json.Serialization;

namespace WebApp.Models
{
    /// <summary>
    /// Model for displaying top 100 cryptocurrency market data
    /// </summary>
public class CryptoMarketModel
{
    public string Id { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Image { get; set; }
    public decimal CurrentPrice { get; set; }
    public long MarketCap { get; set; }
    public int MarketCapRank { get; set; }
    public decimal PriceChangePercentage24h { get; set; }
    public long TotalVolume { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<string> Categories { get; set; } = new List<string>();        // Additional properties for display
        public string FormattedMarketCap => FormatLargeNumber(MarketCap);
        public string FormattedVolume => FormatLargeNumber(TotalVolume);
        public string ChangeColorClass => PriceChangePercentage24h >= 0 ? "text-success" : "text-danger";
        public string ChangeIcon => PriceChangePercentage24h >= 0 ? "↗" : "↘";

        private static string FormatLargeNumber(long number)
        {
            if (number >= 1_000_000_000_000)
                return $"${(number / 1_000_000_000_000.0):F2}T";
            if (number >= 1_000_000_000)
                return $"${(number / 1_000_000_000.0):F2}B";
            if (number >= 1_000_000)
                return $"${(number / 1_000_000.0):F2}M";
            if (number >= 1_000)
                return $"${(number / 1_000.0):F2}K";
            return $"${number:F2}";
        }
    }

    /// <summary>
    /// CoinGecko API response model for markets endpoint
    /// </summary>
    public class CoinGeckoMarketResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("image")]
        public string Image { get; set; } = string.Empty;

        [JsonPropertyName("current_price")]
        public decimal CurrentPrice { get; set; }

        [JsonPropertyName("market_cap")]
        public long MarketCap { get; set; }

        [JsonPropertyName("market_cap_rank")]
        public int MarketCapRank { get; set; }

        [JsonPropertyName("price_change_percentage_24h")]
        public decimal PriceChangePercentage24h { get; set; }

        [JsonPropertyName("total_volume")]
        public long TotalVolume { get; set; }

        [JsonPropertyName("last_updated")]
        public DateTime LastUpdated { get; set; }
    }
}