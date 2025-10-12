using WebApp.Models;

namespace WebApp.Models
{
    /// <summary>
    /// View model for the market data management page
    /// </summary>
    public class MarketDataManagementViewModel
    {
        public MarketData? LatestBtcData { get; set; }
        public MarketData? OldestBtcData { get; set; }
        public int TotalBtcRecords { get; set; }
        public List<string> AvailableSymbols { get; set; } = new();
        
        public string DataRangeDisplay
        {
            get
            {
                if (OldestBtcData != null && LatestBtcData != null)
                {
                    return $"{OldestBtcData.OpenTime:yyyy-MM-dd HH:mm} to {LatestBtcData.CloseTime:yyyy-MM-dd HH:mm}";
                }
                return "No data available";
            }
        }
    }
}