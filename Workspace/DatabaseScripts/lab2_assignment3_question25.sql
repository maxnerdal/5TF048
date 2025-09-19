-- Lab 2 Assignment Question 25
-- Question: Skapa en transaktion som innehåller minst två (2) uppdateringar av databasen 
-- men om den senare av uppdateringarna inte är möjlig så ska ingen av dom utföras.
-- Created: 2025-09-19

USE Northwind;
GO

-- Enkel transaktion: Höj pris på alla produkter och uppdatera en specifik produkt
-- Om den andra uppdateringen misslyckas (produkt existerar inte) så rullas allt tillbaka

-- Scenario 1: Lyckat (båda uppdateringarna fungerar)
PRINT '=== TEST 1: LYCKAT ===';
BEGIN TRANSACTION;
BEGIN TRY
    UPDATE Products SET UnitPrice = UnitPrice * 1.05;  -- Höj 5%
    UPDATE Products SET UnitPrice = 25.00 WHERE ProductID = 1;  -- Sätt pris för produkt 1
    
    -- Kontrollera om produkten faktiskt uppdaterades
    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR('ProductID 1 existerar inte!', 16, 1);
    END
    
    COMMIT TRANSACTION;
    PRINT 'SUCCESS: Båda uppdateringarna lyckades!';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR: Något gick fel!';
    PRINT 'Felmeddelande: ' + ERROR_MESSAGE();
END CATCH;

-- Scenario 2: Misslyckat (andra uppdateringen misslyckas)
PRINT '=== TEST 2: MISSLYCKAT ===';
BEGIN TRANSACTION;
BEGIN TRY
    UPDATE Products SET UnitPrice = UnitPrice * 1.05;  -- Första: OK
    UPDATE Products SET UnitPrice = 50.00 WHERE ProductID = 999;  -- Andra: FEL!
    
    -- Kontrollera om produkten faktiskt uppdaterades
    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR('ProductID 999 existerar inte!', 16, 1);
    END
    
    COMMIT TRANSACTION;
    PRINT 'SUCCESS: Båda uppdateringarna lyckades!';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR: Andra uppdateringen misslyckades - allt rullas tillbaka!';
    PRINT 'Felmeddelande: ' + ERROR_MESSAGE();
END CATCH;