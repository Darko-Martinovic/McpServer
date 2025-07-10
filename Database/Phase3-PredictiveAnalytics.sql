-- =============================================================================
-- Phase 3: Predictive Analytics Database Schema Enhancement
-- =============================================================================
-- This script creates new schemas and tables for predictive analytics while
-- maintaining backward compatibility with existing dbo schema tables.
-- 
-- Schema Organization:
-- - inventory: Product and stock management
-- - sales: Sales transactions and analytics
-- - analytics: Predictive analytics and forecasting
-- - reporting: Business intelligence and reports
-- =============================================================================

USE SupermarketDB;
GO

PRINT '=== Phase 3: Predictive Analytics Schema Setup ===';
PRINT 'Creating schemas and tables for advanced business intelligence...';
PRINT '';

-- =============================================================================
-- STEP 1: CREATE SCHEMAS
-- =============================================================================
PRINT 'STEP 1: Creating database schemas...';

-- Inventory Management Schema
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'inventory')
BEGIN
    EXEC('CREATE SCHEMA inventory');
    PRINT '✓ Created schema: inventory';
END
ELSE
    PRINT '✓ Schema already exists: inventory';

-- Sales Analytics Schema  
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'sales')
BEGIN
    EXEC('CREATE SCHEMA sales');
    PRINT '✓ Created schema: sales';
END
ELSE
    PRINT '✓ Schema already exists: sales';

-- Predictive Analytics Schema
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'analytics')
BEGIN
    EXEC('CREATE SCHEMA analytics');
    PRINT '✓ Created schema: analytics';
END
ELSE
    PRINT '✓ Schema already exists: analytics';

-- Business Intelligence Reporting Schema
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'reporting')
BEGIN
    EXEC('CREATE SCHEMA reporting');
    PRINT '✓ Created schema: reporting';
END
ELSE
    PRINT '✓ Schema already exists: reporting';

PRINT '';

-- =============================================================================
-- STEP 2: CREATE INVENTORY SCHEMA TABLES
-- =============================================================================
PRINT 'STEP 2: Creating inventory management tables...';

-- Inventory Movements Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'inventory.Movements') AND type in (N'U'))
BEGIN
    CREATE TABLE inventory.Movements (
        MovementId BIGINT IDENTITY(1,1) PRIMARY KEY,
        ProductId INT NOT NULL,
        MovementType VARCHAR(20) NOT NULL, -- 'SALE', 'RESTOCK', 'ADJUSTMENT', 'RETURN'
        Quantity INT NOT NULL, -- Positive for incoming, negative for outgoing
        PreviousStock INT NOT NULL,
        NewStock INT NOT NULL,
        UnitCost DECIMAL(10,2) NULL,
        Reason VARCHAR(100) NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
        CreatedBy VARCHAR(50) NOT NULL DEFAULT SYSTEM_USER,
        FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId)
    );
    
    -- Index for performance
    CREATE INDEX IX_InventoryMovements_ProductId_Date 
    ON inventory.Movements (ProductId, CreatedDate DESC);
    
    CREATE INDEX IX_InventoryMovements_Type_Date 
    ON inventory.Movements (MovementType, CreatedDate DESC);
    
    PRINT '✓ Created table: inventory.Movements';
END
ELSE
    PRINT '✓ Table already exists: inventory.Movements';

-- Reorder Points History Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'inventory.ReorderHistory') AND type in (N'U'))
BEGIN
    CREATE TABLE inventory.ReorderHistory (
        ReorderHistoryId BIGINT IDENTITY(1,1) PRIMARY KEY,
        ProductId INT NOT NULL,
        OldReorderLevel INT NOT NULL,
        NewReorderLevel INT NOT NULL,
        CalculationMethod VARCHAR(50) NOT NULL, -- 'MANUAL', 'PREDICTIVE', 'SEASONAL'
        EffectiveDate DATE NOT NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
        CreatedBy VARCHAR(50) NOT NULL DEFAULT SYSTEM_USER,
        FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId)
    );
    
    CREATE INDEX IX_ReorderHistory_ProductId_Date 
    ON inventory.ReorderHistory (ProductId, EffectiveDate DESC);
    
    PRINT '✓ Created table: inventory.ReorderHistory';
