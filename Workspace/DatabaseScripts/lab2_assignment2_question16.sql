-- Lab 2 Assignment 2 Question 16
-- Question: Hur stor var den totala ordersumman 96-12-12 (Orders, Order Details, OrderID, UnitPrice, Quantity, OrderDate, Discount)?
-- Purpose: Calculate total order amount for date 1996-12-12, considering discount
-- Created: 2025-09-19

USE Northwind;
GO

-- Query to calculate total order sum for 1996-12-12
-- Joins Orders with Order Details to get complete order information
-- Formula: (UnitPrice * Quantity) * (1 - Discount) = Net amount per order line
SELECT 
    o.OrderDate,
    SUM(od.UnitPrice * od.Quantity * (1 - od.Discount)) AS TotalOrderSum
FROM Orders o
INNER JOIN [Order Details] od ON o.OrderID = od.OrderID
WHERE o.OrderDate = '1996-12-12'
GROUP BY o.OrderDate;