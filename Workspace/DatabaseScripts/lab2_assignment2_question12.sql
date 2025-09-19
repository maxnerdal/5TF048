-- Lab 2 Assignment 2 Question 12
-- Question: Till vilken kategori (CategoryName, CategoryID, Categories) hör produkten (Products, ProductName) Lakkalikööri?
-- Purpose: Find the category information for the product "Lakkalikööri"
-- Created: 2025-09-19

USE Northwind;
GO

-- Query to find category information for product "Lakkalikööri"
-- Uses JOIN to connect Products table with Categories table
SELECT 
    p.ProductName,
    c.CategoryID,
    c.CategoryName
FROM Products p
INNER JOIN Categories c ON p.CategoryID = c.CategoryID
WHERE p.ProductName = 'Lakkalikööri';