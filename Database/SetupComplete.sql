-- =============================================
-- Complete Database Setup for Supermarket MCP Server
-- SQL Server: DARKO\SQLEXPRESS
-- Run this script to set up everything at once
-- =============================================

PRINT 'Starting complete database setup for Supermarket MCP Server...';
GO

-- Create the database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SupermarketDB')
BEGIN
    CREATE DATABASE SupermarketDB;
    PRINT 'Database SupermarketDB created successfully.';
END
ELSE
BEGIN
    PRINT 'Database SupermarketDB already exists.';
END
GO

-- Use the database
USE SupermarketDB;
GO

-- Create Products table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Products] (
        [ProductId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ProductName] NVARCHAR(100) NOT NULL,
        [Category] NVARCHAR(50) NOT NULL,
        [Price] DECIMAL(10,2) NOT NULL,
        [StockQuantity] INT NOT NULL DEFAULT 0,
        [Supplier] NVARCHAR(100) NOT NULL,
        [CreatedDate] DATETIME2 DEFAULT GETDATE(),
        [ModifiedDate] DATETIME2 DEFAULT GETDATE()
    );
    PRINT 'Table Products created successfully.';
END
ELSE
BEGIN
    PRINT 'Table Products already exists.';
END
GO

-- Create Sales table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Sales]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Sales] (
        [SaleId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ProductId] INT NOT NULL,
        [Quantity] INT NOT NULL,
        [UnitPrice] DECIMAL(10,2) NOT NULL,
        [TotalAmount] DECIMAL(10,2) NOT NULL,
        [SaleDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [CreatedDate] DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT [FK_Sales_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([ProductId])
    );
    PRINT 'Table Sales created successfully.';
END
ELSE
BEGIN
    PRINT 'Table Sales already exists.';
END
GO

-- Create indexes for better performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_Category')
BEGIN
    CREATE INDEX [IX_Products_Category] ON [dbo].[Products] ([Category]);
    PRINT 'Index IX_Products_Category created successfully.';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_SaleDate')
BEGIN
    CREATE INDEX [IX_Sales_SaleDate] ON [dbo].[Sales] ([SaleDate]);
    PRINT 'Index IX_Sales_SaleDate created successfully.';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_ProductId')
BEGIN
    CREATE INDEX [IX_Sales_ProductId] ON [dbo].[Sales] ([ProductId]);
    PRINT 'Index IX_Sales_ProductId created successfully.';
END
GO

-- Insert sample products if table is empty
IF NOT EXISTS (SELECT TOP 1 1 FROM Products)
BEGIN
    INSERT INTO Products (ProductName, Category, Price, StockQuantity, Supplier) VALUES
    ('Milk 2L', 'Dairy', 3.99, 50, 'Fresh Dairy Co.'),
    ('Bread Whole Wheat', 'Bakery', 2.49, 30, 'Local Bakery'),
    ('Bananas 1kg', 'Fruits', 1.99, 100, 'Tropical Fruits Ltd.'),
    ('Chicken Breast 500g', 'Meat', 8.99, 25, 'Premium Meats'),
    ('Rice Basmati 1kg', 'Grains', 4.99, 40, 'Global Foods'),
    ('Tomatoes 500g', 'Vegetables', 2.99, 60, 'Fresh Farms'),
    ('Eggs 12 pack', 'Dairy', 3.49, 80, 'Fresh Dairy Co.'),
    ('Potatoes 2kg', 'Vegetables', 3.99, 45, 'Fresh Farms'),
    ('Orange Juice 1L', 'Beverages', 2.99, 35, 'Juice Co.'),
    ('Pasta Spaghetti 500g', 'Grains', 1.99, 55, 'Global Foods'),
    ('Beef Ground 400g', 'Meat', 6.99, 20, 'Premium Meats'),
    ('Apples Red 1kg', 'Fruits', 3.49, 70, 'Tropical Fruits Ltd.'),
    ('Cheese Cheddar 200g', 'Dairy', 4.99, 40, 'Fresh Dairy Co.'),
    ('Onions 1kg', 'Vegetables', 1.49, 90, 'Fresh Farms'),
    ('Coffee Beans 250g', 'Beverages', 12.99, 25, 'Coffee Co.'),
    ('Salmon Fillet 300g', 'Seafood', 15.99, 15, 'Ocean Fresh'),
    ('Yogurt Greek 500g', 'Dairy', 4.49, 30, 'Fresh Dairy Co.'),
    ('Carrots 1kg', 'Vegetables', 1.99, 75, 'Fresh Farms'),
    ('Grapes 500g', 'Fruits', 4.99, 40, 'Tropical Fruits Ltd.'),
    ('Pork Chops 400g', 'Meat', 7.99, 18, 'Premium Meats');

    PRINT 'Sample products inserted successfully.';
END
ELSE
BEGIN
    PRINT 'Products table already contains data.';
END
GO

-- Insert sample sales data if table is empty
IF NOT EXISTS (SELECT TOP 1 1 FROM Sales)
BEGIN
    -- Get product IDs for reference
    DECLARE @MilkId INT = (SELECT ProductId FROM Products WHERE ProductName = 'Milk 2L');
    DECLARE @BreadId INT = (SELECT ProductId FROM Products WHERE ProductName = 'Bread Whole Wheat');
    DECLARE @BananasId INT = (SELECT ProductId FROM Products WHERE ProductName = 'Bananas 1kg');
    DECLARE @ChickenId INT = (SELECT ProductId FROM Products WHERE ProductName = 'Chicken Breast 500g');
    DECLARE @RiceId INT = (SELECT ProductId FROM Products WHERE ProductName = 'Rice Basmati 1kg');
    DECLARE @TomatoesId INT = (SELECT ProductId FROM Products WHERE ProductName = 'Tomatoes 500g');
    DECLARE @EggsId INT = (SELECT ProductId FROM Products WHERE ProductName = 'Eggs 12 pack');
    DECLARE @PotatoesId INT = (SELECT ProductId FROM Products WHERE ProductName = 'Potatoes 2kg');
    DECLARE @JuiceId INT = (SELECT ProductId FROM Products WHERE ProductName = 'Orange Juice 1L');
    DECLARE @PastaId INT = (SELECT ProductId FROM Products WHERE ProductName = 'Pasta Spaghetti 500g');

    -- Insert sales for the last 30 days
    INSERT INTO Sales (ProductId, Quantity, UnitPrice, TotalAmount, SaleDate) VALUES
    -- Last week
    (@MilkId, 2, 3.99, 7.98, DATEADD(day, -1, GETDATE())),
    (@BreadId, 1, 2.49, 2.49, DATEADD(day, -1, GETDATE())),
    (@BananasId, 3, 1.99, 5.97, DATEADD(day, -2, GETDATE())),
    (@ChickenId, 1, 8.99, 8.99, DATEADD(day, -2, GETDATE())),
    (@RiceId, 2, 4.99, 9.98, DATEADD(day, -3, GETDATE())),
    (@TomatoesId, 1, 2.99, 2.99, DATEADD(day, -3, GETDATE())),
    (@EggsId, 1, 3.49, 3.49, DATEADD(day, -4, GETDATE())),
    (@PotatoesId, 1, 3.99, 3.99, DATEADD(day, -4, GETDATE())),
    (@JuiceId, 2, 2.99, 5.98, DATEADD(day, -5, GETDATE())),
    (@PastaId, 1, 1.99, 1.99, DATEADD(day, -5, GETDATE())),
    
    -- Two weeks ago
    (@MilkId, 1, 3.99, 3.99, DATEADD(day, -8, GETDATE())),
    (@BreadId, 2, 2.49, 4.98, DATEADD(day, -8, GETDATE())),
    (@BananasId, 2, 1.99, 3.98, DATEADD(day, -9, GETDATE())),
    (@ChickenId, 2, 8.99, 17.98, DATEADD(day, -9, GETDATE())),
    (@RiceId, 1, 4.99, 4.99, DATEADD(day, -10, GETDATE())),
    (@TomatoesId, 3, 2.99, 8.97, DATEADD(day, -10, GETDATE())),
    (@EggsId, 2, 3.49, 6.98, DATEADD(day, -11, GETDATE())),
    (@PotatoesId, 2, 3.99, 7.98, DATEADD(day, -11, GETDATE())),
    (@JuiceId, 1, 2.99, 2.99, DATEADD(day, -12, GETDATE())),
    (@PastaId, 3, 1.99, 5.97, DATEADD(day, -12, GETDATE())),
    
    -- Three weeks ago
    (@MilkId, 3, 3.99, 11.97, DATEADD(day, -15, GETDATE())),
    (@BreadId, 1, 2.49, 2.49, DATEADD(day, -15, GETDATE())),
    (@BananasId, 1, 1.99, 1.99, DATEADD(day, -16, GETDATE())),
    (@ChickenId, 1, 8.99, 8.99, DATEADD(day, -16, GETDATE())),
    (@RiceId, 2, 4.99, 9.98, DATEADD(day, -17, GETDATE())),
    (@TomatoesId, 2, 2.99, 5.98, DATEADD(day, -17, GETDATE())),
    (@EggsId, 1, 3.49, 3.49, DATEADD(day, -18, GETDATE())),
    (@PotatoesId, 1, 3.99, 3.99, DATEADD(day, -18, GETDATE())),
    (@JuiceId, 1, 2.99, 2.99, DATEADD(day, -19, GETDATE())),
    (@PastaId, 2, 1.99, 3.98, DATEADD(day, -19, GETDATE())),
    
    -- Four weeks ago
    (@MilkId, 2, 3.99, 7.98, DATEADD(day, -22, GETDATE())),
    (@BreadId, 2, 2.49, 4.98, DATEADD(day, -22, GETDATE())),
    (@BananasId, 2, 1.99, 3.98, DATEADD(day, -23, GETDATE())),
    (@ChickenId, 1, 8.99, 8.99, DATEADD(day, -23, GETDATE())),
    (@RiceId, 1, 4.99, 4.99, DATEADD(day, -24, GETDATE())),
    (@TomatoesId, 1, 2.99, 2.99, DATEADD(day, -24, GETDATE())),
    (@EggsId, 2, 3.49, 6.98, DATEADD(day, -25, GETDATE())),
    (@PotatoesId, 1, 3.99, 3.99, DATEADD(day, -25, GETDATE())),
    (@JuiceId, 2, 2.99, 5.98, DATEADD(day, -26, GETDATE())),
    (@PastaId, 1, 1.99, 1.99, DATEADD(day, -26, GETDATE()));

    PRINT 'Sample sales data inserted successfully.';
END
ELSE
BEGIN
    PRINT 'Sales table already contains data.';
END
GO

-- Display final summary
PRINT '=== DATABASE SETUP SUMMARY ===';
SELECT 
    'Products' as TableName,
    COUNT(*) as RecordCount
FROM Products
UNION ALL
SELECT 
    'Sales' as TableName,
    COUNT(*) as RecordCount
FROM Sales;

PRINT '=== SAMPLE DATA PREVIEW ===';
PRINT 'Top 5 Products:';
SELECT TOP 5 ProductName, Category, Price, StockQuantity FROM Products ORDER BY ProductId;

PRINT 'Recent Sales (Last 5):';
SELECT TOP 5 
    s.SaleId,
    p.ProductName,
    s.Quantity,
    s.TotalAmount,
    s.SaleDate
FROM Sales s
JOIN Products p ON s.ProductId = p.ProductId
ORDER BY s.SaleDate DESC;

PRINT '=== SETUP COMPLETED SUCCESSFULLY ===';
PRINT 'Your Supermarket MCP Server database is ready!';
PRINT 'Connection String: Server=DARKO\\SQLEXPRESS;Database=SupermarketDB;Integrated Security=true;TrustServerCertificate=true;';
GO 