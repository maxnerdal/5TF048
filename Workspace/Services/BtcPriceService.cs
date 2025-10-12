using WebApp.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApp.Services
{
    public interface IBtcPriceService
    {
        Task<BtcPriceModel> GetBtcPriceAsync();
        Task<List<CryptoMarketModel>> GetTop100CryptosAsync();
    }

    public class BtcPriceService : IBtcPriceService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BtcPriceService> _logger;
        private readonly IConfiguration _configuration;

        public BtcPriceService(HttpClient httpClient, ILogger<BtcPriceService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
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

        public async Task<List<CryptoMarketModel>> GetTop100CryptosAsync()
        {
            try
            {
                _logger.LogInformation("Fetching top cryptocurrencies from CoinGecko API");
                
                // Try to fetch real data from CoinGecko free API first
                var realData = await FetchRealCryptoDataAsync();
                if (realData != null && realData.Any())
                {
                    _logger.LogInformation("Successfully fetched {Count} cryptocurrencies from CoinGecko API", realData.Count);
                    return realData;
                }
                
                _logger.LogWarning("Failed to fetch real data, falling back to demo data");
                return GetDemoCryptoData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching cryptocurrency data");
                return GetFallbackCryptoData();
            }
        }

        private async Task<List<CryptoMarketModel>?> FetchRealCryptoDataAsync()
        {
            try
            {
                var apiKey = _configuration["CoinGecko:ApiKey"];
                var baseUrl = _configuration["CoinGecko:BaseUrl"];
                var proBaseUrl = _configuration["CoinGecko:ProBaseUrl"];
                
                string url;
                HttpRequestMessage request;
                
                if (!string.IsNullOrEmpty(apiKey))
                {
                    // Demo keys start with "CG-", Pro keys are different format
                    bool isDemoKey = apiKey.StartsWith("CG-");
                    string apiBaseUrl = isDemoKey ? baseUrl : proBaseUrl;
                    string headerName = isDemoKey ? "x-cg-demo-api-key" : "x-cg-pro-api-key";
                    
                    url = $"{apiBaseUrl}/coins/markets?vs_currency=usd&order=market_cap_desc&per_page=100&page=1&sparkline=false&price_change_percentage=24h";
                    request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Add(headerName, apiKey);
                    
                    _logger.LogInformation("Using {ApiType} API with URL: {Url}", isDemoKey ? "Demo" : "Pro", apiBaseUrl);
                    
                    var response = await _httpClient.SendAsync(request);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("{ApiType} API failed with status {StatusCode}: {Content}", isDemoKey ? "Demo" : "Pro", response.StatusCode, responseContent);
                        throw new HttpRequestException($"API returned {response.StatusCode}");
                    }
                    
                    _logger.LogInformation("Successfully fetched cryptocurrency data from {ApiType} API", isDemoKey ? "Demo" : "Pro");
                    return await ParseCryptoDataAsync(responseContent);
                }
                else
                {
                    // Try public API without authentication (limited functionality)
                    url = $"{baseUrl}/coins/markets?vs_currency=usd&order=market_cap_desc&per_page=100&page=1&sparkline=false&price_change_percentage=24h";
                    var response = await _httpClient.GetStringAsync(url);
                    return await ParseCryptoDataAsync(response);
                }
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("429") || ex.Message.Contains("rate limit"))
            {
                _logger.LogWarning("API rate limit exceeded, falling back to demo data");
                return null;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("401") || ex.Message.Contains("403"))
            {
                _logger.LogWarning("API authentication required, falling back to demo data");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch real crypto data");
                return null;
            }
        }

        private async Task<List<CryptoMarketModel>?> ParseCryptoDataAsync(string jsonResponse)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Parse JSON response directly to our model
            var apiData = JsonSerializer.Deserialize<List<CoinGeckoApiResponse>>(jsonResponse, options);
            
            if (apiData == null) return null;

            var cryptoModels = new List<CryptoMarketModel>();
            
            foreach (var item in apiData)
            {
                cryptoModels.Add(new CryptoMarketModel
                {
                    Id = item.Id ?? "",
                    Name = item.Name ?? "",
                    Symbol = item.Symbol?.ToUpper() ?? "",
                    Image = item.Image ?? "",
                    CurrentPrice = item.CurrentPrice ?? 0,
                    MarketCap = item.MarketCap ?? 0,
                    MarketCapRank = item.MarketCapRank ?? 0,
                    PriceChangePercentage24h = item.PriceChangePercentage24h ?? 0,
                        TotalVolume = (long)(item.TotalVolume ?? 0),
                        LastUpdated = item.LastUpdated ?? DateTime.UtcNow,
                    Categories = DetermineCategories(item.Id ?? "", item.Symbol ?? "")
                });
            }

            return cryptoModels;
        }

        private List<CryptoMarketModel> GetDemoCryptoData()
        {
            var random = new Random();
            var baseTime = DateTime.UtcNow;
            
            var demoData = new List<(string id, string name, string symbol, decimal basePrice, long baseCap, List<string> categories)>
            {
                ("bitcoin", "Bitcoin", "BTC", 43250.67m, 845_000_000_000L, new List<string> { "Layer 1", "Store of Value" }),
                ("ethereum", "Ethereum", "ETH", 2678.34m, 321_000_000_000L, new List<string> { "Layer 1", "Smart Contracts" }),
                ("binancecoin", "BNB", "BNB", 312.45m, 48_000_000_000L, new List<string> { "Exchange", "Smart Contracts" }),
                ("cardano", "Cardano", "ADA", 0.456m, 16_000_000_000L, new List<string> { "Layer 1", "Smart Contracts" }),
                ("solana", "Solana", "SOL", 87.23m, 38_000_000_000L, new List<string> { "Layer 1", "Smart Contracts" }),
                ("xrp", "XRP", "XRP", 0.634m, 34_500_000_000L, new List<string> { "Layer 1", "Payments" }),
                ("polkadot", "Polkadot", "DOT", 7.89m, 9_500_000_000L, new List<string> { "Layer 0", "Interoperability" }),
                ("dogecoin", "Dogecoin", "DOGE", 0.0876m, 12_300_000_000L, new List<string> { "Memes", "Payments" }),
                ("avalanche-2", "Avalanche", "AVAX", 34.56m, 13_200_000_000L, new List<string> { "Layer 1", "Smart Contracts" }),
                ("shiba-inu", "Shiba Inu", "SHIB", 0.0000098m, 5_800_000_000L, new List<string> { "Memes", "DeFi" }),
                ("polygon", "Polygon", "MATIC", 0.789m, 7_300_000_000L, new List<string> { "Layer 2", "Scaling" }),
                ("chainlink", "Chainlink", "LINK", 14.67m, 8_200_000_000L, new List<string> { "Oracles", "Infrastructure" }),
                ("cosmos", "Cosmos", "ATOM", 9.87m, 3_850_000_000L, new List<string> { "Layer 0", "Interoperability" }),
                ("litecoin", "Litecoin", "LTC", 67.43m, 5_000_000_000L, new List<string> { "Layer 1", "Payments" }),
                ("near", "NEAR Protocol", "NEAR", 2.34m, 2_500_000_000L, new List<string> { "Layer 1", "Smart Contracts" }),
                ("uniswap", "Uniswap", "UNI", 6.78m, 5_100_000_000L, new List<string> { "DeFi", "DEX" }),
                ("algorand", "Algorand", "ALGO", 0.198m, 1_600_000_000L, new List<string> { "Layer 1", "Smart Contracts" }),
                ("vechain", "VeChain", "VET", 0.0234m, 1_700_000_000L, new List<string> { "Layer 1", "Supply Chain" }),
                ("stellar", "Stellar", "XLM", 0.123m, 3_100_000_000L, new List<string> { "Layer 1", "Payments" }),
                ("filecoin", "Filecoin", "FIL", 4.56m, 2_200_000_000L, new List<string> { "Storage", "Infrastructure" }),
                ("tron", "TRON", "TRX", 0.0987m, 8_900_000_000L, new List<string> { "Layer 1", "Smart Contracts" }),
                ("ethereum-classic", "Ethereum Classic", "ETC", 20.34m, 2_900_000_000L, new List<string> { "Layer 1", "Smart Contracts" }),
                ("hedera", "Hedera", "HBAR", 0.0654m, 2_200_000_000L, new List<string> { "Layer 1", "Enterprise" }),
                ("cronos", "Cronos", "CRO", 0.089m, 2_250_000_000L, new List<string> { "Exchange", "DeFi" }),
                ("monero", "Monero", "XMR", 145.67m, 2_650_000_000L, new List<string> { "Privacy", "Payments" }),
                ("pepe", "Pepe", "PEPE", 0.00000123m, 5_200_000_000L, new List<string> { "Memes" }),
                ("floki", "FLOKI", "FLOKI", 0.000145m, 1_400_000_000L, new List<string> { "Memes", "Gaming" }),
                ("mantra-dao", "MANTRA", "OM", 3.45m, 3_100_000_000L, new List<string> { "RWA", "DeFi" }),
                ("ondo-finance", "Ondo", "ONDO", 0.67m, 1_300_000_000L, new List<string> { "RWA", "DeFi" }),
                ("polyhedra-network", "Polyhedra", "ZKJ", 0.89m, 850_000_000L, new List<string> { "Layer 2", "ZK-Proofs" })
            };

            var result = new List<CryptoMarketModel>();
            
            for (int i = 0; i < demoData.Count; i++)
            {
                var (id, name, symbol, basePrice, baseCap, categories) = demoData[i];
                var priceChange = (decimal)((random.NextDouble() - 0.5) * 10); // -5% to +5%
                var currentPrice = basePrice + (basePrice * priceChange / 100);
                
                result.Add(new CryptoMarketModel
                {
                    Id = id,
                    Name = name,
                    Symbol = symbol,
                    Image = $"https://assets.coingecko.com/coins/images/{i + 1}/small/{id}.png",
                    CurrentPrice = Math.Round(currentPrice, symbol == "BTC" ? 2 : symbol == "ETH" ? 2 : 8),
                    MarketCap = baseCap + (long)(baseCap * (double)priceChange / 100),
                    MarketCapRank = i + 1,
                    PriceChangePercentage24h = Math.Round(priceChange, 2),
                    TotalVolume = (long)(baseCap * 0.15 * (0.8 + random.NextDouble() * 0.4)), // 12-20% of market cap
                    LastUpdated = baseTime.AddMinutes(-random.Next(0, 60)),
                    Categories = categories
                });
            }

            return result;
        }

        private List<CryptoMarketModel> GetFallbackCryptoData()
        {
            return new List<CryptoMarketModel>
            {
                new() { Id = "bitcoin", Name = "Bitcoin", Symbol = "BTC", CurrentPrice = 43250.67m, MarketCap = 845_000_000_000L, MarketCapRank = 1, PriceChangePercentage24h = 2.34m, TotalVolume = 18_500_000_000L, LastUpdated = DateTime.UtcNow, Categories = new List<string> { "Layer 1", "Store of Value" } },
                new() { Id = "ethereum", Name = "Ethereum", Symbol = "ETH", CurrentPrice = 2678.34m, MarketCap = 321_000_000_000L, MarketCapRank = 2, PriceChangePercentage24h = -1.23m, TotalVolume = 12_300_000_000L, LastUpdated = DateTime.UtcNow, Categories = new List<string> { "Layer 1", "Smart Contracts" } },
                new() { Id = "binancecoin", Name = "BNB", Symbol = "BNB", CurrentPrice = 312.45m, MarketCap = 48_000_000_000L, MarketCapRank = 3, PriceChangePercentage24h = 4.56m, TotalVolume = 1_200_000_000L, LastUpdated = DateTime.UtcNow, Categories = new List<string> { "Exchange", "Smart Contracts" } },
                new() { Id = "cardano", Name = "Cardano", Symbol = "ADA", CurrentPrice = 0.456m, MarketCap = 16_000_000_000L, MarketCapRank = 4, PriceChangePercentage24h = -2.67m, TotalVolume = 890_000_000L, LastUpdated = DateTime.UtcNow, Categories = new List<string> { "Layer 1", "Smart Contracts" } },
                new() { Id = "solana", Name = "Solana", Symbol = "SOL", CurrentPrice = 87.23m, MarketCap = 38_000_000_000L, MarketCapRank = 5, PriceChangePercentage24h = 7.89m, TotalVolume = 2_100_000_000L, LastUpdated = DateTime.UtcNow, Categories = new List<string> { "Layer 1", "Smart Contracts" } }
            };
        }

        private List<string> DetermineCategories(string coinId, string symbol)
        {
            // Simple category mapping based on well-known coins
            var categories = new List<string>();
            
            var categoryMap = new Dictionary<string, List<string>>
            {
                ["bitcoin"] = new() { "Layer 1", "Store of Value" },
                ["ethereum"] = new() { "Layer 1", "Smart Contracts" },
                ["binancecoin"] = new() { "Exchange", "Smart Contracts" },
                ["cardano"] = new() { "Layer 1", "Smart Contracts" },
                ["solana"] = new() { "Layer 1", "Smart Contracts" },
                ["xrp"] = new() { "Layer 1", "Payments" },
                ["polkadot"] = new() { "Layer 0", "Interoperability" },
                ["dogecoin"] = new() { "Memes", "Payments" },
                ["avalanche-2"] = new() { "Layer 1", "Smart Contracts" },
                ["shiba-inu"] = new() { "Memes", "DeFi" },
                ["polygon"] = new() { "Layer 2", "Scaling" },
                ["chainlink"] = new() { "Oracles", "Infrastructure" },
                ["cosmos"] = new() { "Layer 0", "Interoperability" },
                ["litecoin"] = new() { "Layer 1", "Payments" },
                ["uniswap"] = new() { "DeFi", "DEX" },
                ["algorand"] = new() { "Layer 1", "Smart Contracts" },
                ["vechain"] = new() { "Layer 1", "Supply Chain" },
                ["stellar"] = new() { "Layer 1", "Payments" },
                ["filecoin"] = new() { "Storage", "Infrastructure" },
                ["tron"] = new() { "Layer 1", "Smart Contracts" },
                ["ethereum-classic"] = new() { "Layer 1", "Smart Contracts" },
                ["hedera"] = new() { "Layer 1", "Enterprise" },
                ["monero"] = new() { "Privacy", "Payments" }
            };
            
            if (categoryMap.TryGetValue(coinId.ToLower(), out var knownCategories))
            {
                categories.AddRange(knownCategories);
            }
            else
            {
                // Default categories based on symbol patterns
                if (symbol.ToLower().Contains("eth") || symbol.ToLower().Contains("token"))
                    categories.Add("Tokens");
                else if (symbol.ToLower().Contains("defi") || symbol.ToLower().Contains("swap"))
                    categories.Add("DeFi");
                else
                    categories.Add("Layer 1");
            }
            
            return categories;
        }
    }

    // API Response model for CoinGecko
    public class CoinGeckoApiResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("symbol")]
        public string? Symbol { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("current_price")]
        public decimal? CurrentPrice { get; set; }

        [JsonPropertyName("market_cap")]
        public long? MarketCap { get; set; }

        [JsonPropertyName("market_cap_rank")]
        public int? MarketCapRank { get; set; }

        [JsonPropertyName("price_change_percentage_24h")]
        public decimal? PriceChangePercentage24h { get; set; }

        [JsonPropertyName("total_volume")]
        public decimal? TotalVolume { get; set; }

        [JsonPropertyName("last_updated")]
        public DateTime? LastUpdated { get; set; }
    }
}