END
ELSE
    PRINT '✓ Table already exists: inventory.ReorderHistory';

PRINT '';

-- =============================================================================
-- STEP 3: CREATE ANALYTICS SCHEMA TABLES
-- =============================================================================
PRINT 'STEP 3: Creating predictive analytics tables...';

-- Demand Forecasts Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'analytics.DemandForecasts') AND type in (N'U'))
BEGIN
    CREATE TABLE analytics.DemandForecasts (
        ForecastId BIGINT IDENTITY(1,1) PRIMARY KEY,
        ProductId INT NOT NULL,
        ForecastDate DATE NOT NULL,
        PredictedDemand DECIMAL(10,2) NOT NULL,
        ConfidenceLevel DECIMAL(5,2) NOT NULL, -- 0.00 to 100.00
        TrendDirection VARCHAR(20) NOT NULL, -- 'INCREASING', 'DECREASING', 'STABLE'
        SeasonalFactor DECIMAL(5,2) NOT NULL DEFAULT 1.00,
        ModelUsed VARCHAR(50) NOT NULL, -- 'MOVING_AVERAGE', 'SEASONAL', 'TREND'
        CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
        ActualDemand DECIMAL(10,2) NULL, -- Populated after the fact for accuracy tracking
        AccuracyScore DECIMAL(5,2) NULL, -- Calculated accuracy percentage
        FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId)
    );
    
    -- Indexes for performance
    CREATE INDEX IX_DemandForecasts_ProductId_Date 
    ON analytics.DemandForecasts (ProductId, ForecastDate);
    
    CREATE INDEX IX_DemandForecasts_Date_Confidence 
    ON analytics.DemandForecasts (ForecastDate, ConfidenceLevel DESC);
    
    PRINT '✓ Created table: analytics.DemandForecasts';
END
ELSE
    PRINT '✓ Table already exists: analytics.DemandForecasts';

-- Seasonal Patterns Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'analytics.SeasonalPatterns') AND type in (N'U'))
BEGIN
    CREATE TABLE analytics.SeasonalPatterns (
        PatternId BIGINT IDENTITY(1,1) PRIMARY KEY,
        Category VARCHAR(100) NOT NULL,
        Month TINYINT NOT NULL, -- 1-12
        MonthName VARCHAR(15) NOT NULL,
        SeasonalityFactor DECIMAL(5,2) NOT NULL, -- Multiplier (e.g., 1.25 = 25% above average)
        AverageMonthlySales DECIMAL(12,2) NOT NULL,
        TrendClassification VARCHAR(20) NOT NULL, -- 'PEAK', 'LOW', 'NORMAL'
        DataYears INT NOT NULL, -- Number of years of data used
        LastCalculated DATETIME2 NOT NULL DEFAULT GETDATE(),
        UNIQUE (Category, Month)
    );
    
    CREATE INDEX IX_SeasonalPatterns_Category_Month 
    ON analytics.SeasonalPatterns (Category, Month);
    
    PRINT '✓ Created table: analytics.SeasonalPatterns';
END
ELSE
    PRINT '✓ Table already exists: analytics.SeasonalPatterns';

