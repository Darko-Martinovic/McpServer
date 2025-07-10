-- =============================================================================
-- Complete Supermarket Database Setup Script
-- This script consolidates all database creation, schema updates, and sample data
-- =============================================================================

-- Check if we're connected to the right server
PRINT '=== Supermarket Database Complete Setup ===';
PRINT 'Server: ' + @@SERVERNAME;
PRINT 'Date: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '';

-- =============================================================================
-- STEP 1: CREATE DATABASE
-- =============================================================================
PRINT 'STEP 1: Creating SupermarketDB database...';

-- Check if database exists and create if not
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SupermarketDB')
BEGIN
    CREATE DATABASE SupermarketDB;
    PRINT '✓ Database SupermarketDB created successfully';
END
ELSE
BEGIN
    PRINT '✓ Database SupermarketDB already exists';
END

-- Switch to the database
USE SupermarketDB;
GO

-- =============================================================================
-- STEP 2: CREATE TABLES WITH ENHANCED SCHEMA
-- =============================================================================
PRINT 'STEP 2: Creating tables with enhanced schema...';

-- Create Products table with all required columns for MCP Resource tools
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'Products') AND type in (N'U'))
BEGIN
    CREATE TABLE Products (
        ProductId INT IDENTITY(1,1) PRIMARY KEY,
        ProductName NVARCHAR(100) NOT NULL,
        Category NVARCHAR(50) NOT NULL,
        UnitPrice DECIMAL(10,2) NOT NULL DEFAULT 0.00,
        StockQuantity INT NOT NULL DEFAULT 0,
        ReorderLevel INT NOT NULL DEFAULT 10,
        Supplier NVARCHAR(100) NOT NULL,
        LastUpdated DATETIME2 NOT NULL DEFAULT GETDATE()
    );
    PRINT '✓ Products table created with enhanced schema';
END
ELSE
BEGIN
    PRINT '✓ Products table already exists';
    
    -- Add missing columns if table exists but lacks new columns
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ReorderLevel')
    BEGIN
        ALTER TABLE Products ADD ReorderLevel INT NOT NULL DEFAULT 10;
        PRINT '✓ Added ReorderLevel column to Products table';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'LastUpdated')
    BEGIN
        ALTER TABLE Products ADD LastUpdated DATETIME2 NOT NULL DEFAULT GETDATE();
        PRINT '✓ Added LastUpdated column to Products table';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'UnitPrice')
    BEGIN
        -- If Price column exists, rename it to UnitPrice
        IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'Price')
        BEGIN
            EXEC sp_rename 'Products.Price', 'UnitPrice', 'COLUMN';
            PRINT '✓ Renamed Price column to UnitPrice in Products table';
        END
        ELSE
        BEGIN
            ALTER TABLE Products ADD UnitPrice DECIMAL(10,2) NOT NULL DEFAULT 0.00;
            PRINT '✓ Added UnitPrice column to Products table';
        END
    END
    
    -- Fix existing products with zero UnitPrice by setting realistic values
    IF EXISTS (SELECT * FROM Products WHERE UnitPrice = 0.00)
    BEGIN
        PRINT 'Updating zero UnitPrice values with realistic prices...';
        UPDATE Products SET UnitPrice = 
            CASE ProductName 
                WHEN 'Milk 2L' THEN 3.99
                WHEN 'Bread Whole Wheat' THEN 2.49
                WHEN 'Bananas 1kg' THEN 1.99
                WHEN 'Chicken Breast 500g' THEN 8.99
                WHEN 'Rice Basmati 1kg' THEN 4.99
                WHEN 'Tomatoes 500g' THEN 2.99
                WHEN 'Eggs 12 pack' THEN 3.49
                WHEN 'Potatoes 2kg' THEN 3.99
                WHEN 'Orange Juice 1L' THEN 2.99
                WHEN 'Pasta Spaghetti 500g' THEN 1.99
                WHEN 'Beef Ground 400g' THEN 6.99
                WHEN 'Apples Red 1kg' THEN 3.49
                WHEN 'Cheese Cheddar 200g' THEN 4.99
                WHEN 'Onions 1kg' THEN 1.49
                WHEN 'Coffee Beans 250g' THEN 12.99
                WHEN 'Salmon Fillet 300g' THEN 15.99
                WHEN 'Yogurt Greek 500g' THEN 4.49
                WHEN 'Carrots 1kg' THEN 1.99
                WHEN 'Grapes 500g' THEN 4.99
                WHEN 'Pork Chops 400g' THEN 7.99
                ELSE 5.99 -- Default price for any other products
            END
        WHERE UnitPrice = 0.00;
        PRINT '✓ Updated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' products with realistic prices';
    END
END

