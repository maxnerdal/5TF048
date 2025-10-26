using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Data
{
    /// <summary>
    /// ApplicationDbContext manages the database connection and entities.
    /// This is the main class that coordinates Entity Framework functionality for your application.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Constructor that receives database configuration options
        /// </summary>
        /// <param name="options">Database configuration options from dependency injection</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// DbSet for Users table - this represents the Users table in the database
        /// EF Core will automatically create, read, update, and delete operations for this table
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// DbSet for DigitalAssets table - represents available cryptocurrencies
        /// </summary>
        public DbSet<DigitalAsset> DigitalAssets { get; set; }

        /// <summary>
        /// DbSet for Portfolios table - represents user portfolios
        /// </summary>
        public DbSet<Portfolio> Portfolios { get; set; }

        /// <summary>
        /// DbSet for PortfolioItems table - represents individual portfolio entries
        /// </summary>
        public DbSet<PortfolioItem> PortfolioItems { get; set; }

        /// <summary>
        /// DbSet for MarketData table - represents historical candlestick data for backtesting
        /// </summary>
        public DbSet<MarketData> MarketData { get; set; }

        /// <summary>
        /// DbSet for TradingBots table - represents available trading bot templates
        /// </summary>
        public DbSet<TradingBot> TradingBots { get; set; }

        /// <summary>
        /// DbSet for UserBots table - represents user instances of trading bot templates
        /// </summary>
        public DbSet<UserBot> UserBots { get; set; }

        /// <summary>
        /// DbSet for BotConfigurations table - represents trading bot configuration settings
        /// </summary>
        public DbSet<BotConfiguration> BotConfigurations { get; set; }

        /// <summary>
        /// DbSet for TradingSessions table - represents backtesting or live trading sessions
        /// </summary>
        public DbSet<TradingSession> TradingSessions { get; set; }

        /// <summary>
        /// DbSet for Trades table - represents individual buy/sell transactions
        /// </summary>
        public DbSet<Trade> Trades { get; set; }

        /// <summary>
        /// DbSet for PerformanceMetrics table - represents calculated performance statistics
        /// </summary>
        public DbSet<PerformanceMetric> PerformanceMetrics { get; set; }

        /// <summary>
        /// Configure entity relationships and database constraints
        /// This method is called when EF Core is building the data model
        /// </summary>
        /// <param name="modelBuilder">Builder to configure the database model</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                // Set the primary key
                entity.HasKey(u => u.Id);

                // Configure Username as unique and required
                entity.HasIndex(u => u.Username)
                      .IsUnique()
                      .HasDatabaseName("IX_Users_Username");

                // Configure Email as unique and required
                entity.HasIndex(u => u.Email)
                      .IsUnique()
                      .HasDatabaseName("IX_Users_Email");

                // Set maximum length for string properties
                entity.Property(u => u.Username)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(u => u.Email)
                      .HasMaxLength(256)
                      .IsRequired();

                entity.Property(u => u.PasswordHash)
                      .HasMaxLength(500)
                      .IsRequired();

                // Set default value for CreatedAt
                entity.Property(u => u.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");
            });

            // Configure DigitalAsset entity
            modelBuilder.Entity<DigitalAsset>(entity =>
            {
                entity.HasKey(d => d.Id);

                entity.Property(d => d.Name)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(d => d.Symbol)
                      .HasColumnName("Symbol")
                      .HasMaxLength(10)
                      .IsRequired();

                // Create unique index on symbol
                entity.HasIndex(d => d.Symbol)
                      .IsUnique()
                      .HasDatabaseName("UQ_DigitalAssets_Symbol");
            });

            // Configure Portfolio entity  
            modelBuilder.Entity<Portfolio>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.ToTable("Portfolios");

                entity.Property(p => p.Name)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(p => p.Description)
                      .HasMaxLength(500);

                entity.Property(p => p.CreatedAt)
                      .HasDefaultValueSql("GETDATE()");

                entity.Property(p => p.UpdatedAt)
                      .HasDefaultValueSql("GETDATE()");

                // Configure foreign key relationships
                entity.HasOne(p => p.User)
                      .WithMany(u => u.Portfolios)
                      .HasForeignKey(p => p.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Create index for user portfolio lookups
                entity.HasIndex(p => p.UserId)
                      .HasDatabaseName("IX_Portfolios_UserId");
            });

            // Configure PortfolioItem entity
            modelBuilder.Entity<PortfolioItem>(entity =>
            {
                entity.HasKey(pi => pi.Id);
                entity.ToTable("PortfolioItems");

                entity.Property(pi => pi.Quantity)
                      .HasColumnType("decimal(18,8)")
                      .IsRequired();

                entity.Property(pi => pi.PurchasePrice)
                      .HasColumnType("decimal(18,8)")
                      .IsRequired();

                entity.Property(pi => pi.PurchaseDate)
                      .IsRequired();

                entity.Property(pi => pi.CreatedAt)
                      .HasDefaultValueSql("GETDATE()");

                entity.Property(pi => pi.UpdatedAt)
                      .HasDefaultValueSql("GETDATE()");

                // Configure foreign key relationships
                entity.HasOne(pi => pi.Portfolio)
                      .WithMany(p => p.PortfolioItems)
                      .HasForeignKey(pi => pi.PortfolioId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pi => pi.DigitalAsset)
                      .WithMany(da => da.PortfolioItems)
                      .HasForeignKey(pi => pi.DigitalAssetId)
                      .HasPrincipalKey(da => da.Id)
                      .OnDelete(DeleteBehavior.Restrict);

                // Create indexes
                entity.HasIndex(pi => pi.PortfolioId)
                      .HasDatabaseName("IX_PortfolioItems_PortfolioId");
            });

            // Configure MarketData entity
            modelBuilder.Entity<MarketData>(entity =>
            {
                entity.ToTable("MarketData");
                
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id)
                      .HasColumnName("Id")
                      .ValueGeneratedOnAdd();
                      
                entity.Property(e => e.Symbol)
                      .HasColumnName("Symbol")
                      .HasMaxLength(20)
                      .IsRequired();
                      
                entity.Property(e => e.TimeFrame)
                      .HasColumnName("TimeFrame")
                      .HasMaxLength(10)
                      .IsRequired();
                      
                entity.Property(e => e.OpenTime)
                      .HasColumnName("OpenTime")
                      .IsRequired();
                      
                entity.Property(e => e.OpenPrice)
                      .HasColumnName("OpenPrice")
                      .HasColumnType("decimal(18,8)")
                      .IsRequired();
                      
                entity.Property(e => e.HighPrice)
                      .HasColumnName("HighPrice")
                      .HasColumnType("decimal(18,8)")
                      .IsRequired();
                      
                entity.Property(e => e.LowPrice)
                      .HasColumnName("LowPrice")
                      .HasColumnType("decimal(18,8)")
                      .IsRequired();
                      
                entity.Property(e => e.ClosePrice)
                      .HasColumnName("ClosePrice")
                      .HasColumnType("decimal(18,8)")
                      .IsRequired();
                      
                entity.Property(e => e.Volume)
                      .HasColumnName("Volume")
                      .HasColumnType("decimal(18,8)")
                      .IsRequired();
                      
                entity.Property(e => e.CloseTime)
                      .HasColumnName("CloseTime")
                      .IsRequired();
                      
                entity.Property(e => e.CreatedAt)
                      .HasColumnName("CreatedAt")
                      .HasDefaultValueSql("GETUTCDATE()")
                      .IsRequired();

                // Create indexes for performance
                entity.HasIndex(e => new { e.Symbol, e.TimeFrame, e.OpenTime })
                      .HasDatabaseName("IX_MarketData_Symbol_TimeFrame_OpenTime");
                      
                entity.HasIndex(e => e.OpenTime)
                      .HasDatabaseName("IX_MarketData_OpenTime");
            });

            // Configure TradingBot entity
            modelBuilder.Entity<TradingBot>(entity =>
            {
                entity.ToTable("TradingBots");
                entity.HasKey(e => e.BotId);
                
                entity.Property(e => e.Name)
                      .HasMaxLength(100)
                      .IsRequired();
                      
                entity.Property(e => e.Strategy)
                      .HasMaxLength(50)
                      .IsRequired();
                      
                entity.Property(e => e.Description)
                      .HasMaxLength(500);
                      
                entity.Property(e => e.Created)
                      .HasDefaultValueSql("GETUTCDATE()");
            });

            // Configure UserBot entity
            modelBuilder.Entity<UserBot>(entity =>
            {
                entity.ToTable("UserBots");
                entity.HasKey(e => e.UserBotId);
                
                entity.Property(e => e.Name)
                      .HasMaxLength(100)
                      .IsRequired();
                      
                entity.Property(e => e.Status)
                      .HasMaxLength(20)
                      .HasDefaultValue("Inactive")
                      .IsRequired();
                      
                entity.Property(e => e.Created)
                      .HasDefaultValueSql("GETUTCDATE()");

                // Configure relationships
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.HasOne(e => e.TradingBot)
                      .WithMany(t => t.UserBots)
                      .HasForeignKey(e => e.BotId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Create indexes
                entity.HasIndex(e => e.UserId)
                      .HasDatabaseName("IX_UserBots_UserId");
                      
                entity.HasIndex(e => e.BotId)
                      .HasDatabaseName("IX_UserBots_BotId");
            });

            // Configure BotConfiguration entity
            modelBuilder.Entity<BotConfiguration>(entity =>
            {
                entity.ToTable("BotConfigurations");
                entity.HasKey(e => e.ConfigId);
                
                // Updated for simplified JSON-based configuration
                entity.Property(e => e.Parameters)
                      .HasColumnType("nvarchar(max)");
                
                // OLD PROPERTIES - COMMENTED OUT FOR NEW JSON ARCHITECTURE
                /*
                entity.Property(e => e.WeeklyBuyAmount)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();
                      
                entity.Property(e => e.InvestmentAmount)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();
                      
                entity.Property(e => e.StartDay)
                      .HasDefaultValue(0);
                      
                entity.Property(e => e.RiskLevel)
                      .HasMaxLength(20)
                      .HasDefaultValue("Medium")
                      .IsRequired();
                */

                // Configure relationships
                entity.HasOne(e => e.UserBot)
                      .WithMany(u => u.BotConfigurations)
                      .HasForeignKey(e => e.UserBotId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure TradingSession entity
            modelBuilder.Entity<TradingSession>(entity =>
            {
                entity.ToTable("TradingSessions");
                entity.HasKey(e => e.SessionId);
                
                entity.Property(e => e.StartTime)
                      .HasDefaultValueSql("GETUTCDATE()")
                      .IsRequired();
                      
                entity.Property(e => e.Mode)
                      .HasMaxLength(20)
                      .HasDefaultValue("Backtest")
                      .IsRequired();
                      
                entity.Property(e => e.InitialBalance)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();
                      
                entity.Property(e => e.FinalBalance)
                      .HasColumnType("decimal(18,2)");
                      
                entity.Property(e => e.Status)
                      .HasMaxLength(20)
                      .HasDefaultValue("Running")
                      .IsRequired();

                // Configure relationships
                entity.HasOne(e => e.UserBot)
                      .WithMany(u => u.TradingSessions)
                      .HasForeignKey(e => e.UserBotId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Create indexes
                entity.HasIndex(e => e.UserBotId)
                      .HasDatabaseName("IX_TradingSessions_UserBotId");
            });

            // Configure Trade entity
            modelBuilder.Entity<Trade>(entity =>
            {
                entity.ToTable("Trades");
                entity.HasKey(e => e.TradeId);
                
                entity.Property(e => e.Symbol)
                      .HasMaxLength(10)
                      .IsRequired();
                      
                entity.Property(e => e.Side)
                      .HasMaxLength(10)
                      .IsRequired();
                      
                entity.Property(e => e.Price)
                      .HasColumnType("decimal(18,8)")
                      .IsRequired();
                      
                entity.Property(e => e.Quantity)
                      .HasColumnType("decimal(18,8)")
                      .IsRequired();
                      
                entity.Property(e => e.Value)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();
                      
                entity.Property(e => e.Fee)
                      .HasColumnType("decimal(18,8)")
                      .HasDefaultValue(0);
                      
                entity.Property(e => e.Timestamp)
                      .HasDefaultValueSql("GETUTCDATE()")
                      .IsRequired();

                // Configure relationships
                entity.HasOne(e => e.TradingSession)
                      .WithMany(t => t.Trades)
                      .HasForeignKey(e => e.SessionId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Create indexes
                entity.HasIndex(e => e.SessionId)
                      .HasDatabaseName("IX_Trades_SessionId");
                      
                entity.HasIndex(e => e.Timestamp)
                      .HasDatabaseName("IX_Trades_Timestamp");
            });

            // Configure PerformanceMetric entity
            modelBuilder.Entity<PerformanceMetric>(entity =>
            {
                entity.ToTable("PerformanceMetrics");
                entity.HasKey(e => e.MetricId);
                
                entity.Property(e => e.TotalInvested)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();
                      
                entity.Property(e => e.TotalValue)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();
                      
                entity.Property(e => e.ROI)
                      .HasColumnType("decimal(8,4)")
                      .IsRequired();
                      
                entity.Property(e => e.WinRate)
                      .HasColumnType("decimal(5,2)");
                      
                entity.Property(e => e.UpdatedAt)
                      .HasDefaultValueSql("GETUTCDATE()")
                      .IsRequired();

                // Configure relationships
                entity.HasOne(e => e.TradingSession)
                      .WithMany(t => t.PerformanceMetrics)
                      .HasForeignKey(e => e.SessionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            
        }
    }
}
