-- 03_AddForeignKeys.sql
-- Add referential integrity constraints (foreign keys)

USE MyFirstDatabase;
GO

-- Add foreign key from Portfolio to Users (CASCADE delete)
ALTER TABLE [Portfolio]
ADD CONSTRAINT [FK_Portfolio_Users_user_id] 
FOREIGN KEY ([user_id]) 
REFERENCES [Users] ([Id]) -- tabell som refereras till
ON DELETE CASCADE -- tar bort portföljposter när användare tas bort
ON UPDATE CASCADE; -- uppdaterar automatiskt user id i Portfolio om det ändras i Users

-- Add foreign key from Portfolio to DigitalAssets (RESTRICT delete)
ALTER TABLE [Portfolio]
ADD CONSTRAINT [FK_Portfolio_DigitalAssets_asset_id]
FOREIGN KEY ([asset_id])
REFERENCES [DigitalAssets] ([AssetId])  -- tabell som refereras till
ON DELETE NO ACTION -- hindrar borttagning av DigitalAssets om det finns referenser i Portfolio
ON UPDATE CASCADE; -- uppdaterar automatiskt assetId i Portfolio om det ändras i DigitalAssets

PRINT 'Foreign key constraints added successfully!';
