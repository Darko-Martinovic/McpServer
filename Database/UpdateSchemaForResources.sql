-- Database Schema Updates for Enhanced MCP Resource Tools
-- This script adds the missing columns needed for the new inventory status functionality

USE SupermarketDB;
GO

-- Step 1: Add missing columns to Products table
PRINT 'Adding missing columns to Products table...';

-- Add ReorderLevel column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ReorderLevel')
BEGIN
    ALTER TABLE Products ADD ReorderLevel INT NOT NULL DEFAULT 10;
    PRINT 'Added ReorderLevel column to Products table';
END
ELSE
BEGIN
    PRINT 'ReorderLevel column already exists in Products table';
END

-- Add LastUpdated column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'LastUpdated')
BEGIN
    ALTER TABLE Products ADD LastUpdated DATETIME2 NOT NULL DEFAULT GETDATE();
    PRINT 'Added LastUpdated column to Products table';
END
ELSE
BEGIN
    PRINT 'LastUpdated column already exists in Products table';
END

-- Add UnitPrice column if it doesn't exist (the code expects UnitPrice, not Price)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'UnitPrice')
BEGIN
    -- If Price column exists, rename it to UnitPrice
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'Price')
    BEGIN
        EXEC sp_rename 'Products.Price', 'UnitPrice', 'COLUMN';
        PRINT 'Renamed Price column to UnitPrice in Products table';
    END
    ELSE
    BEGIN
        ALTER TABLE Products ADD UnitPrice DECIMAL(10,2) NOT NULL DEFAULT 0.00;
        PRINT 'Added UnitPrice column to Products table';
    END
END
ELSE
BEGIN
    PRINT 'UnitPrice column already exists in Products table';
END

-- Step 2: Update existing data with realistic reorder levels
PRINT 'Updating reorder levels based on product categories...';

UPDATE Products 
SET ReorderLevel = CASE 
    WHEN Category = 'Dairy' THEN 15
    WHEN Category = 'Beverages' THEN 25
    WHEN Category = 'Snacks' THEN 20
    WHEN Category = 'Produce' THEN 10
    WHEN Category = 'Meat' THEN 8
    WHEN Category = 'Bakery' THEN 12
    WHEN Category = 'Frozen' THEN 18
    ELSE 10
END
WHERE ReorderLevel = 10; -- Only update default values

-- Step 3: Update LastUpdated timestamps
PRINT 'Setting LastUpdated timestamps...';

UPDATE Products 
SET LastUpdated = DATEADD(day, -ABS(CHECKSUM(NEWID()) % 30), GETDATE())
WHERE LastUpdated = (SELECT MIN(LastUpdated) FROM Products);

-- Step 4: Create indexes for better performance
PRINT 'Creating performance indexes...';

-- Index for inventory status queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_StockQuantity_ReorderLevel')
BEGIN
    CREATE INDEX IX_Products_StockQuantity_ReorderLevel 
    ON Products (StockQuantity, ReorderLevel) 
    INCLUDE (ProductName, Category, UnitPrice, LastUpdated);
    PRINT 'Created index IX_Products_StockQuantity_ReorderLevel';
END

-- Index for sales data queries by date
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_SaleDate_ProductId')
BEGIN
    CREATE INDEX IX_Sales_SaleDate_ProductId 
    ON Sales (SaleDate, ProductId) 
    INCLUDE (Quantity, TotalAmount);
    PRINT 'Created index IX_Sales_SaleDate_ProductId';
END

-- Index for category-based queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_Category')
BEGIN
    CREATE INDEX IX_Products_Category 
    ON Products (Category) 
    INCLUDE (ProductName, StockQuantity, ReorderLevel, UnitPrice);
    PRINT 'Created index IX_Products_Category';
END

-- Step 5: Verify the schema
PRINT 'Verifying updated schema...';

SELECT 
    'Products Table Columns' AS Info,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Products'
ORDER BY ORDINAL_POSITION;

-- Step 6: Sample data verification query
PRINT 'Sample data verification...';

SELECT TOP 5
    ProductId,
    ProductName,
    Category,
    StockQuantity,
    ReorderLevel,
    UnitPrice,
    CASE 
        WHEN StockQuantity <= 0 THEN 'Out of Stock'
        WHEN StockQuantity <= ReorderLevel THEN 'Low Stock'
        WHEN StockQuantity <= ReorderLevel * 2 THEN 'Medium Stock'
        ELSE 'In Stock'
    END AS StockStatus,
    LastUpdated
FROM Products
ORDER BY Category, ProductName;

PRINT 'Database schema update completed successfully!';
PRINT 'The database now supports all enhanced MCP resource tools.';
