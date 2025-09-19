--MyPassword123#


-- Lab 2 Assignment 3 Question 21
EXEC sp_IncreaseAllProductPrices @PercentageIncrease = 10.0;

-- Lab 2 Assignment 3 Question 23
SELECT * FROM vw_ProductCategoryPrice
ORDER BY Kategori, ProductName;
