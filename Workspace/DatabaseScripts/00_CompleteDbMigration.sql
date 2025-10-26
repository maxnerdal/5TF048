-- =============================================
-- Complete Database Migration Script for CryptoBot Project
-- This script will:
-- 1. Create database if needed
-- 2. Drop ALL tables except MarketData (preserving historical data)
-- 3. Recreate all tables with inline constraints
-- 4. Create indexes for performance
-- 5. Insert only DigitalAssets reference data
-- =============================================

USE master;
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'MyFirstDatabase')
BEGIN
    CREATE DATABASE MyFirstDatabase;
    PRINT '✅ Database MyFirstDatabase created successfully.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Database MyFirstDatabase already exists.';
END
GO

USE MyFirstDatabase;
GO

PRINT '';
PRINT '🚀 Starting complete database migration...';
PRINT '';

-- =============================================
-- STEP 1: DROP ALL TABLES EXCEPT MarketData
-- =============================================

PRINT '🗑️  Dropping existing tables (except MarketData)...';

-- Drop tables in reverse dependency order to avoid foreign key issues
DROP TABLE IF EXISTS [dbo].[PerformanceMetrics];
DROP TABLE IF EXISTS [dbo].[Trades];
DROP TABLE IF EXISTS [dbo].[TradingSessions];
DROP TABLE IF EXISTS [dbo].[BotConfigurations];
DROP TABLE IF EXISTS [dbo].[UserBots];
DROP TABLE IF EXISTS [dbo].[TradingBots];
DROP TABLE IF EXISTS [dbo].[PortfolioItems];
DROP TABLE IF EXISTS [dbo].[Portfolios];
DROP TABLE IF EXISTS [dbo].[DigitalAssets];
DROP TABLE IF EXISTS [dbo].[Users];

PRINT '✅ All tables dropped (MarketData preserved)';
PRINT '';

-- =============================================
-- STEP 2: CREATE CORE TABLES WITH INLINE CONSTRAINTS
-- =============================================

PRINT '🏗️  Creating core application tables...';

-- =============================================
-- 1. Users Table
-- =============================================
CREATE TABLE [dbo].[Users] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [Username] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(255) NOT NULL,
    [PasswordHash] NVARCHAR(255) NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(7) NOT NULL DEFAULT GETDATE(),
    
    -- Primary Key
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id]),
    
    -- Unique Constraints
    CONSTRAINT [UQ_Users_Username] UNIQUE ([Username]),
    CONSTRAINT [UQ_Users_Email] UNIQUE ([Email])
);
PRINT '✅ Users table created';

-- =============================================
-- 2. DigitalAssets Table
-- =============================================
CREATE TABLE [dbo].[DigitalAssets] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [Symbol] NVARCHAR(10) NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETDATE(),
    
    -- Primary Key
    CONSTRAINT [PK_DigitalAssets] PRIMARY KEY CLUSTERED ([Id]),
    
    -- Unique Constraints
    CONSTRAINT [UQ_DigitalAssets_Symbol] UNIQUE ([Symbol])
);
PRINT '✅ DigitalAssets table created';

-- =============================================
-- 3. Portfolios Table
-- =============================================
CREATE TABLE [dbo].[Portfolios] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [UserId] BIGINT NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(7) NOT NULL DEFAULT GETDATE(),
    
    -- Primary Key
    CONSTRAINT [PK_Portfolios] PRIMARY KEY CLUSTERED ([Id]),
    
    -- Foreign Keys
    CONSTRAINT [FK_Portfolios_Users] FOREIGN KEY ([UserId]) 
        REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
);
PRINT '✅ Portfolios table created';

