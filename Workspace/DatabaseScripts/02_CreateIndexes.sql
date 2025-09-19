-- 02_CreateIndexes.sql
-- Create indexes for performance and uniqueness constraints

USE MyFirstDatabase;
GO

-- Create unique indexes for Users table
CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);

-- Create unique index for DigitalAssets
CREATE UNIQUE INDEX [IX_DigitalAssets_Ticker] ON [DigitalAssets] ([Ticker]);

-- Create performance indexes for Portfolio
CREATE INDEX [IX_Portfolio_UserId] ON [Portfolio] ([user_id]);
CREATE INDEX [IX_Portfolio_AssetId] ON [Portfolio] ([asset_id]);

PRINT 'Indexes created successfully!';