-- Stockout Risk Analysis Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'analytics.StockoutRisks') AND type in (N'U'))
BEGIN
    CREATE TABLE analytics.StockoutRisks (
        RiskId BIGINT IDENTITY(1,1) PRIMARY KEY,
        ProductId INT NOT NULL,
        AnalysisDate DATE NOT NULL,
        CurrentStock INT NOT NULL,
        PredictedDemand DECIMAL(10,2) NOT NULL,
        DaysOfStock DECIMAL(5,1) NOT NULL,
        RiskLevel VARCHAR(10) NOT NULL, -- 'HIGH', 'MEDIUM', 'LOW'
        RiskScore DECIMAL(5,2) NOT NULL, -- 0.00 to 100.00
        RecommendedAction VARCHAR(100) NULL,
        EstimatedStockoutDate DATE NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
        FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId)
    );
    
    CREATE INDEX IX_StockoutRisks_RiskLevel_Date 
    ON analytics.StockoutRisks (RiskLevel, AnalysisDate DESC);
    
    CREATE INDEX IX_StockoutRisks_ProductId_Date 
    ON analytics.StockoutRisks (ProductId, AnalysisDate DESC);
    
    PRINT '✓ Created table: analytics.StockoutRisks';
END
ELSE
    PRINT '✓ Table already exists: analytics.StockoutRisks';

PRINT '';

-- =============================================================================
-- STEP 4: CREATE REPORTING SCHEMA TABLES
-- =============================================================================
PRINT 'STEP 4: Creating business intelligence reporting tables...';

-- Snapshot table for daily business metrics
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'reporting.DailySnapshots') AND type in (N'U'))
BEGIN
    CREATE TABLE reporting.DailySnapshots (
        SnapshotId BIGINT IDENTITY(1,1) PRIMARY KEY,
        SnapshotDate DATE NOT NULL UNIQUE,
        TotalProducts INT NOT NULL,
        TotalInventoryValue DECIMAL(15,2) NOT NULL,
        LowStockProducts INT NOT NULL,
        OutOfStockProducts INT NOT NULL,
        DailyRevenue DECIMAL(12,2) NOT NULL,
        DailyTransactions INT NOT NULL,
        TopCategory VARCHAR(100) NULL,
        TopCategoryRevenue DECIMAL(12,2) NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE()
    );
    
    CREATE INDEX IX_DailySnapshots_Date 
    ON reporting.DailySnapshots (SnapshotDate DESC);
    
    PRINT '✓ Created table: reporting.DailySnapshots';
END
ELSE
    PRINT '✓ Table already exists: reporting.DailySnapshots';

PRINT '';

-- =============================================================================
-- STEP 5: CREATE VIEWS FOR EASY ACCESS
-- =============================================================================
PRINT 'STEP 5: Creating views for simplified access...';

