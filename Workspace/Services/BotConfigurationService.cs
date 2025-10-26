using System.Text.Json;
using System.Text.Json.Serialization;
using WebApp.Models;

namespace WebApp.Services
{
    /// <summary>
    /// Service for handling bot configuration serialization and management
    /// </summary>
    public interface IBotConfigurationService
    {
        string SerializeConfiguration<T>(T configuration) where T : BaseBotConfiguration;
        T? DeserializeConfiguration<T>(string json) where T : BaseBotConfiguration;
        BaseBotConfiguration? DeserializeConfiguration(string json, string botType);
        bool ValidateConfiguration<T>(T configuration, out List<string> errors) where T : BaseBotConfiguration;
        Task<DcaBotConfiguration> CreateDefaultDcaConfigurationAsync(string targetAsset = "BTC");
    }

    public class BotConfigurationService : IBotConfigurationService
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public BotConfigurationService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() },
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// Serializes a bot configuration to JSON string
        /// </summary>
        public string SerializeConfiguration<T>(T configuration) where T : BaseBotConfiguration
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            configuration.UpdatedAt = DateTime.UtcNow;
            return JsonSerializer.Serialize(configuration, _jsonOptions);
        }

        /// <summary>
        /// Deserializes a JSON string to a specific bot configuration type
        /// </summary>
        public T? DeserializeConfiguration<T>(string json) where T : BaseBotConfiguration
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Deserializes a JSON string to the appropriate bot configuration type based on botType
        /// </summary>
        public BaseBotConfiguration? DeserializeConfiguration(string json, string botType)
        {
            if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(botType))
                return null;

            try
            {
                return botType.ToUpper() switch
                {
                    "DCA" => JsonSerializer.Deserialize<DcaBotConfiguration>(json, _jsonOptions),
                    // Add other bot types here as they are implemented
                    // "GRID" => JsonSerializer.Deserialize<GridBotConfiguration>(json, _jsonOptions),
                    // "MOMENTUM" => JsonSerializer.Deserialize<MomentumBotConfiguration>(json, _jsonOptions),
                    _ => null
                };
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Validates a bot configuration and returns any validation errors
        /// </summary>
        public bool ValidateConfiguration<T>(T configuration, out List<string> errors) where T : BaseBotConfiguration
        {
            errors = new List<string>();

            if (configuration == null)
            {
                errors.Add("Configuration cannot be null");
                return false;
            }

            // Type-specific validation
            switch (configuration)
            {
                case DcaBotConfiguration dcaConfig:
                    return dcaConfig.IsValid(out errors);
                
                // Add validation for other bot types here
                default:
                    errors.Add($"Unknown bot configuration type: {configuration.GetType().Name}");
                    return false;
            }
        }

        /// <summary>
        /// Creates a default DCA configuration with sensible defaults
        /// </summary>
        public async Task<DcaBotConfiguration> CreateDefaultDcaConfigurationAsync(string targetAsset = "BTC")
        {
            return new DcaBotConfiguration
            {
                TargetAsset = targetAsset,
                InvestmentAmount = 100.00m, // $100 default
                Frequency = DcaFrequency.Weekly,
                DayOfWeek = DayOfWeek.Monday,
                ExecutionHour = 9, // 9 AM UTC
                ExecutionMinute = 0,
                Currency = "USD",
                AutoStart = false,
                StartDate = DateTime.UtcNow.Date,
                Notes = $"Weekly ${100:F2} investment in {targetAsset}"
            };
        }

        /// <summary>
        /// Helper method to get a user-friendly description of the DCA configuration
        /// </summary>
        public static string GetConfigurationDescription(DcaBotConfiguration config)
        {
            if (config == null) return "No configuration";

            var frequency = config.Frequency switch
            {
                DcaFrequency.Daily => "daily",
                DcaFrequency.Weekly => $"weekly on {config.DayOfWeek}",
                DcaFrequency.Monthly => $"monthly on day {config.DayOfMonth}",
                _ => "unknown frequency"
            };

            var time = $"{config.ExecutionHour:D2}:{config.ExecutionMinute:D2} UTC";
            
            return $"Invest {config.Currency} {config.InvestmentAmount:F2} in {config.TargetAsset} {frequency} at {time}";
        }

        /// <summary>
        /// Creates a sample JSON configuration for testing/documentation
        /// </summary>
        public static string CreateSampleDcaJson()
        {
            var sampleConfig = new DcaBotConfiguration
            {
                TargetAsset = "BTC",
                InvestmentAmount = 50.00m,
                Frequency = DcaFrequency.Weekly,
                DayOfWeek = DayOfWeek.Monday,
                ExecutionHour = 9,
                ExecutionMinute = 0,
                Currency = "USD",
                AutoStart = false,
                MaxTotalInvestment = 2600.00m, // 1 year of $50/week
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddYears(1),
                Notes = "Conservative weekly Bitcoin DCA strategy"
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };

            return JsonSerializer.Serialize(sampleConfig, options);
        }
    }
}