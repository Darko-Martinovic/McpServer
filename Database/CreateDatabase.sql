-- =============================================
-- Supermarket MCP Server Database Setup
-- SQL Server: DARKO\SQLEXPRESS
-- =============================================

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

PRINT 'Database setup completed successfully!';
GO 