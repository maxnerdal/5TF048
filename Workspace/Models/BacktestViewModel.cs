using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class BacktestViewModel
    {
        [Required(ErrorMessage = "Please select a bot to backtest.")]
        public long UserBotId { get; set; }

        [Required(ErrorMessage = "Please select a start date.")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-30);

        [Required(ErrorMessage = "Please select an end date.")]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(-1);

        public string? BotName { get; set; }
    }
}