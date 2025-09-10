using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class PortfolioItem
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Coin name is required")]
        [Display(Name = "Coin Name")]
        public string CoinName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Symbol is required")]
        [Display(Name = "Symbol")]
        [StringLength(10, ErrorMessage = "Symbol must be 10 characters or less")]
        public string Symbol { get; set; } = string.Empty;
        
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
        
        // Calculated properties
        [Display(Name = "Total Investment")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal TotalInvestment => Quantity * BuyPrice;
        
        // This will be calculated with current price later
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
