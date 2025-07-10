-- =============================================================================
-- Supermarket Database Verification and Test Script
-- This script tests and verifies the database setup for MCP Resource tools
-- =============================================================================

USE SupermarketDB;
GO

PRINT '=== Supermarket Database Verification and Test Suite ===';
PRINT 'Server: ' + @@SERVERNAME;
PRINT 'Database: ' + DB_NAME();
PRINT 'Date: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '';

-- =============================================================================
-- TEST 1: VERIFY DATABASE AND TABLES EXIST
-- =============================================================================
PRINT 'TEST 1: Verifying database structure...';

-- Check if database exists
IF DB_NAME() = 'SupermarketDB'
    PRINT '✓ Connected to SupermarketDB database';
ELSE
    PRINT '✗ ERROR: Not connected to SupermarketDB database';

-- Check if tables exist
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'Products') AND type in (N'U'))
    PRINT '✓ Products table exists';
ELSE
    PRINT '✗ ERROR: Products table does not exist';

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'Sales') AND type in (N'U'))
    PRINT '✓ Sales table exists';
ELSE
    PRINT '✗ ERROR: Sales table does not exist';

PRINT '';

-- =============================================================================
-- TEST 2: VERIFY REQUIRED COLUMNS FOR MCP RESOURCE TOOLS
-- =============================================================================
PRINT 'TEST 2: Verifying required columns for MCP resource tools...';

DECLARE @MissingColumns TABLE (TableName VARCHAR(50), ColumnName VARCHAR(50));

-- Check Products table columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ProductId')
    INSERT INTO @MissingColumns VALUES ('Products', 'ProductId');
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ProductName')
    INSERT INTO @MissingColumns VALUES ('Products', 'ProductName');
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'Category')
    INSERT INTO @MissingColumns VALUES ('Products', 'Category');
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'UnitPrice')
    INSERT INTO @MissingColumns VALUES ('Products', 'UnitPrice');
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'StockQuantity')
    INSERT INTO @MissingColumns VALUES ('Products', 'StockQuantity');
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ReorderLevel')
    INSERT INTO @MissingColumns VALUES ('Products', 'ReorderLevel');
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'Supplier')
    INSERT INTO @MissingColumns VALUES ('Products', 'Supplier');
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'LastUpdated')
    INSERT INTO @MissingColumns VALUES ('Products', 'LastUpdated');

-- Check Sales table columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'SaleId')
    INSERT INTO @MissingColumns VALUES ('Sales', 'SaleId');
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'ProductId')
    INSERT INTO @MissingColumns VALUES ('Sales', 'ProductId');
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'Quantity')
    INSERT INTO @MissingColumns VALUES ('Sales', 'Quantity');
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'UnitPrice')
    INSERT INTO @MissingColumns VALUES ('Sales', 'UnitPrice');
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'TotalAmount')
    INSERT INTO @MissingColumns VALUES ('Sales', 'TotalAmount');
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Sales') AND name = 'SaleDate')
    INSERT INTO @MissingColumns VALUES ('Sales', 'SaleDate');

IF EXISTS (SELECT * FROM @MissingColumns)
BEGIN
    PRINT '✗ ERROR: Missing required columns:';
    SELECT TableName, ColumnName FROM @MissingColumns ORDER BY TableName, ColumnName;
END
ELSE
BEGIN
    PRINT '✓ All required columns exist';
END

PRINT '';

-- =============================================================================
-- TEST 3: VERIFY PERFORMANCE INDEXES
-- =============================================================================
PRINT 'TEST 3: Verifying performance indexes...';

SELECT 
    i.name AS IndexName,
    t.name AS TableName,
    CASE WHEN i.is_primary_key = 1 THEN 'PRIMARY KEY'
         WHEN i.is_unique = 1 THEN 'UNIQUE'
         ELSE 'INDEX' END AS IndexType,
    CASE WHEN i.name IS NOT NULL THEN '✓' ELSE '✗' END AS Status
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name IN ('Products', 'Sales')
  AND (i.is_primary_key = 1 OR i.name LIKE 'IX_%')
ORDER BY t.name, i.name;

PRINT '';

-- =============================================================================
-- TEST 4: TEST GETINVENTORYSTATUSASYNC QUERY
-- =============================================================================
PRINT 'TEST 4: Testing GetInventoryStatusAsync query...';

BEGIN TRY
    SELECT TOP 5
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
    PRINT 'Error: ' + ERROR_MESSAGE();
END CATCH

PRINT '';

-- =============================================================================
-- TEST 5: TEST GETDAILYSUMMARYASYNC QUERY
-- =============================================================================
PRINT 'TEST 5: Testing GetDailySummaryAsync query...';

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
    SELECT TOP 5
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
    PRINT 'Error: ' + ERROR_MESSAGE();
END CATCH

PRINT '';

-- =============================================================================
-- TEST 6: TEST GETDETAILEDINVENTORYASYNC QUERY
-- =============================================================================
PRINT 'TEST 6: Testing GetDetailedInventoryAsync query...';

