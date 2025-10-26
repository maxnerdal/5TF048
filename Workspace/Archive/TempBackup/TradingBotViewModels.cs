using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    /// <summary>
    /// ViewModel for creating a new user bot instance from a trading bot template
    /// </summary>
    public class CreateUserBotViewModel
    {
        /// <summary>
        /// The trading bot template to base this instance on
        /// </summary>
        [Required]
        public long BotId { get; set; }

        /// <summary>
        /// User's custom name for this bot
        /// </summary>
        [Required]
        [StringLength(100, ErrorMessage = "Bot name must be between 1 and 100 characters")]
        [Display(Name = "Bot Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Weekly investment amount for DCA bots
        /// </summary>
        [Required]
        [Range(1.0, 10000.0, ErrorMessage = "Weekly amount must be between $1 and $10,000")]
        [Display(Name = "Weekly Investment Amount ($)")]
        public decimal WeeklyBuyAmount { get; set; }

        /// <summary>
        /// Total investment limit
        /// </summary>
        [Required]
        [Range(10.0, 1000000.0, ErrorMessage = "Total investment must be between $10 and $1,000,000")]
        [Display(Name = "Total Investment Limit ($)")]
        public decimal InvestmentAmount { get; set; }

        /// <summary>
        /// Day of the week to execute trades (0 = Monday)
        /// </summary>
        [Required]
        [Range(0, 6, ErrorMessage = "Start day must be between 0 (Monday) and 6 (Sunday)")]
        [Display(Name = "Start Day")]
        public int StartDay { get; set; } = 0;

        /// <summary>
        /// Risk tolerance level
        /// </summary>
        [Required]
        [Display(Name = "Risk Level")]
        public string RiskLevel { get; set; } = "Medium";

        /// <summary>
        /// DCA Frequency: Daily, Weekly, or Monthly
        /// </summary>
        [Required]
        [Display(Name = "DCA Frequency")]
        public string DCAFrequency { get; set; } = "Weekly";

        /// <summary>
        /// Additional JSON parameters (optional)
        /// </summary>
        [Display(Name = "Additional Parameters (JSON)")]
        public string? Parameters { get; set; }
    }

    /// <summary>
    /// ViewModel for displaying trading bot information on the main page
    /// </summary>
    public class TradingBotViewModel
    {
        public long BotId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Strategy { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime Created { get; set; }
    }

    /// <summary>
    /// ViewModel for displaying user bot instances
    /// </summary>
    public class UserBotViewModel
    {
        public long UserBotId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string TradingBotName { get; set; } = string.Empty;
        public string Strategy { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public DateTime? LastRun { get; set; }
        public decimal WeeklyBuyAmount { get; set; }
        public decimal InvestmentAmount { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel for displaying trading sessions
    /// </summary>
    public class TradingSessionViewModel
    {
        public long SessionId { get; set; }
        public string UserBotName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Mode { get; set; } = string.Empty;
        public decimal InitialBalance { get; set; }
        public decimal? FinalBalance { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalTrades { get; set; }
        public decimal? ROI { get; set; }
    }

    /// <summary>
    /// Main view model for the Trading Bots page
    /// </summary>
    public class TradingBotsPageViewModel
    {
        /// <summary>
        /// Available trading bot templates
        /// </summary>
        public List<TradingBotViewModel> AvailableBots { get; set; } = new();

        /// <summary>
        /// User's active bot instances
        /// </summary>
        public List<UserBotViewModel> UserBots { get; set; } = new();

        /// <summary>
        /// Recent trading sessions
        /// </summary>
        public List<TradingSessionViewModel> TradingSessions { get; set; } = new();

        /// <summary>
        /// Form model for creating new user bots
        /// </summary>
        public CreateUserBotViewModel CreateUserBot { get; set; } = new();
    }
}