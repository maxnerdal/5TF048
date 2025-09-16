-- Create DigitalAssets table
CREATE TABLE [DigitalAssets] (
    [AssetId] int NOT NULL IDENTITY(1,1),
    [Name] nvarchar(100) NOT NULL,
    [Ticker] nvarchar(10) NOT NULL,
    CONSTRAINT [PK_DigitalAssets] PRIMARY KEY ([AssetId])
);

-- Create unique index on Ticker
CREATE UNIQUE INDEX [IX_DigitalAssets_Ticker] ON [DigitalAssets] ([Ticker]);

-- Create Portfolio table
CREATE TABLE [Portfolio] (
    [Id] int NOT NULL IDENTITY(1,1),
    [user_id] int NOT NULL,
    [asset_id] int NOT NULL,
    [qty] decimal(18,8) NOT NULL,
    [buyprice] decimal(18,8) NOT NULL,
    [datepurchased] datetime2 NOT NULL,
    [datelastupdate] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_Portfolio] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Portfolio_Users_user_id] FOREIGN KEY ([user_id]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Portfolio_DigitalAssets_asset_id] FOREIGN KEY ([asset_id]) REFERENCES [DigitalAssets] ([AssetId]) ON DELETE NO ACTION
);

-- Create indexes for Portfolio table
CREATE INDEX [IX_Portfolio_UserId] ON [Portfolio] ([user_id]);
CREATE INDEX [IX_Portfolio_asset_id] ON [Portfolio] ([asset_id]);

-- Insert some common digital assets
INSERT INTO [DigitalAssets] ([Name], [Ticker]) VALUES 
('Bitcoin', 'BTC'),
('Ethereum', 'ETH'),
('Cardano', 'ADA'),
('Polkadot', 'DOT'),
('Chainlink', 'LINK'),
('Litecoin', 'LTC'),
('Stellar', 'XLM'),
('Dogecoin', 'DOGE'),
('Ripple', 'XRP'),
('Binance Coin', 'BNB');

-- Add migration record
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES ('20250916000002_AddPortfolioAndDigitalAssets', '9.0.9');
