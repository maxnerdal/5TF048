using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApp.Models
{
    [Table("PortfolioItems")]
    public class PortfolioItem
    {
        public long Id { get; set; }
        
        [Required]
        public long PortfolioId { get; set; }
        
        [Required]
        public long DigitalAssetId { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,8)")]
        [Range(0.00000001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        [Display(Name = "Quantity")]
        public decimal Quantity { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,8)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Purchase price must be greater than 0")]
        [Display(Name = "Purchase Price ($)")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal PurchasePrice { get; set; }
        
        [Required]
        [Display(Name = "Purchase Date")]
        [DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; } = DateTime.Today;
        
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Portfolio Portfolio { get; set; } = null!;
        public virtual DigitalAsset DigitalAsset { get; set; } = null!;
    }
}
