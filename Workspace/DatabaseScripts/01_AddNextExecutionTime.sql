-- Add NextExecutionTime column to UserBots table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'UserBots' AND COLUMN_NAME = 'NextExecutionTime')
BEGIN
    ALTER TABLE UserBots
    ADD NextExecutionTime DATETIME2 NULL;
    
    PRINT 'Added NextExecutionTime column to UserBots table';
END
ELSE
BEGIN
    PRINT 'NextExecutionTime column already exists in UserBots table';
END
GO