-- Current Inventory Status View
IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'CurrentInventoryStatus')
BEGIN
    EXEC('
    CREATE VIEW analytics.CurrentInventoryStatus AS
    SELECT 
        p.ProductId,
        p.ProductName,
        p.Category,
        p.StockQuantity,
        p.ReorderLevel,
        p.UnitPrice,
        p.LastUpdated,
        CASE 
            WHEN p.StockQuantity <= 0 THEN ''OUT_OF_STOCK''
            WHEN p.StockQuantity <= p.ReorderLevel THEN ''LOW_STOCK''
            WHEN p.StockQuantity <= p.ReorderLevel * 2 THEN ''MEDIUM_STOCK''
            ELSE ''IN_STOCK''
        END AS StockStatus,
        COALESCE(recent.RecentSales, 0) as RecentSales,
        COALESCE(recent.RecentRevenue, 0) as RecentRevenue
    FROM dbo.Products p
    LEFT JOIN (
        SELECT 
            ProductId, 
            SUM(Quantity) as RecentSales,
            SUM(TotalAmount) as RecentRevenue
        FROM dbo.Sales 
        WHERE SaleDate >= DATEADD(day, -7, GETDATE())
        GROUP BY ProductId
    ) recent ON p.ProductId = recent.ProductId
    ');
    
    PRINT '✓ Created view: analytics.CurrentInventoryStatus';
END
ELSE
    PRINT '✓ View already exists: analytics.CurrentInventoryStatus';

-- Weekly Sales Trends View
IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'WeeklySalesTrends')
BEGIN
    EXEC('
    CREATE VIEW analytics.WeeklySalesTrends AS
    SELECT 
        p.Category,
        DATEPART(YEAR, s.SaleDate) as SalesYear,
        DATEPART(WEEK, s.SaleDate) as WeekNumber,
        COUNT(DISTINCT s.SaleId) as Transactions,
        SUM(s.Quantity) as TotalQuantity,
        SUM(s.TotalAmount) as TotalRevenue,
        AVG(s.TotalAmount) as AvgTransactionValue,
        COUNT(DISTINCT s.ProductId) as UniqueProducts
    FROM dbo.Sales s
    JOIN dbo.Products p ON s.ProductId = p.ProductId
    WHERE s.SaleDate >= DATEADD(month, -6, GETDATE())
    GROUP BY p.Category, DATEPART(YEAR, s.SaleDate), DATEPART(WEEK, s.SaleDate)
    ');
    
    PRINT '✓ Created view: analytics.WeeklySalesTrends';
END
ELSE
    PRINT '✓ View already exists: analytics.WeeklySalesTrends';

PRINT '';

-- =============================================================================
-- STEP 6: INSERT INITIAL ANALYTICAL DATA
-- =============================================================================
PRINT 'STEP 6: Inserting initial analytical data...';

-- Insert sample inventory movements based on recent sales
IF (SELECT COUNT(*) FROM inventory.Movements) = 0
BEGIN
    PRINT 'Generating sample inventory movements...';
    
    INSERT INTO inventory.Movements (ProductId, MovementType, Quantity, PreviousStock, NewStock, CreatedDate)
    SELECT 
        s.ProductId,
        'SALE',
        -s.Quantity, -- Negative for outgoing
        p.StockQuantity + s.Quantity as PreviousStock,
        p.StockQuantity as NewStock,
        s.SaleDate
    FROM dbo.Sales s
    JOIN dbo.Products p ON s.ProductId = p.ProductId
    WHERE s.SaleDate >= DATEADD(day, -7, GETDATE());
    
    PRINT '✓ Inserted ' + CAST(@@ROWCOUNT AS VARCHAR) + ' inventory movements';
END
ELSE
    PRINT '✓ Inventory movements data already exists';

-- Calculate and insert seasonal patterns
IF (SELECT COUNT(*) FROM analytics.SeasonalPatterns) = 0
BEGIN
    PRINT 'Calculating seasonal patterns...';
    
    INSERT INTO analytics.SeasonalPatterns (Category, Month, MonthName, SeasonalityFactor, AverageMonthlySales, TrendClassification, DataYears)
    SELECT 
        p.Category,
        MONTH(s.SaleDate) as Month,
        DATENAME(MONTH, s.SaleDate) as MonthName,
        CASE 
            WHEN AVG(s.TotalAmount) > (SELECT AVG(TotalAmount) * 1.15 FROM dbo.Sales) THEN 1.25
            WHEN AVG(s.TotalAmount) < (SELECT AVG(TotalAmount) * 0.85 FROM dbo.Sales) THEN 0.85
            ELSE 1.00
        END as SeasonalityFactor,
        AVG(s.TotalAmount) as AverageMonthlySales,
        CASE 
            WHEN AVG(s.TotalAmount) > (SELECT AVG(TotalAmount) * 1.15 FROM dbo.Sales) THEN 'PEAK'
            WHEN AVG(s.TotalAmount) < (SELECT AVG(TotalAmount) * 0.85 FROM dbo.Sales) THEN 'LOW'
            ELSE 'NORMAL'
        END as TrendClassification,
        1 as DataYears -- We only have sample data
    FROM dbo.Sales s
    JOIN dbo.Products p ON s.ProductId = p.ProductId
    GROUP BY p.Category, MONTH(s.SaleDate), DATENAME(MONTH, s.SaleDate)
    HAVING COUNT(*) >= 5; -- Only categories with sufficient data
    
    PRINT '✓ Calculated seasonal patterns for ' + CAST(@@ROWCOUNT AS VARCHAR) + ' category-month combinations';
END
ELSE
    PRINT '✓ Seasonal patterns already calculated';

PRINT '';

-- =============================================================================
-- STEP 7: CREATE STORED PROCEDURES FOR ANALYTICS
-- =============================================================================
PRINT 'STEP 7: Creating stored procedures for analytics...';

-- Procedure to calculate demand forecast
IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'CalculateDemandForecast')
BEGIN
    EXEC('
    CREATE PROCEDURE analytics.CalculateDemandForecast
        @ProductId INT = NULL,
        @DaysAhead INT = 7
    AS
    BEGIN
        SET NOCOUNT ON;
        
        DECLARE @AnalysisDate DATE = CAST(GETDATE() AS DATE);
        
        -- Clear old forecasts for the same period
        DELETE FROM analytics.DemandForecasts 
        WHERE ForecastDate = @AnalysisDate AND (@ProductId IS NULL OR ProductId = @ProductId);
        
        -- Calculate demand forecast using moving average with trend
        WITH SalesHistory AS (
            SELECT 
                s.ProductId,
                CAST(s.SaleDate AS DATE) as SaleDay,
                SUM(s.Quantity) as DailyQuantity
            FROM dbo.Sales s
            WHERE s.SaleDate >= DATEADD(day, -30, GETDATE())
                AND (@ProductId IS NULL OR s.ProductId = @ProductId)
            GROUP BY s.ProductId, CAST(s.SaleDate AS DATE)
        ),
        DemandAnalysis AS (
            SELECT 
                sh.ProductId,
                AVG(sh.DailyQuantity) as AvgDailyDemand,
                STDEV(sh.DailyQuantity) as StdDevDemand,
                COUNT(*) as DataPoints
            FROM SalesHistory sh
            GROUP BY sh.ProductId
            HAVING COUNT(*) >= 7 -- Need at least a week of data
        )
        INSERT INTO analytics.DemandForecasts (
            ProductId, ForecastDate, PredictedDemand, ConfidenceLevel, 
            TrendDirection, ModelUsed
        )
        SELECT 
            da.ProductId,
            @AnalysisDate,
            da.AvgDailyDemand * @DaysAhead as PredictedDemand,
            CASE 
                WHEN da.DataPoints >= 20 THEN 85.0
                WHEN da.DataPoints >= 14 THEN 75.0
                ELSE 60.0
            END as ConfidenceLevel,
            ''STABLE'' as TrendDirection, -- Simplified for initial version
            ''MOVING_AVERAGE'' as ModelUsed
        FROM DemandAnalysis da;
        
        SELECT @@ROWCOUNT as ForecastsGenerated;
    END
    ');
    
    PRINT '✓ Created procedure: analytics.CalculateDemandForecast';
END
ELSE
    PRINT '✓ Procedure already exists: analytics.CalculateDemandForecast';

-- Procedure to analyze stockout risks
IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'AnalyzeStockoutRisks')
BEGIN
    EXEC('
    CREATE PROCEDURE analytics.AnalyzeStockoutRisks
        @DaysAhead INT = 14
    AS
    BEGIN
        SET NOCOUNT ON;
        
        DECLARE @AnalysisDate DATE = CAST(GETDATE() AS DATE);
        
        -- Clear old risk analysis
        DELETE FROM analytics.StockoutRisks WHERE AnalysisDate = @AnalysisDate;
        
        -- Calculate stockout risks based on current stock and predicted demand
        WITH CurrentForecasts AS (
            SELECT 
                df.ProductId,
                df.PredictedDemand / 7.0 as DailyDemand -- Convert weekly to daily
            FROM analytics.DemandForecasts df
            WHERE df.ForecastDate = @AnalysisDate
        )
        INSERT INTO analytics.StockoutRisks (
            ProductId, AnalysisDate, CurrentStock, PredictedDemand, 
            DaysOfStock, RiskLevel, RiskScore, RecommendedAction, EstimatedStockoutDate
        )
        SELECT 
            p.ProductId,
            @AnalysisDate,
            p.StockQuantity,
            cf.DailyDemand * @DaysAhead as PredictedDemand,
            CASE 
                WHEN cf.DailyDemand > 0 THEN p.StockQuantity / cf.DailyDemand
                ELSE 999
            END as DaysOfStock,
            CASE 
                WHEN cf.DailyDemand > 0 AND p.StockQuantity / cf.DailyDemand <= 3 THEN ''HIGH''
                WHEN cf.DailyDemand > 0 AND p.StockQuantity / cf.DailyDemand <= 7 THEN ''MEDIUM''
                ELSE ''LOW''
            END as RiskLevel,
            CASE 
                WHEN cf.DailyDemand > 0 THEN 
                    CASE 
                        WHEN p.StockQuantity / cf.DailyDemand <= 1 THEN 95.0
                        WHEN p.StockQuantity / cf.DailyDemand <= 3 THEN 80.0
                        WHEN p.StockQuantity / cf.DailyDemand <= 7 THEN 50.0
                        ELSE 10.0
                    END
                ELSE 5.0
            END as RiskScore,
            CASE 
                WHEN cf.DailyDemand > 0 AND p.StockQuantity / cf.DailyDemand <= 3 THEN ''URGENT REORDER''
                WHEN cf.DailyDemand > 0 AND p.StockQuantity / cf.DailyDemand <= 7 THEN ''SCHEDULE REORDER''
                ELSE ''MONITOR''
            END as RecommendedAction,
            CASE 
                WHEN cf.DailyDemand > 0 THEN DATEADD(day, p.StockQuantity / cf.DailyDemand, @AnalysisDate)
                ELSE NULL
            END as EstimatedStockoutDate
        FROM dbo.Products p
        JOIN CurrentForecasts cf ON p.ProductId = cf.ProductId;
        
        SELECT @@ROWCOUNT as RiskAnalysisGenerated;
    END
    ');
    
    PRINT '✓ Created procedure: analytics.AnalyzeStockoutRisks';
END
ELSE
    PRINT '✓ Procedure already exists: analytics.AnalyzeStockoutRisks';

PRINT '';

-- =============================================================================
-- STEP 8: VERIFICATION AND SUMMARY
-- =============================================================================
PRINT 'STEP 8: Verification and summary...';

-- Check all schemas were created
SELECT 
    'Schema Information' as Category,
    name as SchemaName,
    'Created' as Status
FROM sys.schemas 
WHERE name IN ('inventory', 'sales', 'analytics', 'reporting')
ORDER BY name;

PRINT '';

-- Check all tables were created
SELECT 
    'Table Information' as Category,
    SCHEMA_NAME(schema_id) as SchemaName,
    name as TableName,
    'Created' as Status
FROM sys.tables 
WHERE SCHEMA_NAME(schema_id) IN ('inventory', 'analytics', 'reporting')
ORDER BY SCHEMA_NAME(schema_id), name;

PRINT '';

-- Show data counts
PRINT 'Data Summary:';
SELECT 'inventory.Movements' as TableName, COUNT(*) as RecordCount FROM inventory.Movements
UNION ALL
SELECT 'analytics.SeasonalPatterns', COUNT(*) FROM analytics.SeasonalPatterns
UNION ALL
SELECT 'analytics.DemandForecasts', COUNT(*) FROM analytics.DemandForecasts
UNION ALL
SELECT 'analytics.StockoutRisks', COUNT(*) FROM analytics.StockoutRisks;

PRINT '';
PRINT '=== Phase 3 Database Schema Setup Complete ===';
PRINT 'New schemas created: inventory, sales, analytics, reporting';
PRINT 'Predictive analytics tables and procedures are ready';
PRINT 'Existing dbo tables remain unchanged for backward compatibility';
PRINT 'Ready for service layer implementation!';
PRINT '';
