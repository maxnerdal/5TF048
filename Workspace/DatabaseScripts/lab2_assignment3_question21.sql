-- Lab 2 Assignment 3 Question 21
-- Question: Priserna för alla produkter ska höjas med 5%. (G)
-- Created: 2025-09-19

USE Northwind;
GO

-- Create stored procedure to increase all product prices by 5%
CREATE PROCEDURE sp_IncreaseAllProductPrices
    @PercentageIncrease DECIMAL(5,2) = 5.0  -- Default 5% increase
AS
BEGIN
    -- Set transaction isolation level for data consistency
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
    
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Show current statistics before update
        PRINT 'Before price increase:';
        SELECT top 5
           Products.ProductName,
           Products.UnitPrice
        FROM Products
        WHERE UnitPrice IS NOT NULL;
        
        -- Update all product prices by the specified percentage
        UPDATE Products 
        SET UnitPrice = UnitPrice * (1 + @PercentageIncrease / 100)
        WHERE UnitPrice IS NOT NULL;

        PRINT 'After price increase:';
        SELECT top 5
           Products.ProductName,
           Products.UnitPrice
        FROM Products
        WHERE UnitPrice IS NOT NULL;

        PRINT CONCAT('Successfully increased all product prices by ', @PercentageIncrease, '%');
        
        -- Commit the transaction
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        -- Rollback transaction if error occurs
        ROLLBACK TRANSACTION;
        
        -- Return error information
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO