-- Test Script for Enhanced MCP Resource Tools Database Schema
-- Run this after UpdateSchemaForResources.sql to verify everything works

USE SupermarketDB;
GO

PRINT '=== Testing Enhanced MCP Resource Tools Database Schema ===';
PRINT '';

-- Test 1: Verify all required columns exist
PRINT '1. Verifying required columns exist...';
DECLARE @MissingColumns TABLE (ColumnName VARCHAR(50));

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ReorderLevel')
    INSERT INTO @MissingColumns VALUES ('ReorderLevel');

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'LastUpdated')
    INSERT INTO @MissingColumns VALUES ('LastUpdated');

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'UnitPrice')
    INSERT INTO @MissingColumns VALUES ('UnitPrice');

IF EXISTS (SELECT * FROM @MissingColumns)
BEGIN
    PRINT 'ERROR: Missing required columns:';
    SELECT ColumnName FROM @MissingColumns;
END
ELSE
BEGIN
    PRINT '✓ All required columns exist';
END

PRINT '';

-- Test 2: Test the exact query used in GetInventoryStatusAsync
PRINT '2. Testing GetInventoryStatusAsync query...';
BEGIN TRY
    SELECT 
        p.ProductId,
        p.ProductName,
        p.Category,
        p.StockQuantity,
        p.ReorderLevel,
        p.UnitPrice,
        CASE 
            WHEN p.StockQuantity <= 0 THEN 'Out of Stock'
            WHEN p.StockQuantity <= p.ReorderLevel THEN 'Low Stock'
            WHEN p.StockQuantity <= p.ReorderLevel * 2 THEN 'Medium Stock'
            ELSE 'In Stock'
        END AS StockStatus,
        COALESCE(recent.RecentSales, 0) as RecentSales,
        p.LastUpdated
    FROM Products p
    LEFT JOIN (
        SELECT ProductId, SUM(Quantity) as RecentSales
        FROM Sales 
        WHERE SaleDate >= DATEADD(day, -7, GETDATE())
        GROUP BY ProductId
    ) recent ON p.ProductId = recent.ProductId
    ORDER BY p.Category, p.ProductName;

    PRINT '✓ GetInventoryStatusAsync query executed successfully';
END TRY
BEGIN CATCH
    PRINT '✗ GetInventoryStatusAsync query failed:';
    PRINT ERROR_MESSAGE();
END CATCH

PRINT '';

-- Test 3: Test the exact query used in GetDailySummaryAsync
PRINT '3. Testing GetDailySummaryAsync query...';
BEGIN TRY
    WITH DailySales AS (
        SELECT 
            CAST(s.SaleDate AS DATE) as SaleDate,
            COUNT(DISTINCT s.SaleId) as TotalTransactions,
            SUM(s.TotalAmount) as TotalRevenue,
            COUNT(DISTINCT s.ProductId) as UniqueProducts,
            SUM(s.Quantity) as TotalItemsSold,
            p.Category
        FROM Sales s
        JOIN Products p ON s.ProductId = p.ProductId
        WHERE CAST(s.SaleDate AS DATE) >= DATEADD(day, -7, GETDATE())
        GROUP BY CAST(s.SaleDate AS DATE), p.Category
    ),
    TopCategory AS (
        SELECT 
            SaleDate,
            Category as TopCategory,
            SUM(TotalRevenue) as TopCategoryRevenue,
            ROW_NUMBER() OVER (PARTITION BY SaleDate ORDER BY SUM(TotalRevenue) DESC) as rn
        FROM DailySales
        GROUP BY SaleDate, Category
    )
    SELECT 
        ds.SaleDate as Date,
        SUM(ds.TotalTransactions) as TotalTransactions,
        SUM(ds.TotalRevenue) as TotalRevenue,
        SUM(ds.UniqueProducts) as UniqueProducts,
        SUM(ds.TotalItemsSold) as TotalItemsSold,
        CASE WHEN SUM(ds.TotalTransactions) > 0 
             THEN SUM(ds.TotalRevenue) / SUM(ds.TotalTransactions) 
             ELSE 0 END as AverageTransactionValue,
        COALESCE(tc.TopCategory, 'N/A') as TopCategory,
        COALESCE(tc.TopCategoryRevenue, 0) as TopCategoryRevenue
    FROM DailySales ds
    LEFT JOIN TopCategory tc ON ds.SaleDate = tc.SaleDate AND tc.rn = 1
    GROUP BY ds.SaleDate, tc.TopCategory, tc.TopCategoryRevenue
    ORDER BY ds.SaleDate DESC;

    PRINT '✓ GetDailySummaryAsync query executed successfully';
