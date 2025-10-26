using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    /// <summary>
    /// Base class for all bot configuration types
    /// </summary>
    public abstract class BaseBotConfiguration
    {
        public string BotType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Configuration model for Dollar Cost Average (DCA) trading bot
    /// </summary>
    public class DcaBotConfiguration : BaseBotConfiguration
    {
        public DcaBotConfiguration()
        {
            BotType = "DCA";
        }

        [Required(ErrorMessage = "Target asset is required")]
        [StringLength(10, ErrorMessage = "Asset symbol must be 10 characters or less")]
        public string TargetAsset { get; set; } = "BTC";

        [Required(ErrorMessage = "Investment amount is required")]
        [Range(0.01, 100000, ErrorMessage = "Investment amount must be between $0.01 and $100,000")]
        public decimal InvestmentAmount { get; set; }

        [Required(ErrorMessage = "Purchase frequency is required")]
        public DcaFrequency Frequency { get; set; } = DcaFrequency.Weekly;

        [Range(1, 31, ErrorMessage = "Day of month must be between 1 and 31")]
        public int? DayOfMonth { get; set; }

        public DayOfWeek? DayOfWeek { get; set; }

        [Range(0, 23, ErrorMessage = "Hour must be between 0 and 23")]
        public int ExecutionHour { get; set; } = 9; // 9 AM UTC

        [Range(0, 59, ErrorMessage = "Minute must be between 0 and 59")]
        public int ExecutionMinute { get; set; } = 0;

        [StringLength(50)]
        public string? Currency { get; set; } = "USD";

        public bool AutoStart { get; set; } = false;

        [Range(0.01, 1000000, ErrorMessage = "Maximum total investment must be between $0.01 and $1,000,000")]
        public decimal? MaxTotalInvestment { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Validates that the configuration is complete and valid
        /// </summary>
        public bool IsValid(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(TargetAsset))
                errors.Add("Target asset is required");

            if (InvestmentAmount <= 0)
                errors.Add("Investment amount must be greater than 0");

            if (Frequency == DcaFrequency.Monthly && (DayOfMonth == null || DayOfMonth < 1 || DayOfMonth > 31))
                errors.Add("Day of month is required for monthly frequency");

            if (Frequency == DcaFrequency.Weekly && DayOfWeek == null)
                errors.Add("Day of week is required for weekly frequency");

            if (StartDate.HasValue && EndDate.HasValue && StartDate >= EndDate)
                errors.Add("End date must be after start date");

            if (MaxTotalInvestment.HasValue && MaxTotalInvestment <= 0)
                errors.Add("Maximum total investment must be greater than 0");

            return errors.Count == 0;
        }

        /// <summary>
        /// Gets the next execution time based on the frequency settings
        /// </summary>
        public DateTime GetNextExecutionTime()
        {
            var now = DateTime.UtcNow;
            var executionTime = new DateTime(now.Year, now.Month, now.Day, ExecutionHour, ExecutionMinute, 0);

            switch (Frequency)
            {
                case DcaFrequency.Daily:
                    if (executionTime <= now)
                        executionTime = executionTime.AddDays(1);
                    break;

                case DcaFrequency.Weekly:
                    if (DayOfWeek.HasValue)
                    {
                        var daysUntilTarget = ((int)DayOfWeek.Value - (int)now.DayOfWeek + 7) % 7;
                        if (daysUntilTarget == 0 && executionTime <= now)
                            daysUntilTarget = 7;
                        executionTime = executionTime.AddDays(daysUntilTarget);
                    }
                    break;

                case DcaFrequency.Monthly:
                    if (DayOfMonth.HasValue)
                    {
                        var targetDay = Math.Min(DayOfMonth.Value, DateTime.DaysInMonth(now.Year, now.Month));
                        executionTime = new DateTime(now.Year, now.Month, targetDay, ExecutionHour, ExecutionMinute, 0);
                        
                        if (executionTime <= now)
                        {
                            var nextMonth = now.AddMonths(1);
                            targetDay = Math.Min(DayOfMonth.Value, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
                            executionTime = new DateTime(nextMonth.Year, nextMonth.Month, targetDay, ExecutionHour, ExecutionMinute, 0);
                        }
                    }
                    break;
            }

            return executionTime;
        }
    }

    /// <summary>
    /// Frequency options for DCA purchases
    /// </summary>
    public enum DcaFrequency
    {
        Daily = 1,
        Weekly = 2,
        Monthly = 3
    }

    /// <summary>
    /// View model for creating/editing DCA bot configurations
    /// </summary>
    public class DcaBotConfigurationViewModel
    {
        public long? UserBotId { get; set; }
        public string BotName { get; set; } = string.Empty;
        public DcaBotConfiguration Configuration { get; set; } = new();
        public List<DigitalAsset> AvailableAssets { get; set; } = new();
    }

    /// <summary>
    /// Summary view model for displaying DCA bot status
    /// </summary>
    public class DcaBotSummaryViewModel
    {
        public long UserBotId { get; set; }
        public string BotName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DcaBotConfiguration Configuration { get; set; } = new();
        public DateTime? LastRun { get; set; }
        public DateTime? NextRun { get; set; }
        public decimal TotalInvested { get; set; }
        public int TotalPurchases { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal UnrealizedPnL { get; set; }
        public decimal ROIPercentage { get; set; }
    }
}