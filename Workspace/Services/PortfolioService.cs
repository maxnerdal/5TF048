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
        public async Task<List<PortfolioItemViewModel>> GetUserPortfolioAsync(long userId)
        {
            // *** ENTITY FRAMEWORK QUERY ***
            var portfolioItems = await _context.PortfolioItems    // Start with PortfolioItems table
                .Include(p => p.DigitalAsset)                // *** JOIN *** - Load related DigitalAsset data (EAGER LOADING)
                .Include(p => p.Portfolio)                   // *** JOIN *** - Load related Portfolio data
                .Where(p => p.Portfolio.UserId == userId)    // *** FILTER *** - Only this user's portfolio items
                .Select(p => new PortfolioItemViewModel      // *** PROJECTION *** - Transform database entities to ViewModels
                {
                    // Map database fields to ViewModel properties
                    Id = p.Id,
                    UserId = p.Portfolio.UserId,
                    AssetId = p.DigitalAsset.Id,
                    AssetName = p.DigitalAsset.Name,         // Data from related DigitalAsset table
                    AssetTicker = p.DigitalAsset.Symbol,     // Data from related DigitalAsset table
                    Quantity = p.Quantity,
                    BuyPrice = p.PurchasePrice,
                    DatePurchased = p.PurchaseDate,
                    DateLastUpdate = p.UpdatedAt
                })
                .ToListAsync();                              // *** ASYNC EXECUTION *** - Don't block the thread

            return portfolioItems; // Return transformed data to controller
        }

        /// <summary>
        /// Get a specific portfolio item for a user
        /// </summary>
        public async Task<PortfolioItemViewModel?> GetPortfolioItemAsync(long id, long userId)
        {
            var portfolioItem = await _context.PortfolioItems
                .Include(p => p.DigitalAsset)
                .Include(p => p.Portfolio)
                .Where(p => p.Id == id && p.Portfolio.UserId == userId)
                .Select(p => new PortfolioItemViewModel
                {
                    Id = p.Id,
                    UserId = p.Portfolio.UserId,
                    AssetId = p.DigitalAsset.Id,
                    AssetName = p.DigitalAsset.Name,
                    AssetTicker = p.DigitalAsset.Symbol,
                    Quantity = p.Quantity,
                    BuyPrice = p.PurchasePrice,
                    DatePurchased = p.PurchaseDate,
                    DateLastUpdate = p.UpdatedAt
                })
                .FirstOrDefaultAsync();

            return portfolioItem;
        }

        /// <summary>
        /// *** CREATE OPERATION (DAL) ***
        /// Add a new portfolio item for a user to the database
        /// This shows how to INSERT data through Entity Framework
        /// </summary>
        public async Task<bool> AddPortfolioItemAsync(PortfolioItemViewModel item, long userId)
        {
            try
            {
                // *** GET OR CREATE DEFAULT PORTFOLIO *** 
                var portfolio = await _context.Portfolios
                    .FirstOrDefaultAsync(p => p.UserId == userId);
                
                if (portfolio == null)
                {
                    // Create default portfolio for user
                    portfolio = new Portfolio
                    {
                        UserId = userId,
                        Name = "My Portfolio",
                        Description = "Default portfolio",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Portfolios.Add(portfolio);
                    await _context.SaveChangesAsync(); // Save to get the Portfolio ID
                }

                // *** ENTITY MAPPING *** - Convert ViewModel to Database Entity
                var portfolioItemEntity = new PortfolioItem
                {
                    PortfolioId = portfolio.Id,               // Foreign key to Portfolios table
                    DigitalAssetId = item.AssetId,            // Foreign key to DigitalAssets table
                    Quantity = item.Quantity,
                    PurchasePrice = item.BuyPrice,
                    PurchaseDate = item.DatePurchased,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // *** ADD TO CONTEXT *** - Tell EF to track this new entity
                _context.PortfolioItems.Add(portfolioItemEntity);
                
                // *** SAVE TO DATABASE *** - Execute the INSERT statement
                // This is where the actual SQL is generated and executed
                await _context.SaveChangesAsync();
                
                return true; // Success
            }
            catch (Exception ex)
            {
                // *** ERROR HANDLING *** - Catch database errors (foreign key violations, etc.)
                // Log the actual error for debugging
                Console.WriteLine($"Error adding portfolio item: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false; // Failed
            }
        }

        /// <summary>
        /// *** UPDATE OPERATION (DAL) ***
        /// Update an existing portfolio item in the database
        /// This shows the typical pattern: Find -> Modify -> Save
        /// </summary>
        public async Task<bool> UpdatePortfolioItemAsync(PortfolioItemViewModel item, long userId)
        {
            try
            {
                // *** FIND EXISTING ENTITY *** - Load from database for modification
                var existingItem = await _context.PortfolioItems
                    .Include(p => p.Portfolio)
                    .FirstOrDefaultAsync(p => p.Id == item.Id && p.Portfolio.UserId == userId);

                // *** SECURITY CHECK *** - Ensure user owns this portfolio item
                if (existingItem == null)
                    return false; // Item not found or doesn't belong to user

                // *** MODIFY TRACKED ENTITY *** - EF will detect these changes
                existingItem.DigitalAssetId = item.AssetId;
                existingItem.Quantity = item.Quantity;
                existingItem.PurchasePrice = item.BuyPrice;
                existingItem.PurchaseDate = item.DatePurchased;
                existingItem.UpdatedAt = DateTime.UtcNow; // Update audit timestamp

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
        public async Task<bool> DeletePortfolioItemAsync(long id, long userId)
        {
            try
            {
                // *** FIND ENTITY TO DELETE *** - Must load it first
                var item = await _context.PortfolioItems
                    .Include(p => p.Portfolio)
                    .FirstOrDefaultAsync(p => p.Id == id && p.Portfolio.UserId == userId);

                // *** SECURITY CHECK *** - Ensure user owns this item
                if (item == null)
                    return false; // Item not found or doesn't belong to user

                // *** MARK FOR DELETION *** - Tell EF to delete this entity
                _context.PortfolioItems.Remove(item);
                
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
            var assets = await _context.DigitalAssets
                .OrderBy(a => a.Name)
                .ToListAsync();
            
            return assets ?? new List<DigitalAsset>();
        }

        /// <summary>
        /// Get a digital asset by ticker symbol
        /// </summary>
        public async Task<DigitalAsset?> GetAssetByTickerAsync(string ticker)
        {
            return await _context.DigitalAssets
                .FirstOrDefaultAsync(a => a.Symbol.ToUpper() == ticker.ToUpper());
        }
    }
}
