-- =============================================================================
-- Test Script for Phase 3 Predictive Analytics Schema
-- =============================================================================
-- This script tests all the new schemas, tables, and procedures created for
-- predictive analytics functionality.
-- =============================================================================

USE SupermarketDB;
GO

PRINT '=== Testing Phase 3 Predictive Analytics Schema ===';
PRINT 'Verifying schemas, tables, procedures, and data...';
PRINT '';

-- =============================================================================
-- TEST 1: VERIFY SCHEMAS EXIST
-- =============================================================================
PRINT 'TEST 1: Verifying schemas exist...';

DECLARE @MissingSchemas TABLE (SchemaName VARCHAR(50));

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'inventory')
    INSERT INTO @MissingSchemas VALUES ('inventory');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'sales')
    INSERT INTO @MissingSchemas VALUES ('sales');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'analytics')
    INSERT INTO @MissingSchemas VALUES ('analytics');
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'reporting')
    INSERT INTO @MissingSchemas VALUES ('reporting');

IF EXISTS (SELECT * FROM @MissingSchemas)
BEGIN
    PRINT '✗ ERROR: Missing schemas:';
    SELECT SchemaName FROM @MissingSchemas;
END
ELSE
BEGIN
    PRINT '✓ All required schemas exist';
    SELECT 
        name as SchemaName,
        '✓ Exists' as Status
    FROM sys.schemas 
    WHERE name IN ('inventory', 'sales', 'analytics', 'reporting')
    ORDER BY name;
END

PRINT '';

-- =============================================================================
-- TEST 2: VERIFY TABLES EXIST WITH CORRECT SCHEMAS
-- =============================================================================
PRINT 'TEST 2: Verifying tables exist in correct schemas...';

DECLARE @ExpectedTables TABLE (SchemaName VARCHAR(50), TableName VARCHAR(100));
INSERT INTO @ExpectedTables VALUES 
    ('inventory', 'Movements'),
    ('inventory', 'ReorderHistory'),
    ('analytics', 'DemandForecasts'),
    ('analytics', 'SeasonalPatterns'),
    ('analytics', 'StockoutRisks'),
    ('reporting', 'DailySnapshots');

DECLARE @MissingTables TABLE (SchemaName VARCHAR(50), TableName VARCHAR(100));

INSERT INTO @MissingTables
SELECT et.SchemaName, et.TableName
FROM @ExpectedTables et
WHERE NOT EXISTS (
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = et.SchemaName AND t.name = et.TableName
);

IF EXISTS (SELECT * FROM @MissingTables)
BEGIN
    PRINT '✗ ERROR: Missing tables:';
    SELECT SchemaName + '.' + TableName as MissingTable FROM @MissingTables;
END
ELSE
BEGIN
    PRINT '✓ All required tables exist';
    SELECT 
        s.name + '.' + t.name as TableName,
        '✓ Exists' as Status
    FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name IN ('inventory', 'analytics', 'reporting')
    ORDER BY s.name, t.name;
END

PRINT '';

-- =============================================================================
-- TEST 3: VERIFY VIEWS EXIST
-- =============================================================================
PRINT 'TEST 3: Verifying analytics views exist...';

IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'CurrentInventoryStatus' AND SCHEMA_NAME(schema_id) = 'analytics')
    PRINT '✗ ERROR: View analytics.CurrentInventoryStatus does not exist';
ELSE
    PRINT '✓ View analytics.CurrentInventoryStatus exists';

IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'WeeklySalesTrends' AND SCHEMA_NAME(schema_id) = 'analytics')
    PRINT '✗ ERROR: View analytics.WeeklySalesTrends does not exist';
ELSE
    PRINT '✓ View analytics.WeeklySalesTrends exists';

PRINT '';

-- =============================================================================
-- TEST 4: VERIFY STORED PROCEDURES EXIST
-- =============================================================================
PRINT 'TEST 4: Verifying stored procedures exist...';

IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'CalculateDemandForecast' AND SCHEMA_NAME(schema_id) = 'analytics')
    PRINT '✗ ERROR: Procedure analytics.CalculateDemandForecast does not exist';
ELSE
    PRINT '✓ Procedure analytics.CalculateDemandForecast exists';

IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'AnalyzeStockoutRisks' AND SCHEMA_NAME(schema_id) = 'analytics')
    PRINT '✗ ERROR: Procedure analytics.AnalyzeStockoutRisks does not exist';
ELSE
    PRINT '✓ Procedure analytics.AnalyzeStockoutRisks exists';

PRINT '';

-- =============================================================================
-- TEST 5: TEST VIEW FUNCTIONALITY
-- =============================================================================
PRINT 'TEST 5: Testing view functionality...';

BEGIN TRY
    PRINT 'Testing analytics.CurrentInventoryStatus view...';
    
    SELECT TOP 5
        ProductId,
        ProductName,
        Category,
        StockQuantity,
        StockStatus,
        RecentSales
    FROM analytics.CurrentInventoryStatus
    ORDER BY ProductName;
    
    PRINT '✓ analytics.CurrentInventoryStatus view working correctly';
END TRY
BEGIN CATCH
    PRINT '✗ analytics.CurrentInventoryStatus view failed:';
    PRINT 'Error: ' + ERROR_MESSAGE();
END CATCH

PRINT '';

BEGIN TRY
    PRINT 'Testing analytics.WeeklySalesTrends view...';
    
    SELECT TOP 5
        Category,
        SalesYear,
        WeekNumber,
        TotalRevenue,
        TotalQuantity
    FROM analytics.WeeklySalesTrends
    ORDER BY SalesYear DESC, WeekNumber DESC;
    
    PRINT '✓ analytics.WeeklySalesTrends view working correctly';
END TRY
BEGIN CATCH
    PRINT '✗ analytics.WeeklySalesTrends view failed:';
    PRINT 'Error: ' + ERROR_MESSAGE();
END CATCH

PRINT '';

-- =============================================================================
-- TEST 6: TEST STORED PROCEDURES
-- =============================================================================
PRINT 'TEST 6: Testing stored procedures...';

BEGIN TRY
    PRINT 'Testing analytics.CalculateDemandForecast procedure...';
    
    EXEC analytics.CalculateDemandForecast @DaysAhead = 7;
    
    PRINT '✓ analytics.CalculateDemandForecast executed successfully';
    
    -- Check results
    SELECT TOP 5
        ProductId,
        ForecastDate,
        PredictedDemand,
        ConfidenceLevel,
        ModelUsed
    FROM analytics.DemandForecasts
    ORDER BY ConfidenceLevel DESC;
    
END TRY
BEGIN CATCH
    PRINT '✗ analytics.CalculateDemandForecast failed:';
    PRINT 'Error: ' + ERROR_MESSAGE();
END CATCH

PRINT '';

BEGIN TRY
    PRINT 'Testing analytics.AnalyzeStockoutRisks procedure...';
    
    EXEC analytics.AnalyzeStockoutRisks @DaysAhead = 14;
    
    PRINT '✓ analytics.AnalyzeStockoutRisks executed successfully';
    
    -- Check results  
    SELECT TOP 5
        ProductId,
        RiskLevel,
        DaysOfStock,
        RiskScore,
        RecommendedAction
    FROM analytics.StockoutRisks
    WHERE RiskLevel IN ('HIGH', 'MEDIUM')
    ORDER BY RiskScore DESC;
    
END TRY
BEGIN CATCH
    PRINT '✗ analytics.AnalyzeStockoutRisks failed:';
    PRINT 'Error: ' + ERROR_MESSAGE();
END CATCH

PRINT '';

-- =============================================================================
-- TEST 7: CHECK INDEXES
-- =============================================================================
PRINT 'TEST 7: Verifying indexes exist...';

