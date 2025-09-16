using WebApp.Models;

namespace WebApp.Services
{
    /// <summary>
    /// Interface for portfolio service operations
    /// </summary>
    public interface IPortfolioService
    {
        Task<List<PortfolioItemViewModel>> GetUserPortfolioAsync(int userId);
        Task<PortfolioItemViewModel?> GetPortfolioItemAsync(int id, int userId);
        Task<bool> AddPortfolioItemAsync(PortfolioItemViewModel item, int userId);
        Task<bool> UpdatePortfolioItemAsync(PortfolioItemViewModel item, int userId);
        Task<bool> DeletePortfolioItemAsync(int id, int userId);
        Task<List<DigitalAsset>> GetAvailableAssetsAsync();
        Task<DigitalAsset?> GetAssetByTickerAsync(string ticker);
    }
}
