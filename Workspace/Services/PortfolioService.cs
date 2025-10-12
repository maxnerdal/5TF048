using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Security.Claims;

namespace WebApp.Services
{
    /// <summary>
    /// *** DAL SERVICE LAYER ***
    /// This is a key part of your Data Access Layer (DAL). It acts as a bridge between:
    /// - Your Controllers (UI layer) 
    /// - Your Database (via ApplicationDbContext)
    /// 
    /// Purpose: Abstract database operations into reusable, testable methods
    /// Benefits: Separation of concerns, easier testing, cleaner controllers
    /// </summary>
    public class PortfolioService : IPortfolioService
    {
        // *** DEPENDENCY INJECTION ***
        // This is your database context - the main DAL component
        // It's injected here so this service can perform database operations
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Constructor receives the database context through Dependency Injection
        /// This allows us to mock the context for unit testing
        /// </summary>
        public PortfolioService(ApplicationDbContext context)
        {
            _context = context; // Store reference to database context
        }

        /// <summary>
        /// *** READ OPERATION (DAL) ***
        /// Get all portfolio items for a specific user from database
        /// This demonstrates a complex database query with relationships
        /// </summary>
        public async Task<List<PortfolioItemViewModel>> GetUserPortfolioAsync(int userId)
        {
            // *** ENTITY FRAMEWORK QUERY ***
            var portfolioItems = await _context.Portfolio    // Start with Portfolio table
                .Include(p => p.DigitalAsset)                // *** JOIN *** - Load related DigitalAsset data (EAGER LOADING)
                .Where(p => p.UserId == userId)              // *** FILTER *** - Only this user's portfolio items
                .Select(p => new PortfolioItemViewModel      // *** PROJECTION *** - Transform database entities to ViewModels
                {
                    // Map database fields to ViewModel properties
                    Id = p.Id,
                    UserId = p.UserId,
                    AssetId = p.AssetId,
                    AssetName = p.DigitalAsset.Name,         // Data from related DigitalAsset table
                    AssetTicker = p.DigitalAsset.Ticker,     // Data from related DigitalAsset table
                    Quantity = p.Quantity,
                    BuyPrice = p.BuyPrice,
                    DatePurchased = p.DatePurchased,
                    DateLastUpdate = p.DateLastUpdate
                })
                .ToListAsync();                              // *** ASYNC EXECUTION *** - Don't block the thread

            return portfolioItems; // Return transformed data to controller
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
        /// *** CREATE OPERATION (DAL) ***
        /// Add a new portfolio item for a user to the database
        /// This shows how to INSERT data through Entity Framework
        /// </summary>
        public async Task<bool> AddPortfolioItemAsync(PortfolioItemViewModel item, int userId)
        {
            try
            {
                // *** ENTITY MAPPING *** - Convert ViewModel to Database Entity
                var portfolioEntity = new Portfolio
                {
                    UserId = userId,                          // Foreign key to Users table
                    AssetId = item.AssetId,                   // Foreign key to DigitalAssets table
                    Quantity = item.Quantity,
                    BuyPrice = item.BuyPrice,
                    DatePurchased = item.DatePurchased,
                    DateLastUpdate = DateTime.UtcNow          // Set audit timestamp
                };

                // *** ADD TO CONTEXT *** - Tell EF to track this new entity
                _context.Portfolio.Add(portfolioEntity);
                
                // *** SAVE TO DATABASE *** - Execute the INSERT statement
                // This is where the actual SQL is generated and executed
                await _context.SaveChangesAsync();
                
                return true; // Success
            }
            catch
            {
                // *** ERROR HANDLING *** - Catch database errors (foreign key violations, etc.)
                return false; // Failed
            }
        }

        /// <summary>
        /// *** UPDATE OPERATION (DAL) ***
        /// Update an existing portfolio item in the database
        /// This shows the typical pattern: Find -> Modify -> Save
        /// </summary>
        public async Task<bool> UpdatePortfolioItemAsync(PortfolioItemViewModel item, int userId)
        {
            try
            {
                // *** FIND EXISTING ENTITY *** - Load from database for modification
                var existingItem = await _context.Portfolio
                    .FirstOrDefaultAsync(p => p.Id == item.Id && p.UserId == userId);

                // *** SECURITY CHECK *** - Ensure user owns this portfolio item
                if (existingItem == null)
                    return false; // Item not found or doesn't belong to user

                // *** MODIFY TRACKED ENTITY *** - EF will detect these changes
                existingItem.AssetId = item.AssetId;
                existingItem.Quantity = item.Quantity;
                existingItem.BuyPrice = item.BuyPrice;
                existingItem.DatePurchased = item.DatePurchased;
                existingItem.DateLastUpdate = DateTime.UtcNow; // Update audit timestamp

                // *** SAVE CHANGES *** - EF generates and executes UPDATE statement
                // Only modified fields will be included in the SQL UPDATE
                await _context.SaveChangesAsync();
                
                return true; // Success
            }
            catch
            {
                // *** ERROR HANDLING *** - Database constraint violations, etc.
                return false; // Failed
            }
        }

        /// <summary>
        /// *** DELETE OPERATION (DAL) ***
        /// Delete a portfolio item from the database
        /// This completes the CRUD operations (Create, Read, Update, Delete)
        /// </summary>
        public async Task<bool> DeletePortfolioItemAsync(int id, int userId)
        {
            try
            {
                // *** FIND ENTITY TO DELETE *** - Must load it first
                var item = await _context.Portfolio
                    .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

                // *** SECURITY CHECK *** - Ensure user owns this item
                if (item == null)
                    return false; // Item not found or doesn't belong to user

                // *** MARK FOR DELETION *** - Tell EF to delete this entity
                _context.Portfolio.Remove(item);
                
                // *** EXECUTE DELETE *** - Generate and execute DELETE statement
                await _context.SaveChangesAsync();
                
                return true; // Success
            }
            catch
            {
                // *** ERROR HANDLING *** - Foreign key constraints might prevent deletion
                return false; // Failed
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
