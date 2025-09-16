using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApp.Models
{
    [Table("Portfolio")]
    public class Portfolio
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("asset_id")]
        public int AssetId { get; set; }

        [Required]
        [Column("qty", TypeName = "decimal(18,8)")]
        public decimal Quantity { get; set; }

        [Required]
        [Column("buyprice", TypeName = "decimal(18,8)")]
        public decimal BuyPrice { get; set; }

        [Required]
        [Column("datepurchased")]
        public DateTime DatePurchased { get; set; }

        [Column("datelastupdate")]
        public DateTime DateLastUpdate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("AssetId")]
        public virtual DigitalAsset DigitalAsset { get; set; } = null!;
    }
}
