-- Lab 2 Assignment 2 Question 19
-- Question: Priserna (UnitPrice) för alla produkter (Products) i kategori 4 (CategoryID) skall höjas med 20%, visa priserna före och efter höjning.
-- Created: 2025-09-19

USE Northwind;
GO

SELECT 
    ProductID,
    ProductName,
    CategoryID,
    UnitPrice AS PriceBefore,
    UnitPrice * 1.20 AS PriceAfter20PercentIncrease
FROM Products
WHERE CategoryID = 4
ORDER BY ProductID;