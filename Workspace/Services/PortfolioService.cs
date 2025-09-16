using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Security.Claims;

namespace WebApp.Services
{
    /// <summary>
    /// Service for managing portfolio operations with database
    /// </summary>
    public class PortfolioService : IPortfolioService
    {
        private readonly ApplicationDbContext _context;

        public PortfolioService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all portfolio items for a specific user
        /// </summary>
        public async Task<List<PortfolioItemViewModel>> GetUserPortfolioAsync(int userId)
        {
            var portfolioItems = await _context.Portfolio
                .Include(p => p.DigitalAsset)
                .Where(p => p.UserId == userId)
                .Select(p => new PortfolioItemViewModel
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    AssetId = p.AssetId,
                    AssetName = p.DigitalAsset.Name,
                    AssetTicker = p.DigitalAsset.Ticker,
                    Quantity = p.Quantity,
                    BuyPrice = p.BuyPrice,
                    DatePurchased = p.DatePurchased,
                    DateLastUpdate = p.DateLastUpdate
                })
                .ToListAsync();

            return portfolioItems;
        }

        /// <summary>
        /// Get a specific portfolio item for a user
        /// </summary>
        public async Task<PortfolioItemViewModel?> GetPortfolioItemAsync(int id, int userId)
        {
            var portfolioItem = await _context.Portfolio
                .Include(p => p.DigitalAsset)
                .Where(p => p.Id == id && p.UserId == userId)
                .Select(p => new PortfolioItemViewModel
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    AssetId = p.AssetId,
                    AssetName = p.DigitalAsset.Name,
                    AssetTicker = p.DigitalAsset.Ticker,
                    Quantity = p.Quantity,
                    BuyPrice = p.BuyPrice,
                    DatePurchased = p.DatePurchased,
                    DateLastUpdate = p.DateLastUpdate
                })
                .FirstOrDefaultAsync();

            return portfolioItem;
        }

        /// <summary>
        /// Add a new portfolio item for a user
        /// </summary>
        public async Task<bool> AddPortfolioItemAsync(PortfolioItemViewModel item, int userId)
        {
            try
            {
                var portfolioEntity = new Portfolio
                {
                    UserId = userId,
                    AssetId = item.AssetId,
                    Quantity = item.Quantity,
                    BuyPrice = item.BuyPrice,
                    DatePurchased = item.DatePurchased,
                    DateLastUpdate = DateTime.UtcNow
                };

                _context.Portfolio.Add(portfolioEntity);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Update an existing portfolio item
        /// </summary>
        public async Task<bool> UpdatePortfolioItemAsync(PortfolioItemViewModel item, int userId)
        {
            try
            {
                var existingItem = await _context.Portfolio
                    .FirstOrDefaultAsync(p => p.Id == item.Id && p.UserId == userId);

                if (existingItem == null)
                    return false;

                existingItem.AssetId = item.AssetId;
                existingItem.Quantity = item.Quantity;
                existingItem.BuyPrice = item.BuyPrice;
                existingItem.DatePurchased = item.DatePurchased;
                existingItem.DateLastUpdate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Delete a portfolio item
        /// </summary>
        public async Task<bool> DeletePortfolioItemAsync(int id, int userId)
        {
            try
            {
                var item = await _context.Portfolio
                    .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

                if (item == null)
                    return false;

                _context.Portfolio.Remove(item);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get all available digital assets
        /// </summary>
        public async Task<List<DigitalAsset>> GetAvailableAssetsAsync()
        {
            return await _context.DigitalAssets
                .OrderBy(a => a.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Get a digital asset by ticker symbol
        /// </summary>
        public async Task<DigitalAsset?> GetAssetByTickerAsync(string ticker)
        {
            return await _context.DigitalAssets
                .FirstOrDefaultAsync(a => a.Ticker.ToUpper() == ticker.ToUpper());
        }
    }
}
