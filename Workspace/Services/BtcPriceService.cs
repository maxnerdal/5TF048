using WebApp.Models;
using System.Text.Json;

namespace WebApp.Services
{
    public interface IBtcPriceService
    {
        Task<BtcPriceModel> GetBtcPriceAsync();
    }

    public class BtcPriceService : IBtcPriceService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BtcPriceService> _logger;

        public BtcPriceService(HttpClient httpClient, ILogger<BtcPriceService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<BtcPriceModel> GetBtcPriceAsync()
        {
            try
            {
                // Using CoinGecko API (free, no API key required)
                var response = await _httpClient.GetStringAsync(
                    "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd&include_24hr_change=true");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Parse the JSON response
                var jsonDoc = JsonDocument.Parse(response);
                var bitcoinData = jsonDoc.RootElement.GetProperty("bitcoin");

                var btcPrice = new BtcPriceModel
                {
                    Price = bitcoinData.GetProperty("usd").GetDecimal(),
                    Currency = "USD",
                    LastUpdated = DateTime.UtcNow,
                    ChangePercent24h = bitcoinData.TryGetProperty("usd_24h_change", out var change) 
                        ? change.GetDecimal() : 0
                };

                return btcPrice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching BTC price");
                
                // Return a default/error model
                return new BtcPriceModel
                {
                    Price = 0,
                    Currency = "USD",
                    LastUpdated = DateTime.UtcNow,
                    ChangePercent24h = 0
                };
            }
        }
    }
}