-- Create Sales table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'Sales') AND type in (N'U'))
BEGIN
    CREATE TABLE Sales (
        SaleId INT IDENTITY(1,1) PRIMARY KEY,
        ProductId INT NOT NULL,
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(10,2) NOT NULL,
        TotalAmount DECIMAL(10,2) NOT NULL,
        SaleDate DATETIME2 NOT NULL DEFAULT GETDATE(),
        FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
    );
    PRINT '✓ Sales table created';
END
ELSE
BEGIN
    PRINT '✓ Sales table already exists';
END

-- =============================================================================
-- STEP 3: CREATE PERFORMANCE INDEXES
-- =============================================================================
PRINT 'STEP 3: Creating performance indexes...';

-- Index for inventory status queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_StockQuantity_ReorderLevel')
BEGIN
    CREATE INDEX IX_Products_StockQuantity_ReorderLevel 
    ON Products (StockQuantity, ReorderLevel) 
    INCLUDE (ProductName, Category, UnitPrice, LastUpdated);
    PRINT '✓ Created index IX_Products_StockQuantity_ReorderLevel';
END

-- Index for sales data queries by date
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_SaleDate_ProductId')
BEGIN
    CREATE INDEX IX_Sales_SaleDate_ProductId 
    ON Sales (SaleDate, ProductId) 
    INCLUDE (Quantity, TotalAmount);
    PRINT '✓ Created index IX_Sales_SaleDate_ProductId';
END

-- Index for category-based queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_Category')
BEGIN
    CREATE INDEX IX_Products_Category 
    ON Products (Category) 
    INCLUDE (ProductName, StockQuantity, ReorderLevel, UnitPrice);
    PRINT '✓ Created index IX_Products_Category';
END

-- =============================================================================
-- STEP 4: INSERT SAMPLE DATA
-- =============================================================================
PRINT 'STEP 4: Inserting sample data...';

-- Check if we already have data
IF (SELECT COUNT(*) FROM Products) = 0
BEGIN
    PRINT 'Inserting sample products...';
    
    -- Insert sample products with realistic data for all categories
    INSERT INTO Products (ProductName, Category, UnitPrice, StockQuantity, ReorderLevel, Supplier, LastUpdated) VALUES
    -- Dairy Products
    ('Whole Milk 1L', 'Dairy', 3.49, 45, 15, 'Local Dairy Co', DATEADD(day, -2, GETDATE())),
    ('Cheddar Cheese 200g', 'Dairy', 5.99, 23, 15, 'Cheese Masters', DATEADD(day, -1, GETDATE())),
    ('Greek Yogurt 500g', 'Dairy', 4.29, 8, 15, 'Dairy Fresh', DATEADD(day, -3, GETDATE())),
    ('Butter 250g', 'Dairy', 4.99, 67, 15, 'Local Dairy Co', GETDATE()),
    
    -- Beverages
    ('Orange Juice 1L', 'Beverages', 4.79, 32, 25, 'Citrus Co', DATEADD(day, -1, GETDATE())),
    ('Cola 2L', 'Beverages', 2.99, 15, 25, 'Soda Works', DATEADD(day, -2, GETDATE())),
    ('Bottled Water 1.5L', 'Beverages', 1.99, 120, 25, 'Pure Water', GETDATE()),
    ('Coffee Beans 500g', 'Beverages', 12.99, 28, 25, 'Coffee Roasters', DATEADD(day, -1, GETDATE())),
    
    -- Snacks
    ('Potato Chips 150g', 'Snacks', 3.29, 44, 20, 'Snack Foods', DATEADD(day, -2, GETDATE())),
    ('Chocolate Bar 100g', 'Snacks', 2.49, 67, 20, 'Sweet Treats', GETDATE()),
    ('Mixed Nuts 200g', 'Snacks', 6.99, 18, 20, 'Nutty Co', DATEADD(day, -3, GETDATE())),
    ('Crackers 250g', 'Snacks', 3.99, 35, 20, 'Crispy Foods', DATEADD(day, -1, GETDATE())),
    
    -- Produce
    ('Bananas 1kg', 'Produce', 2.99, 12, 10, 'Fresh Farms', DATEADD(day, -1, GETDATE())),
    ('Apples 1kg', 'Produce', 4.49, 8, 10, 'Orchard Fresh', DATEADD(day, -2, GETDATE())),
    ('Carrots 500g', 'Produce', 1.99, 25, 10, 'Garden Produce', GETDATE()),
    ('Lettuce Head', 'Produce', 2.79, 15, 10, 'Green Leaf', DATEADD(day, -1, GETDATE())),
    
    -- Meat
    ('Chicken Breast 500g', 'Meat', 8.99, 22, 8, 'Prime Meats', DATEADD(day, -1, GETDATE())),
    ('Ground Beef 500g', 'Meat', 9.49, 5, 8, 'Butcher Shop', DATEADD(day, -2, GETDATE())),
    ('Salmon Fillet 300g', 'Meat', 12.99, 14, 8, 'Ocean Fresh', GETDATE()),
    
    -- Bakery
    ('White Bread Loaf', 'Bakery', 2.99, 28, 12, 'Local Bakery', DATEADD(day, -1, GETDATE())),
    ('Croissants 6pk', 'Bakery', 5.49, 16, 12, 'French Baker', DATEADD(day, -2, GETDATE())),
    ('Bagels 6pk', 'Bakery', 4.99, 9, 12, 'Bread Co', GETDATE()),
    
    -- Frozen
    ('Frozen Pizza 400g', 'Frozen', 6.99, 34, 18, 'Frozen Foods', DATEADD(day, -1, GETDATE())),
    ('Ice Cream 1L', 'Frozen', 7.99, 22, 18, 'Cool Treats', DATEADD(day, -2, GETDATE())),
    ('Frozen Vegetables 500g', 'Frozen', 3.99, 41, 18, 'Healthy Frozen', GETDATE());

    PRINT '✓ Inserted ' + CAST(@@ROWCOUNT AS VARCHAR) + ' sample products';