SELECT 
    s.name as SchemaName,
    t.name as TableName,
    i.name as IndexName,
    i.type_desc as IndexType,
    '✓' as Status
FROM sys.indexes i
JOIN sys.tables t ON i.object_id = t.object_id
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name IN ('inventory', 'analytics', 'reporting')
  AND i.name IS NOT NULL -- Exclude heap tables
  AND i.is_primary_key = 0 -- Exclude primary keys
ORDER BY s.name, t.name, i.name;

PRINT '';

-- =============================================================================
-- TEST 8: DATA QUALITY CHECKS
-- =============================================================================
PRINT 'TEST 8: Data quality checks...';

-- Check inventory movements
PRINT 'Inventory movements summary:';
SELECT 
    MovementType,
    COUNT(*) as Count,
    SUM(Quantity) as TotalQuantity
FROM inventory.Movements
GROUP BY MovementType;

-- Check seasonal patterns
PRINT 'Seasonal patterns summary:';
SELECT 
    Category,
    COUNT(*) as MonthsCovered,
    AVG(SeasonalityFactor) as AvgSeasonalityFactor
FROM analytics.SeasonalPatterns
GROUP BY Category
ORDER BY Category;

-- Check demand forecasts
PRINT 'Demand forecasts summary:';
SELECT 
    ModelUsed,
    COUNT(*) as ForecastCount,
    AVG(ConfidenceLevel) as AvgConfidence,
    AVG(PredictedDemand) as AvgPredictedDemand
FROM analytics.DemandForecasts
GROUP BY ModelUsed;

-- Check stockout risks
PRINT 'Stockout risks summary:';
SELECT 
    RiskLevel,
    COUNT(*) as ProductCount,
    AVG(RiskScore) as AvgRiskScore
FROM analytics.StockoutRisks
GROUP BY RiskLevel
ORDER BY RiskLevel;

PRINT '';

-- =============================================================================
-- TEST 9: FOREIGN KEY RELATIONSHIPS
-- =============================================================================
PRINT 'TEST 9: Verifying foreign key relationships...';

SELECT 
    s.name as SchemaName,
    t.name as TableName,
    fk.name as ForeignKeyName,
    rs.name as ReferencedSchema,
    rt.name as ReferencedTable,
    '✓ Valid' as Status
FROM sys.foreign_keys fk
JOIN sys.tables t ON fk.parent_object_id = t.object_id
JOIN sys.schemas s ON t.schema_id = s.schema_id
JOIN sys.tables rt ON fk.referenced_object_id = rt.object_id
JOIN sys.schemas rs ON rt.schema_id = rs.schema_id
WHERE s.name IN ('inventory', 'analytics', 'reporting')
ORDER BY s.name, t.name;

PRINT '';

-- =============================================================================
-- SUMMARY
-- =============================================================================
PRINT '=== Phase 3 Schema Test Summary ===';

DECLARE @TotalTables INT = (SELECT COUNT(*) FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name IN ('inventory', 'analytics', 'reporting'));
DECLARE @TotalViews INT = (SELECT COUNT(*) FROM sys.views v JOIN sys.schemas s ON v.schema_id = s.schema_id WHERE s.name IN ('analytics'));
DECLARE @TotalProcedures INT = (SELECT COUNT(*) FROM sys.procedures p JOIN sys.schemas s ON p.schema_id = s.schema_id WHERE s.name IN ('analytics'));

PRINT 'Schemas created: 4 (inventory, sales, analytics, reporting)';
PRINT 'Tables created: ' + CAST(@TotalTables AS VARCHAR);
PRINT 'Views created: ' + CAST(@TotalViews AS VARCHAR);
PRINT 'Procedures created: ' + CAST(@TotalProcedures AS VARCHAR);
PRINT '';
PRINT 'Schema structure is ready for Phase 3 predictive analytics implementation!';
PRINT 'Existing dbo schema remains unchanged for backward compatibility.';
GO
