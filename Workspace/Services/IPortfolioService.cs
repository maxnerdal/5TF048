using WebApp.Models;

namespace WebApp.Services
{
    /// <summary>
    /// Interface for portfolio service operations
    /// </summary>
    public interface IPortfolioService
    {
        Task<List<PortfolioItemViewModel>> GetUserPortfolioAsync(long userId);
        Task<PortfolioItemViewModel?> GetPortfolioItemAsync(long id, long userId);
        Task<bool> AddPortfolioItemAsync(PortfolioItemViewModel item, long userId);
        Task<bool> UpdatePortfolioItemAsync(PortfolioItemViewModel item, long userId);
        Task<bool> DeletePortfolioItemAsync(long id, long userId);
        Task<List<DigitalAsset>> GetAvailableAssetsAsync();
        Task<DigitalAsset?> GetAssetByTickerAsync(string ticker);
    }
}
