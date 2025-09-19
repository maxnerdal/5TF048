-- Lab 2 Assignment Question 23
-- Question: Skapa en vy som visar Produktnamn, Kategori och Pris. (G)
-- Created: 2025-09-19

USE Northwind;
GO

-- Create view to show Product name, Category, and Price
CREATE VIEW vw_ProductCategoryPrice
AS
SELECT 
    p.ProductName,
    c.CategoryName AS Kategori,
    p.UnitPrice AS Pris
FROM Products p
INNER JOIN Categories c ON p.CategoryID = c.CategoryID
GO