using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    /// <summary>
    /// View model for displaying and editing portfolio items
    /// Combines database Portfolio and DigitalAsset data with UI-specific properties
    /// </summary>
    public class PortfolioItemViewModel
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        [Required(ErrorMessage = "Asset is required")]
        [Display(Name = "Digital Asset")]
        public int AssetId { get; set; }
        
        [Display(Name = "Asset Name")]
        public string AssetName { get; set; } = string.Empty;
        
        [Display(Name = "Ticker")]
        public string AssetTicker { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Quantity is required")]
        [Range(0.00000001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        [Display(Name = "Quantity")]
        public decimal Quantity { get; set; }
        
        [Required(ErrorMessage = "Buy price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Buy price must be greater than 0")]
        [Display(Name = "Buy Price ($)")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal BuyPrice { get; set; }
        
        [Required(ErrorMessage = "Purchase date is required")]
        [Display(Name = "Date Purchased")]
        [DataType(DataType.Date)]
        public DateTime DatePurchased { get; set; } = DateTime.Today;
        
        public DateTime DateLastUpdate { get; set; }
        
        // Calculated properties
        [Display(Name = "Total Investment")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal TotalInvestment => Quantity * BuyPrice;
        
        // This will be calculated with current price from API
        public decimal CurrentPrice { get; set; }
        
        [Display(Name = "Current Value")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal CurrentValue => Quantity * CurrentPrice;
        
        [Display(Name = "Profit/Loss")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal ProfitLoss => CurrentValue - TotalInvestment;
        
        [Display(Name = "Profit/Loss %")]
        [DisplayFormat(DataFormatString = "{0:P2}")]
        public decimal ProfitLossPercentage => TotalInvestment > 0 ? ProfitLoss / TotalInvestment : 0;
    }
}