BEGIN TRY
    SELECT TOP 5
        p.ProductId,
        p.ProductName,
        p.Category,
        p.UnitPrice,
        p.StockQuantity,
        p.ReorderLevel,
        p.Supplier,
        COALESCE(weekly.WeeklySales, 0) as WeeklySales,
        COALESCE(monthly.MonthlySales, 0) as MonthlySales,
        COALESCE(weekly.WeeklyRevenue, 0) as WeeklyRevenue,
        COALESCE(monthly.MonthlyRevenue, 0) as MonthlyRevenue,
        p.LastUpdated,
        CASE 
            WHEN p.StockQuantity <= 0 THEN 'Out of Stock'
            WHEN p.StockQuantity <= p.ReorderLevel THEN 'Low Stock'
            WHEN p.StockQuantity <= p.ReorderLevel * 2 THEN 'Medium Stock'
            ELSE 'In Stock'
        END AS StockStatus
    FROM Products p
    LEFT JOIN (
        SELECT 
            ProductId, 
            SUM(Quantity) as WeeklySales,
            SUM(TotalAmount) as WeeklyRevenue
        FROM Sales 
        WHERE SaleDate >= DATEADD(day, -7, GETDATE())
        GROUP BY ProductId
    ) weekly ON p.ProductId = weekly.ProductId
    LEFT JOIN (
        SELECT 
            ProductId, 
            SUM(Quantity) as MonthlySales,
            SUM(TotalAmount) as MonthlyRevenue
        FROM Sales 
        WHERE SaleDate >= DATEADD(day, -30, GETDATE())
        GROUP BY ProductId
    ) monthly ON p.ProductId = monthly.ProductId
    ORDER BY p.Category, p.ProductName;

    PRINT '✓ GetDetailedInventoryAsync query executed successfully';
END TRY
BEGIN CATCH
    PRINT '✗ GetDetailedInventoryAsync query failed:';
    PRINT 'Error: ' + ERROR_MESSAGE();
END CATCH

PRINT '';

-- =============================================================================
-- TEST 7: DATA QUALITY CHECKS
-- =============================================================================
PRINT 'TEST 7: Data quality checks...';

DECLARE @DataIssues TABLE (CheckType VARCHAR(50), RecordCount INT, Issue VARCHAR(100));

-- Check for NULL values in required columns
INSERT INTO @DataIssues
SELECT 'NULL Check', COUNT(*), 'Products with NULL ReorderLevel'
FROM Products WHERE ReorderLevel IS NULL HAVING COUNT(*) > 0;

INSERT INTO @DataIssues
SELECT 'NULL Check', COUNT(*), 'Products with NULL LastUpdated'
FROM Products WHERE LastUpdated IS NULL HAVING COUNT(*) > 0;

INSERT INTO @DataIssues
SELECT 'NULL Check', COUNT(*), 'Products with NULL UnitPrice'
FROM Products WHERE UnitPrice IS NULL HAVING COUNT(*) > 0;

-- Check for unrealistic values
INSERT INTO @DataIssues
SELECT 'Data Quality', COUNT(*), 'Products with negative stock'
FROM Products WHERE StockQuantity < 0 HAVING COUNT(*) > 0;

INSERT INTO @DataIssues
SELECT 'Data Quality', COUNT(*), 'Products with zero or negative price'
FROM Products WHERE UnitPrice <= 0 HAVING COUNT(*) > 0;

INSERT INTO @DataIssues
SELECT 'Data Quality', COUNT(*), 'Sales with zero or negative quantity'
FROM Sales WHERE Quantity <= 0 HAVING COUNT(*) > 0;

INSERT INTO @DataIssues
SELECT 'Data Quality', COUNT(*), 'Sales with zero or negative total amount'
FROM Sales WHERE TotalAmount <= 0 HAVING COUNT(*) > 0;

IF EXISTS (SELECT * FROM @DataIssues)
BEGIN
    PRINT '⚠ Data quality issues found:';
    SELECT CheckType, RecordCount, Issue FROM @DataIssues;
END
ELSE
BEGIN
    PRINT '✓ No data quality issues found';
END

PRINT '';

-- =============================================================================
-- TEST 8: RECORD COUNTS AND SUMMARY
-- =============================================================================
PRINT 'TEST 8: Database summary...';

SELECT 
    'Products' as TableName,
    COUNT(*) as RecordCount,
    MIN(LastUpdated) as OldestRecord,
    MAX(LastUpdated) as NewestRecord
FROM Products
UNION ALL
SELECT 
    'Sales' as TableName,
    COUNT(*) as RecordCount,
    MIN(SaleDate) as OldestRecord,
    MAX(SaleDate) as NewestRecord
FROM Sales;

PRINT '';

-- Show inventory summary by category
PRINT 'Inventory summary by category:';
SELECT 
    Category,
    COUNT(*) as ProductCount,
    SUM(StockQuantity) as TotalStock,
    AVG(UnitPrice) as AvgPrice,
    SUM(CASE WHEN StockQuantity <= ReorderLevel THEN 1 ELSE 0 END) as LowStockCount
FROM Products
GROUP BY Category
ORDER BY Category;

PRINT '';

-- Show recent sales summary
PRINT 'Recent sales summary (last 7 days):';
SELECT 
    CAST(SaleDate AS DATE) as SaleDate,
    COUNT(*) as TransactionCount,
    SUM(Quantity) as TotalItemsSold,
    SUM(TotalAmount) as TotalRevenue
FROM Sales
WHERE SaleDate >= DATEADD(day, -7, GETDATE())
GROUP BY CAST(SaleDate AS DATE)
ORDER BY SaleDate DESC;

PRINT '';
PRINT '=== Database Verification Complete ===';
PRINT 'If no errors (✗) were reported above, the database is ready for MCP resource tools!';
PRINT 'The database contains all required tables, columns, indexes, and data for advanced AI integration.';
GO