-- =============================================
-- 4. PortfolioItems Table
-- =============================================
CREATE TABLE [dbo].[PortfolioItems] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [PortfolioId] BIGINT NOT NULL,
    [DigitalAssetId] BIGINT NOT NULL,
    [Quantity] DECIMAL(18,8) NOT NULL,
    [PurchasePrice] DECIMAL(18,8) NOT NULL,
    [PurchaseDate] DATETIME2(7) NOT NULL,
    [Notes] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2(7) NOT NULL DEFAULT GETDATE(),
    
    -- Primary Key
    CONSTRAINT [PK_PortfolioItems] PRIMARY KEY CLUSTERED ([Id]),
    
    -- Foreign Keys
    CONSTRAINT [FK_PortfolioItems_Portfolios] FOREIGN KEY ([PortfolioId]) 
        REFERENCES [dbo].[Portfolios]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PortfolioItems_DigitalAssets] FOREIGN KEY ([DigitalAssetId]) 
        REFERENCES [dbo].[DigitalAssets]([Id]) ON DELETE CASCADE
);
PRINT '✅ PortfolioItems table created';


-- =============================================
-- STEP 3: CREATE TRADING BOT TABLES
-- =============================================

PRINT '🤖 Creating trading bot tables...';

-- =============================================
-- 6. TradingBots Table (Bot Templates)
-- =============================================
CREATE TABLE [dbo].[TradingBots] (
    [BotId] BIGINT IDENTITY(1,1) NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Strategy] NVARCHAR(50) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [Created] DATETIME2(7) NOT NULL DEFAULT GETDATE(),
    
    -- Primary Key
    CONSTRAINT [PK_TradingBots] PRIMARY KEY CLUSTERED ([BotId])
);
PRINT '✅ TradingBots table created';

-- =============================================
-- 7. UserBots Table (User's Bot Instances)
-- =============================================
CREATE TABLE [dbo].[UserBots] (
    [UserBotId] BIGINT IDENTITY(1,1) NOT NULL,
    [UserId] BIGINT NOT NULL,
    [BotId] BIGINT NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'Inactive',
    [Created] DATETIME2(7) NOT NULL DEFAULT GETDATE(),
    [LastRun] DATETIME2(7) NULL,
    
    -- Primary Key
    CONSTRAINT [PK_UserBots] PRIMARY KEY CLUSTERED ([UserBotId]),
    
    -- Foreign Keys
    CONSTRAINT [FK_UserBots_Users] FOREIGN KEY ([UserId]) 
        REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserBots_TradingBots] FOREIGN KEY ([BotId]) 
        REFERENCES [dbo].[TradingBots]([BotId]) ON DELETE CASCADE
);
PRINT '✅ UserBots table created';

-- =============================================
-- 8. BotConfigurations Table
-- =============================================
CREATE TABLE [dbo].[BotConfigurations] (
    [ConfigId] BIGINT IDENTITY(1,1) NOT NULL,
    [UserBotId] BIGINT NOT NULL,
    [Parameters] NVARCHAR(MAX) NULL, -- JSON configuration
    
    -- Primary Key
    CONSTRAINT [PK_BotConfigurations] PRIMARY KEY CLUSTERED ([ConfigId]),
    
    -- Foreign Keys
    CONSTRAINT [FK_BotConfigurations_UserBots] FOREIGN KEY ([UserBotId]) 
        REFERENCES [dbo].[UserBots]([UserBotId]) ON DELETE CASCADE
);
PRINT '✅ BotConfigurations table created';

-- =============================================
-- 9. TradingSessions Table
-- =============================================
CREATE TABLE [dbo].[TradingSessions] (
    [SessionId] BIGINT IDENTITY(1,1) NOT NULL,
    [UserBotId] BIGINT NOT NULL,
    [StartTime] DATETIME2(7) NOT NULL,
    [EndTime] DATETIME2(7) NULL,
    [Mode] NVARCHAR(20) NOT NULL, -- 'Backtest' or 'Live'
    [InitialBalance] DECIMAL(18,2) NOT NULL,
    [FinalBalance] DECIMAL(18,2) NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'Running',
    
    -- Primary Key
    CONSTRAINT [PK_TradingSessions] PRIMARY KEY CLUSTERED ([SessionId]),
    
    -- Foreign Keys
    CONSTRAINT [FK_TradingSessions_UserBots] FOREIGN KEY ([UserBotId]) 
        REFERENCES [dbo].[UserBots]([UserBotId]) ON DELETE CASCADE
);
PRINT '✅ TradingSessions table created';

