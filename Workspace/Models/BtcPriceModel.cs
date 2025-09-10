namespace WebApp.Models
{
    public class BtcPriceModel
    {
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";
        public DateTime LastUpdated { get; set; }
        public decimal Change24h { get; set; }
        public decimal ChangePercent24h { get; set; }

        public BtcPriceModel()
        {
            Price = 0;
            Currency = "USD";
            LastUpdated = DateTime.UtcNow;
            Change24h = 0;
            ChangePercent24h = 0;
        }
    }

    // For CoinGecko API response
    public class CoinGeckoResponse
    {
        public CoinGeckoBitcoin Bitcoin { get; set; }
    }

    public class CoinGeckoBitcoin
    {
        public decimal Usd { get; set; }
        public decimal Usd_24h_change { get; set; }
    }
}
