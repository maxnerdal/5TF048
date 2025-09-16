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
        /// DbSet for Portfolio table - represents user's cryptocurrency holdings
        /// </summary>
        public DbSet<Portfolio> Portfolio { get; set; }

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
                entity.HasKey(d => d.AssetId);

                entity.Property(d => d.Name)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(d => d.Ticker)
                      .HasMaxLength(10)
                      .IsRequired();

                // Create unique index on ticker
                entity.HasIndex(d => d.Ticker)
                      .IsUnique()
                      .HasDatabaseName("IX_DigitalAssets_Ticker");
            });

            // Configure Portfolio entity
            modelBuilder.Entity<Portfolio>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Quantity)
                      .HasColumnType("decimal(18,8)")
                      .IsRequired();

                entity.Property(p => p.DatePurchased)
                      .IsRequired();

                entity.Property(p => p.DateLastUpdate)
                      .HasDefaultValueSql("GETUTCDATE()");

                // Configure foreign key relationships
                entity.HasOne(p => p.User)
                      .WithMany(u => u.PortfolioItems)
                      .HasForeignKey(p => p.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.DigitalAsset)
                      .WithMany(d => d.Portfolios)
                      .HasForeignKey(p => p.AssetId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Create index for user portfolio lookups
                entity.HasIndex(p => p.UserId)
                      .HasDatabaseName("IX_Portfolio_UserId");
            });
        }
    }
}
