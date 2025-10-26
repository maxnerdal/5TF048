-- =============================================
-- MarketData Database Migration Script
-- This script creates the MarketData table and related indexes
-- Preserves existing data if table already exists
-- =============================================

USE MyFirstDatabase;
GO

-- =============================================
-- 1. MarketData Table (Create only if doesn't exist - preserve data!)
-- =============================================

PRINT '🏗️  Checking MarketData table...';

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MarketData]') AND type in (N'U'))
BEGIN
    PRINT '📋 Creating MarketData table...';
    
    CREATE TABLE [dbo].[MarketData] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [Symbol] NVARCHAR(10) NOT NULL,
        [TimeFrame] NVARCHAR(10) NOT NULL,
        [OpenTime] DATETIME2(7) NOT NULL,
        [OpenPrice] DECIMAL(18,8) NOT NULL,
        [HighPrice] DECIMAL(18,8) NOT NULL,
        [LowPrice] DECIMAL(18,8) NOT NULL,
        [ClosePrice] DECIMAL(18,8) NOT NULL,
        [Volume] DECIMAL(18,8) NOT NULL,
        [CloseTime] DATETIME2(7) NOT NULL,
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETDATE(),
        
        -- Primary Key
        CONSTRAINT [PK_MarketData] PRIMARY KEY CLUSTERED ([Id])
    );
    
    PRINT '✅ MarketData table created successfully';
END
ELSE
BEGIN
    PRINT 'ℹ️  MarketData table already exists - preserving existing data';
    
    -- Get record count for existing table
    DECLARE @RecordCount INT;
    SELECT @RecordCount = COUNT(*) FROM [dbo].[MarketData];
    PRINT '📊 Existing records: ' + CAST(@RecordCount AS NVARCHAR(50));
END

PRINT '';

-- =============================================
-- 2. MarketData Performance Indexes
-- =============================================

PRINT '⚡ Creating MarketData performance indexes...';

-- Index for Symbol + OpenTime queries (most common for trading bots)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MarketData_Symbol_OpenTime' AND object_id = OBJECT_ID('[dbo].[MarketData]'))
BEGIN
    PRINT '🔍 Creating Symbol + OpenTime index...';
    CREATE NONCLUSTERED INDEX [IX_MarketData_Symbol_OpenTime] 
        ON [dbo].[MarketData] ([Symbol], [OpenTime]);
    PRINT '✅ IX_MarketData_Symbol_OpenTime created';
END
ELSE
BEGIN
    PRINT 'ℹ️  IX_MarketData_Symbol_OpenTime already exists';
END

-- Index for Symbol + TimeFrame queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MarketData_Symbol_TimeFrame' AND object_id = OBJECT_ID('[dbo].[MarketData]'))
BEGIN
    PRINT '🔍 Creating Symbol + TimeFrame index...';
    CREATE NONCLUSTERED INDEX [IX_MarketData_Symbol_TimeFrame] 
        ON [dbo].[MarketData] ([Symbol], [TimeFrame]);
    PRINT '✅ IX_MarketData_Symbol_TimeFrame created';
END
ELSE
BEGIN
    PRINT 'ℹ️  IX_MarketData_Symbol_TimeFrame already exists';
END

-- Index for OpenTime range queries (backtesting date ranges)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MarketData_OpenTime' AND object_id = OBJECT_ID('[dbo].[MarketData]'))
BEGIN
    PRINT '🔍 Creating OpenTime index...';
    CREATE NONCLUSTERED INDEX [IX_MarketData_OpenTime] 
        ON [dbo].[MarketData] ([OpenTime]);
    PRINT '✅ IX_MarketData_OpenTime created';
END
ELSE
BEGIN
    PRINT 'ℹ️  IX_MarketData_OpenTime already exists';
END

-- Composite index for complex trading bot queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MarketData_Symbol_TimeFrame_OpenTime' AND object_id = OBJECT_ID('[dbo].[MarketData]'))
BEGIN
    PRINT '🔍 Creating comprehensive Symbol + TimeFrame + OpenTime index...';
    CREATE NONCLUSTERED INDEX [IX_MarketData_Symbol_TimeFrame_OpenTime] 
        ON [dbo].[MarketData] ([Symbol], [TimeFrame], [OpenTime]);
    PRINT '✅ IX_MarketData_Symbol_TimeFrame_OpenTime created';
END
ELSE
BEGIN
    PRINT 'ℹ️  IX_MarketData_Symbol_TimeFrame_OpenTime already exists';
END

PRINT '';

-- =============================================
-- 3. Verification and Statistics
-- =============================================

PRINT '🔍 MarketData verification...';

-- Get table statistics
SELECT 
    'MarketData' as TableName,
    COUNT(*) as TotalRecords,
    MIN(OpenTime) as EarliestDate,
    MAX(OpenTime) as LatestDate,
    COUNT(DISTINCT Symbol) as UniqueSymbols,
    COUNT(DISTINCT TimeFrame) as UniqueTimeFrames
FROM [dbo].[MarketData];

-- Show data distribution by symbol
PRINT '';
PRINT '📈 Data distribution by Symbol:';
SELECT 
    Symbol,
    COUNT(*) as RecordCount,
    MIN(OpenTime) as FirstRecord,
    MAX(OpenTime) as LastRecord
FROM [dbo].[MarketData]
GROUP BY Symbol
ORDER BY RecordCount DESC;

-- Show index information
PRINT '';
PRINT '🗂️  MarketData indexes:';
SELECT 
    i.name as IndexName,
    i.type_desc as IndexType,
    STUFF((
        SELECT ', ' + c.name
        FROM sys.index_columns ic
        INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
        ORDER BY ic.key_ordinal
        FOR XML PATH('')
    ), 1, 2, '') as IndexColumns
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID('[dbo].[MarketData]')
    AND i.name IS NOT NULL
ORDER BY i.name;

PRINT '';
PRINT '🎉 MarketData migration completed successfully!';
PRINT '';
PRINT '💡 Performance Tips:';
PRINT '   - Use Symbol + OpenTime for date range queries';
PRINT '   - Use Symbol + TimeFrame for timeframe-specific queries';
PRINT '   - Indexes will significantly speed up trading bot backtesting';
PRINT '';