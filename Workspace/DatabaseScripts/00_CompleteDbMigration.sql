-- =============================================
-- Database Migration Script for CryptoBot Project
-- Run this script on a new SQL Server instance to recreate the complete database structure
-- =============================================

USE master;
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'MyFirstDatabase')
BEGIN
    CREATE DATABASE MyFirstDatabase;
    PRINT 'Database MyFirstDatabase created successfully.';
END
ELSE
BEGIN
    PRINT 'Database MyFirstDatabase already exists.';
END
GO

USE MyFirstDatabase;
GO

-- =============================================
-- 1. Users Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id] bigint IDENTITY(1,1) NOT NULL,
        [Username] nvarchar(100) NOT NULL,
        [Email] nvarchar(255) NOT NULL,
        [PasswordHash] nvarchar(255) NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] datetime2(7) NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_Users_Username] UNIQUE ([Username]),
        CONSTRAINT [UQ_Users_Email] UNIQUE ([Email])
    );
    PRINT 'Users table created successfully.';
END
ELSE
BEGIN
    PRINT 'Users table already exists.';
END
GO

-- =============================================
-- 2. DigitalAssets Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DigitalAssets]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DigitalAssets] (
        [Id] bigint IDENTITY(1,1) NOT NULL,
        [Symbol] nvarchar(10) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_DigitalAssets] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_DigitalAssets_Symbol] UNIQUE ([Symbol])
    );
    PRINT 'DigitalAssets table created successfully.';
END
ELSE
BEGIN
    PRINT 'DigitalAssets table already exists.';
END
GO

-- =============================================
-- 3. Portfolios Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Portfolios]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Portfolios] (
        [Id] bigint IDENTITY(1,1) NOT NULL,
        [UserId] bigint NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] datetime2(7) NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_Portfolios] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Portfolios table created successfully.';
END
ELSE
BEGIN
    PRINT 'Portfolios table already exists.';
END
GO

-- =============================================
-- 4. PortfolioItems Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PortfolioItems]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PortfolioItems] (
        [Id] bigint IDENTITY(1,1) NOT NULL,
        [PortfolioId] bigint NOT NULL,
        [DigitalAssetId] bigint NOT NULL,
        [Quantity] decimal(18,8) NOT NULL,
        [PurchasePrice] decimal(18,8) NOT NULL,
        [PurchaseDate] datetime2(7) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] datetime2(7) NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_PortfolioItems] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'PortfolioItems table created successfully.';
END
ELSE
BEGIN
    PRINT 'PortfolioItems table already exists.';
END
GO

-- =============================================
-- 5. MarketData Table (for Bitcoin price data)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MarketData]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MarketData] (
        [Id] bigint IDENTITY(1,1) NOT NULL,
        [Symbol] nvarchar(10) NOT NULL,
        [TimeFrame] nvarchar(10) NOT NULL,
        [OpenTime] datetime2(7) NOT NULL,
        [OpenPrice] decimal(18,8) NOT NULL,
        [HighPrice] decimal(18,8) NOT NULL,
        [LowPrice] decimal(18,8) NOT NULL,
        [ClosePrice] decimal(18,8) NOT NULL,
        [Volume] decimal(18,8) NOT NULL,
        [CloseTime] datetime2(7) NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_MarketData] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'MarketData table created successfully.';
END
ELSE
BEGIN
    PRINT 'MarketData table already exists.';
END
GO

-- =============================================
-- CREATE FOREIGN KEY CONSTRAINTS
-- =============================================

-- Portfolios -> Users
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Portfolios_Users')
BEGIN
    ALTER TABLE [dbo].[Portfolios]
    ADD CONSTRAINT [FK_Portfolios_Users] FOREIGN KEY ([UserId])
    REFERENCES [dbo].[Users] ([Id])
    ON DELETE CASCADE;
    PRINT 'Foreign key FK_Portfolios_Users created.';
END

-- PortfolioItems -> Portfolios
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PortfolioItems_Portfolios')
BEGIN
    ALTER TABLE [dbo].[PortfolioItems]
    ADD CONSTRAINT [FK_PortfolioItems_Portfolios] FOREIGN KEY ([PortfolioId])
    REFERENCES [dbo].[Portfolios] ([Id])
    ON DELETE CASCADE;
    PRINT 'Foreign key FK_PortfolioItems_Portfolios created.';
END