-- =============================================
-- 10. Trades Table
-- =============================================
CREATE TABLE [dbo].[Trades] (
    [TradeId] BIGINT IDENTITY(1,1) NOT NULL,
    [SessionId] BIGINT NOT NULL,
    [Symbol] NVARCHAR(10) NOT NULL,
    [Side] NVARCHAR(10) NOT NULL, -- 'BUY' or 'SELL'
    [Price] DECIMAL(18,8) NOT NULL,
    [Quantity] DECIMAL(18,8) NOT NULL,
    [Value] DECIMAL(18,2) NOT NULL, -- Price * Quantity
    [Fee] DECIMAL(18,8) NOT NULL DEFAULT 0,
    [Timestamp] DATETIME2(7) NOT NULL,
    
    -- Primary Key
    CONSTRAINT [PK_Trades] PRIMARY KEY CLUSTERED ([TradeId]),
    
    -- Foreign Keys
    CONSTRAINT [FK_Trades_TradingSessions] FOREIGN KEY ([SessionId]) 
        REFERENCES [dbo].[TradingSessions]([SessionId]) ON DELETE CASCADE
);
PRINT '✅ Trades table created';

-- =============================================
-- 11. PerformanceMetrics Table
-- =============================================
CREATE TABLE [dbo].[PerformanceMetrics] (
    [MetricId] BIGINT IDENTITY(1,1) NOT NULL,
    [SessionId] BIGINT NOT NULL,
    [TotalInvested] DECIMAL(18,2) NOT NULL,
    [TotalValue] DECIMAL(18,2) NOT NULL,
    [ROI] DECIMAL(8,4) NOT NULL, -- Return on Investment %
    [TotalTrades] INT NOT NULL,
    [WinRate] DECIMAL(5,2) NULL,
    [UpdatedAt] DATETIME2(7) NOT NULL DEFAULT GETDATE(),
    
    -- Primary Key
    CONSTRAINT [PK_PerformanceMetrics] PRIMARY KEY CLUSTERED ([MetricId]),
    
    -- Foreign Keys
    CONSTRAINT [FK_PerformanceMetrics_TradingSessions] FOREIGN KEY ([SessionId]) 
        REFERENCES [dbo].[TradingSessions]([SessionId]) ON DELETE CASCADE
);
PRINT '✅ PerformanceMetrics table created';
PRINT '';
-- =============================================
-- STEP 4: CREATE PERFORMANCE INDEXES
-- =============================================

PRINT '⚡ Creating performance indexes...';

-- User-related indexes
CREATE NONCLUSTERED INDEX [IX_Users_Email] 
    ON [dbo].[Users] ([Email]);

CREATE NONCLUSTERED INDEX [IX_Users_Username] 
    ON [dbo].[Users] ([Username]);

-- Portfolio indexes
CREATE NONCLUSTERED INDEX [IX_Portfolios_UserId] 
    ON [dbo].[Portfolios] ([UserId]);

CREATE NONCLUSTERED INDEX [IX_PortfolioItems_PortfolioId] 
    ON [dbo].[PortfolioItems] ([PortfolioId]);

-- Trading bot indexes
CREATE NONCLUSTERED INDEX [IX_UserBots_UserId] 
    ON [dbo].[UserBots] ([UserId]);

CREATE NONCLUSTERED INDEX [IX_UserBots_BotId_Status] 
    ON [dbo].[UserBots] ([BotId], [Status]);

CREATE NONCLUSTERED INDEX [IX_TradingSessions_UserBotId] 
    ON [dbo].[TradingSessions] ([UserBotId]);

CREATE NONCLUSTERED INDEX [IX_TradingSessions_Mode_Status] 
    ON [dbo].[TradingSessions] ([Mode], [Status]);

-- Trade indexes (critical for performance)
CREATE NONCLUSTERED INDEX [IX_Trades_SessionId] 
    ON [dbo].[Trades] ([SessionId]);