END TRY
BEGIN CATCH
    PRINT '✗ GetDailySummaryAsync query failed:';
    PRINT ERROR_MESSAGE();
END CATCH

PRINT '';

-- Test 4: Verify indexes were created
PRINT '4. Verifying performance indexes...';
SELECT 
    i.name AS IndexName,
    t.name AS TableName,
    CASE WHEN i.is_primary_key = 1 THEN 'PRIMARY KEY'
         WHEN i.is_unique = 1 THEN 'UNIQUE'
         ELSE 'INDEX' END AS IndexType
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name IN ('Products', 'Sales')
  AND i.name IS NOT NULL
  AND i.name LIKE 'IX_%'
ORDER BY t.name, i.name;

PRINT '';

-- Test 5: Show sample inventory status data
PRINT '5. Sample inventory status data (first 10 rows):';
SELECT TOP 10
    ProductName,
    Category,
    StockQuantity,
    ReorderLevel,
    CASE 
        WHEN StockQuantity <= 0 THEN 'Out of Stock'
        WHEN StockQuantity <= ReorderLevel THEN 'Low Stock'
        WHEN StockQuantity <= ReorderLevel * 2 THEN 'Medium Stock'
        ELSE 'In Stock'
    END AS StockStatus,
    UnitPrice,
    LastUpdated
FROM Products
ORDER BY 
    CASE 
        WHEN StockQuantity <= 0 THEN 1
        WHEN StockQuantity <= ReorderLevel THEN 2
        WHEN StockQuantity <= ReorderLevel * 2 THEN 3
        ELSE 4
    END,
    Category;

PRINT '';

-- Test 6: Check for any data quality issues
PRINT '6. Data quality checks...';

-- Check for NULL values in required columns
SELECT 
    'NULL Check' AS CheckType,
    COUNT(*) AS RecordCount,
    'ReorderLevel has NULL values' AS Issue
FROM Products 
WHERE ReorderLevel IS NULL
HAVING COUNT(*) > 0

UNION ALL

SELECT 
    'NULL Check' AS CheckType,
    COUNT(*) AS RecordCount,
    'LastUpdated has NULL values' AS Issue
FROM Products 
WHERE LastUpdated IS NULL
HAVING COUNT(*) > 0

UNION ALL

SELECT 
    'NULL Check' AS CheckType,
    COUNT(*) AS RecordCount,
    'UnitPrice has NULL values' AS Issue
FROM Products 
WHERE UnitPrice IS NULL
HAVING COUNT(*) > 0;

-- Check for unrealistic values
SELECT 
    'Data Quality' AS CheckType,
    COUNT(*) AS RecordCount,
    'Products with negative stock' AS Issue
FROM Products 
WHERE StockQuantity < 0
HAVING COUNT(*) > 0

UNION ALL

SELECT 
    'Data Quality' AS CheckType,
    COUNT(*) AS RecordCount,
    'Products with zero or negative price' AS Issue
FROM Products 
WHERE UnitPrice <= 0
HAVING COUNT(*) > 0;

PRINT '';
PRINT '=== Database Schema Test Completed ===';
PRINT 'If no errors were reported above, the database is ready for the enhanced MCP resource tools!';