END
ELSE
BEGIN
    PRINT '✓ Sample products already exist';
    SELECT 'Products Count' AS Info, COUNT(*) AS Count FROM Products;
END

-- Insert sample sales data if none exists
IF (SELECT COUNT(*) FROM Sales) = 0
BEGIN
    PRINT 'Inserting sample sales data...';
    
    -- Generate realistic sales data for the past 30 days
    DECLARE @StartDate DATE = DATEADD(day, -30, GETDATE());
    DECLARE @EndDate DATE = GETDATE();
    DECLARE @CurrentDate DATE = @StartDate;
    DECLARE @ProductCount INT = (SELECT COUNT(*) FROM Products);
    
    WHILE @CurrentDate <= @EndDate
    BEGIN
        -- Generate 15-25 random sales per day
        DECLARE @DailySales INT = 15 + (ABS(CHECKSUM(NEWID())) % 10);
        DECLARE @SaleCount INT = 0;
        
        WHILE @SaleCount < @DailySales
        BEGIN
            DECLARE @ProductId INT = 1 + (ABS(CHECKSUM(NEWID())) % @ProductCount);
            DECLARE @Quantity INT = 1 + (ABS(CHECKSUM(NEWID())) % 5);
            DECLARE @UnitPrice DECIMAL(10,2) = (SELECT UnitPrice FROM Products WHERE ProductId = @ProductId);
            DECLARE @SaleTime DATETIME2 = DATEADD(hour, ABS(CHECKSUM(NEWID())) % 12 + 8, @CurrentDate); -- Sales between 8 AM and 8 PM
            
            INSERT INTO Sales (ProductId, Quantity, UnitPrice, TotalAmount, SaleDate)
            VALUES (@ProductId, @Quantity, @UnitPrice, @Quantity * @UnitPrice, @SaleTime);
            
            SET @SaleCount = @SaleCount + 1;
        END
        
        SET @CurrentDate = DATEADD(day, 1, @CurrentDate);
    END
    
    PRINT '✓ Inserted sample sales data for the past 30 days';
    SELECT 'Sales Count' AS Info, COUNT(*) AS Count FROM Sales;
END
ELSE
BEGIN
    PRINT '✓ Sample sales data already exists';
    SELECT 'Sales Count' AS Info, COUNT(*) AS Count FROM Sales;
END

-- =============================================================================
-- STEP 5: UPDATE REORDER LEVELS BASED ON CATEGORIES
-- =============================================================================
PRINT 'STEP 5: Updating reorder levels based on product categories...';

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

PRINT '✓ Updated reorder levels based on categories';

-- =============================================================================
-- STEP 6: VERIFICATION
-- =============================================================================
PRINT 'STEP 6: Verifying database setup...';

-- Check table structure
SELECT 
    'Table Structure' AS Info,
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME IN ('Products', 'Sales')
ORDER BY TABLE_NAME, ORDINAL_POSITION;

-- Show data summary
SELECT 
    'Data Summary' AS Info,
    'Products' AS TableName,
    COUNT(*) AS RecordCount,
    MIN(LastUpdated) AS OldestUpdate,
    MAX(LastUpdated) AS NewestUpdate
FROM Products

UNION ALL

SELECT 
    'Data Summary' AS Info,
    'Sales' AS TableName,
    COUNT(*) AS RecordCount,
    MIN(SaleDate) AS OldestSale,
    MAX(SaleDate) AS NewestSale
FROM Sales;

-- Show sample inventory status
SELECT TOP 5
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
    UnitPrice
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
PRINT '=== Database Setup Completed Successfully! ===';
PRINT 'The SupermarketDB database is ready for the MCP server.';
PRINT 'All tables, indexes, and sample data have been created.';
PRINT 'Enhanced schema supports all MCP Resource tools.';
PRINT '';