CREATE NONCLUSTERED INDEX [IX_Trades_Timestamp] 
    ON [dbo].[Trades] ([Timestamp]);

CREATE NONCLUSTERED INDEX [IX_Trades_Symbol_Timestamp] 
    ON [dbo].[Trades] ([Symbol], [Timestamp]);

-- Performance metrics indexes
CREATE NONCLUSTERED INDEX [IX_PerformanceMetrics_SessionId] 
    ON [dbo].[PerformanceMetrics] ([SessionId]);

PRINT '✅ All performance indexes created';
PRINT '';

-- =============================================
-- STEP 5: INSERT REFERENCE DATA
-- =============================================

PRINT '📊 Inserting reference data...';

-- Insert trading bot templates
INSERT INTO [dbo].[TradingBots] ([Name], [Strategy], [Description])
VALUES 
    ('Dollar Cost Average', 'DCA', 'Buy Bitcoin at regular intervals regardless of price')

PRINT '✅ Trading bot templates inserted';

-- Insert digital assets (cryptocurrency reference data)
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
    ('AAVE', 'Aave', 'Decentralized lending protocol'),
    ('XRP', 'Ripple', 'Digital payment protocol'),
    ('DOGE', 'Dogecoin', 'Meme-based cryptocurrency'),
    ('SHIB', 'Shiba Inu', 'Community-driven cryptocurrency'),
    ('LTC', 'Litecoin', 'Peer-to-peer cryptocurrency'),
    ('BCH', 'Bitcoin Cash', 'Bitcoin fork with larger block size');

PRINT '✅ Digital assets reference data inserted';
PRINT '';

-- =============================================
-- STEP 6: VERIFICATION & SUMMARY
-- =============================================

PRINT '🔍 Verifying database structure...';
PRINT '';

-- Show table counts and structure
SELECT 
    'Users' as TableName, 
    COUNT(*) as RecordCount,
    'User accounts and authentication' as Description
FROM [dbo].[Users]
UNION ALL
SELECT 'DigitalAssets', COUNT(*), 'Cryptocurrency reference data' FROM [dbo].[DigitalAssets]
UNION ALL
SELECT 'Portfolios', COUNT(*), 'User portfolio containers' FROM [dbo].[Portfolios]
UNION ALL
SELECT 'PortfolioItems', COUNT(*), 'Individual holdings in portfolios' FROM [dbo].[PortfolioItems]
UNION ALL
SELECT 'MarketData', COUNT(*), '🔥 PRESERVED: Historical price data' FROM [dbo].[MarketData]
UNION ALL
SELECT 'TradingBots', COUNT(*), 'Bot templates and strategies' FROM [dbo].[TradingBots]
UNION ALL
SELECT 'UserBots', COUNT(*), 'User bot instances' FROM [dbo].[UserBots]
UNION ALL
SELECT 'BotConfigurations', COUNT(*), 'Bot configuration settings' FROM [dbo].[BotConfigurations]
UNION ALL
SELECT 'TradingSessions', COUNT(*), 'Trading session records' FROM [dbo].[TradingSessions]
UNION ALL
SELECT 'Trades', COUNT(*), 'Individual trade executions' FROM [dbo].[Trades]
UNION ALL
SELECT 'PerformanceMetrics', COUNT(*), 'Session performance data' FROM [dbo].[PerformanceMetrics]
ORDER BY TableName;

PRINT '';
PRINT '🎉 DATABASE MIGRATION COMPLETED SUCCESSFULLY!';
PRINT '';
PRINT '📋 Summary:';
PRINT '   ✅ All tables recreated (except MarketData - preserved)';
PRINT '   ✅ All constraints defined inline with tables';
PRINT '   ✅ Performance indexes created';
PRINT '   ✅ Reference data inserted (DigitalAssets & TradingBots only)';
PRINT '   ✅ No sample user data inserted';
PRINT '';
PRINT '🚀 Your trading bot application is ready to use!';
PRINT '';