-- PortfolioItems -> DigitalAssets
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PortfolioItems_DigitalAssets')
BEGIN
    ALTER TABLE [dbo].[PortfolioItems]
    ADD CONSTRAINT [FK_PortfolioItems_DigitalAssets] FOREIGN KEY ([DigitalAssetId])
    REFERENCES [dbo].[DigitalAssets] ([Id])
    ON DELETE CASCADE;
    PRINT 'Foreign key FK_PortfolioItems_DigitalAssets created.';
END

-- =============================================
-- CREATE INDEXES FOR PERFORMANCE
-- =============================================

-- Index on MarketData for efficient querying
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MarketData_Symbol_OpenTime')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MarketData_Symbol_OpenTime]
    ON [dbo].[MarketData] ([Symbol], [OpenTime]);
    PRINT 'Index IX_MarketData_Symbol_OpenTime created.';
END

-- Index on Users for login performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Email')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Users_Email]
    ON [dbo].[Users] ([Email]);
    PRINT 'Index IX_Users_Email created.';
END

-- Index on Portfolios for user queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Portfolios_UserId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Portfolios_UserId]
    ON [dbo].[Portfolios] ([UserId]);
    PRINT 'Index IX_Portfolios_UserId created.';
END

-- Index on PortfolioItems for portfolio queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PortfolioItems_PortfolioId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PortfolioItems_PortfolioId]
    ON [dbo].[PortfolioItems] ([PortfolioId]);
    PRINT 'Index IX_PortfolioItems_PortfolioId created.';
END

-- =============================================
-- INSERT INITIAL DATA
-- =============================================

-- Insert common digital assets
IF NOT EXISTS (SELECT * FROM [dbo].[DigitalAssets] WHERE Symbol = 'BTC')
BEGIN
    INSERT INTO [dbo].[DigitalAssets] ([Symbol], [Name], [Description])
    VALUES 
        ('BTC', 'Bitcoin', 'The first and largest cryptocurrency by market cap'),
        ('ETH', 'Ethereum', 'Smart contract platform and cryptocurrency'),
        ('ADA', 'Cardano', 'Proof-of-stake blockchain platform'),
        ('DOT', 'Polkadot', 'Multi-chain blockchain platform'),
        ('LINK', 'Chainlink', 'Decentralized oracle network'),
        ('SOL', 'Solana', 'High-performance blockchain platform'),
        ('AVAX', 'Avalanche', 'Platform for decentralized applications'),
        ('MATIC', 'Polygon', 'Ethereum scaling solution'),
        ('UNI', 'Uniswap', 'Decentralized exchange protocol'),
        ('AAVE', 'Aave', 'Decentralized lending protocol');
    
    PRINT 'Initial digital assets inserted successfully.';
END
ELSE
BEGIN
    PRINT 'Digital assets already exist, skipping initial data insertion.';
END

-- =============================================
-- CREATE SAMPLE TEST USER (Optional - for testing)
-- =============================================

-- Create test user if it doesn't exist
IF NOT EXISTS (SELECT * FROM [dbo].[Users] WHERE Username = 'testuser')
BEGIN
    INSERT INTO [dbo].[Users] ([Username], [Email], [PasswordHash])
    VALUES ('testuser', 'test@example.com', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f'); -- SHA256 of 'password123'
    
    DECLARE @UserId bigint = SCOPE_IDENTITY();
    
    -- Create sample portfolio
    INSERT INTO [dbo].[Portfolios] ([UserId], [Name], [Description])
    VALUES (@UserId, 'My Crypto Portfolio', 'Sample portfolio for testing');
    
    PRINT 'Test user and sample portfolio created successfully.';
    PRINT 'Test login: Username=testuser, Password=password123';
END
ELSE
BEGIN
    PRINT 'Test user already exists, skipping test data creation.';
END

-- =============================================
-- VERIFICATION QUERIES
-- =============================================

PRINT '';
PRINT '=== DATABASE MIGRATION COMPLETED ===';
PRINT '';

-- Show table counts
SELECT 'Users' as TableName, COUNT(*) as RecordCount FROM [dbo].[Users]
UNION ALL
SELECT 'DigitalAssets', COUNT(*) FROM [dbo].[DigitalAssets]
UNION ALL
SELECT 'Portfolios', COUNT(*) FROM [dbo].[Portfolios]
UNION ALL
SELECT 'PortfolioItems', COUNT(*) FROM [dbo].[PortfolioItems]
UNION ALL
SELECT 'MarketData', COUNT(*) FROM [dbo].[MarketData];

PRINT '';
PRINT 'Database structure created successfully!';
PRINT 'You can now run your ASP.NET Core application.';
PRINT '';

GO