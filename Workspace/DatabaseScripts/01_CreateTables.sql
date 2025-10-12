-- 01_CreateTables.sql
-- Complete database setup script for Crypto Portfolio Application
-- Execute this script in Visual Studio Code with SQL Server extension

USE MyFirstDatabase;
GO

-- Drop existing tables if they exist (for clean setup)
IF OBJECT_ID('Portfolio', 'U') IS NOT NULL DROP TABLE Portfolio;
IF OBJECT_ID('MarketData', 'U') IS NOT NULL DROP TABLE MarketData;
IF OBJECT_ID('DigitalAssets', 'U') IS NOT NULL DROP TABLE DigitalAssets;
IF OBJECT_ID('Users', 'U') IS NOT NULL DROP TABLE Users;
GO

-- Create Users table
CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY(1,1),
    [Email] nvarchar(256) NOT NULL,
    [Username] nvarchar(50) NOT NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO

-- Create DigitalAssets table
CREATE TABLE [DigitalAssets] (
    [AssetId] int NOT NULL IDENTITY(1,1),
    [Name] nvarchar(100) NOT NULL,
    [Ticker] nvarchar(10) NOT NULL,
    CONSTRAINT [PK_DigitalAssets] PRIMARY KEY ([AssetId])
);
GO

-- Create Portfolio table
CREATE TABLE [Portfolio] (
    [Id] int NOT NULL IDENTITY(1,1),
    [user_id] int NOT NULL,
    [asset_id] int NOT NULL,
    [qty] decimal(18,8) NOT NULL,
    [buyprice] decimal(18,8) NOT NULL,
    [datepurchased] datetime2 NOT NULL,
    [datelastupdate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_Portfolio] PRIMARY KEY ([Id])
);
GO

-- Create MarketData table for storing historical candlestick data
CREATE TABLE [MarketData] (
    [Id] bigint NOT NULL IDENTITY(1,1),
    [Symbol] nvarchar(20) NOT NULL,
    [TimeFrame] nvarchar(10) NOT NULL, -- '1m', '5m', '15m', '30m', '1h', '4h', '1d', '1w'
    [OpenTime] datetime2 NOT NULL,
    [OpenPrice] decimal(18,8) NOT NULL,
    [HighPrice] decimal(18,8) NOT NULL,
    [LowPrice] decimal(18,8) NOT NULL,
    [ClosePrice] decimal(18,8) NOT NULL,
    [Volume] decimal(18,8) NOT NULL,
    [CloseTime] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_MarketData] PRIMARY KEY ([Id])
);
GO

PRINT 'Tables created successfully!';
