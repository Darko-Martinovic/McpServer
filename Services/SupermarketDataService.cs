using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;
using McpServer.Models;
using McpServer.Services.Interfaces;
using McpServer.Configuration;

namespace McpServer.Services;

public class SupermarketDataService : ISupermarketDataService
{
    private readonly string _connectionString;
    private readonly ILogger<SupermarketDataService> _logger;

    public SupermarketDataService(
        IOptions<ConnectionStringOptions> connectionOptions,
        ILogger<SupermarketDataService> logger
    )
    {
        _connectionString = connectionOptions.Value.DefaultConnection;
        _logger = logger;

        _logger.LogInformation("SupermarketDataService initialized with connection string: {ConnectionString}",
            string.IsNullOrEmpty(_connectionString) ? "[EMPTY]" : "[SET]");
    }

    public async Task<IEnumerable<Product>> GetProductsAsync()
    {
        var requestId = LoggingHelper.CreateRequestId();
        _logger.LogInformation("[{RequestId}] GetProductsAsync called", requestId);

        try
        {
            var products = new List<Product>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql =
                "SELECT ProductId, ProductName, Category, UnitPrice, StockQuantity, Supplier FROM Products";
            LoggingHelper.LogDatabaseOperationStart(_logger, requestId, "GetProducts", sql);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                products.Add(
                    new Product
                    {
                        ProductId = reader.GetInt32("ProductId"),
                        ProductName = reader.GetString("ProductName"),
                        Category = reader.GetString("Category"),
                        Price = reader.GetDecimal("UnitPrice"),
                        StockQuantity = reader.GetInt32("StockQuantity"),
                        Supplier = reader.GetString("Supplier")
                    }
                );
            }
            stopwatch.Stop();

            LoggingHelper.LogDatabaseOperationSuccess(
                _logger,
                requestId,
                "GetProducts",
                products.Count,
                stopwatch.ElapsedMilliseconds
            );
            return products;
        }
        catch (Exception ex)
        {
            LoggingHelper.LogDatabaseError(_logger, ex, requestId, "GetProductsAsync");
            return new List<Product>();
        }
    }

    public async Task<IEnumerable<SalesRecord>> GetSalesDataAsync(
        DateTime startDate,
        DateTime endDate
    )
    {
        var requestId = LoggingHelper.CreateRequestId();
        _logger.LogInformation(
            "[{RequestId}] GetSalesDataAsync called with startDate: {StartDate}, endDate: {EndDate}",
            requestId,
            startDate.ToString("yyyy-MM-dd"),
            endDate.ToString("yyyy-MM-dd")
        );

        try
        {
            var salesRecords = new List<SalesRecord>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql =
                "SELECT s.SaleId, s.ProductId, p.ProductName, s.Quantity, s.UnitPrice, s.TotalAmount, s.SaleDate FROM Sales s JOIN Products p ON s.ProductId = p.ProductId WHERE s.SaleDate >= @StartDate AND s.SaleDate <= @EndDate";
            LoggingHelper.LogDatabaseOperationStart(
                _logger,
                requestId,
                "GetSalesData",
                sql,
                ("StartDate", startDate),
                ("EndDate", endDate)
            );

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                salesRecords.Add(
                    new SalesRecord
                    {
                        SaleId = reader.GetInt32("SaleId"),
                        ProductId = reader.GetInt32("ProductId"),
                        ProductName = reader.GetString("ProductName"),
                        Quantity = reader.GetInt32("Quantity"),
                        UnitPrice = reader.GetDecimal("UnitPrice"),
                        TotalAmount = reader.GetDecimal("TotalAmount"),
                        SaleDate = reader.GetDateTime("SaleDate")
                    }
                );
            }
            stopwatch.Stop();

            LoggingHelper.LogDatabaseOperationSuccess(
                _logger,
                requestId,
                "GetSalesData",
                salesRecords.Count,
                stopwatch.ElapsedMilliseconds
            );
            return salesRecords;
        }
        catch (Exception ex)
        {
            LoggingHelper.LogDatabaseError(_logger, ex, requestId, "GetSalesDataAsync");
            return new List<SalesRecord>();
        }
    }

    public async Task<decimal> GetTotalRevenueAsync(DateTime startDate, DateTime endDate)
    {
        var requestId = LoggingHelper.CreateRequestId();
        _logger.LogInformation(
            "[{RequestId}] GetTotalRevenueAsync called with startDate: {StartDate}, endDate: {EndDate}",
            requestId,
            startDate.ToString("yyyy-MM-dd"),
            endDate.ToString("yyyy-MM-dd")
        );

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql =
                "SELECT ISNULL(SUM(TotalAmount), 0) FROM Sales WHERE SaleDate >= @StartDate AND SaleDate <= @EndDate";
            LoggingHelper.LogDatabaseOperationStart(
                _logger,
                requestId,
                "GetTotalRevenue",
                sql,
                ("StartDate", startDate),
                ("EndDate", endDate)
            );

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);
            var result = await command.ExecuteScalarAsync();
            stopwatch.Stop();

            var revenue = result != null ? (decimal)result : 0;
            LoggingHelper.LogDatabaseOperationSuccess(
                _logger,
                requestId,
                "GetTotalRevenue",
                1,
                stopwatch.ElapsedMilliseconds,
                revenue
            );
            return revenue;
        }
        catch (Exception ex)
        {
            LoggingHelper.LogDatabaseError(_logger, ex, requestId, "GetTotalRevenueAsync");
            return 0;
        }
    }

    public async Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold)
    {
        var requestId = LoggingHelper.CreateRequestId();
        _logger.LogInformation(
            "[{RequestId}] GetLowStockProductsAsync called with threshold: {Threshold}",
            requestId,
            threshold
        );

        try
        {
            var products = new List<Product>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql =
                "SELECT ProductId, ProductName, Category, UnitPrice, StockQuantity, Supplier FROM Products WHERE StockQuantity <= @Threshold";
            LoggingHelper.LogDatabaseOperationStart(
                _logger,
                requestId,
                "GetLowStockProducts",
                sql,
                ("Threshold", threshold)
            );

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Threshold", threshold);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                products.Add(
                    new Product
                    {
                        ProductId = reader.GetInt32("ProductId"),
                        ProductName = reader.GetString("ProductName"),
                        Category = reader.GetString("Category"),
                        Price = reader.GetDecimal("UnitPrice"),
                        StockQuantity = reader.GetInt32("StockQuantity"),
                        Supplier = reader.GetString("Supplier")
                    }
                );
            }
            stopwatch.Stop();

            LoggingHelper.LogDatabaseOperationSuccess(
                _logger,
                requestId,
                "GetLowStockProducts",
                products.Count,
                stopwatch.ElapsedMilliseconds
            );
            return products;
        }
        catch (Exception ex)
        {
            LoggingHelper.LogDatabaseError(_logger, ex, requestId, "GetLowStockProductsAsync");
            return new List<Product>();
        }
    }

    public async Task<IEnumerable<CategorySales>> GetSalesByCategoryAsync(
        DateTime startDate,
        DateTime endDate
    )
    {
        var requestId = LoggingHelper.CreateRequestId();
        _logger.LogInformation(
            "[{RequestId}] GetSalesByCategoryAsync called with startDate: {StartDate}, endDate: {EndDate}",
            requestId,
            startDate.ToString("yyyy-MM-dd"),
            endDate.ToString("yyyy-MM-dd")
        );

        try
        {
            var categorySales = new List<CategorySales>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql =
                "SELECT p.Category, SUM(s.TotalAmount) as TotalSales, SUM(s.Quantity) as TotalQuantity FROM Sales s JOIN Products p ON s.ProductId = p.ProductId WHERE s.SaleDate >= @StartDate AND s.SaleDate <= @EndDate GROUP BY p.Category";
            LoggingHelper.LogDatabaseOperationStart(
                _logger,
                requestId,
                "GetSalesByCategory",
                sql,
                ("StartDate", startDate),
                ("EndDate", endDate)
            );

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                categorySales.Add(
                    new CategorySales
                    {
                        Category = reader.GetString("Category"),
                        TotalSales = reader.GetDecimal("TotalSales"),
                        TotalQuantity = reader.GetInt32("TotalQuantity")
                    }
                );
            }
            stopwatch.Stop();

            LoggingHelper.LogDatabaseOperationSuccess(
                _logger,
                requestId,
                "GetSalesByCategory",
                categorySales.Count,
                stopwatch.ElapsedMilliseconds
            );
            return categorySales;
        }
        catch (Exception ex)
        {
            LoggingHelper.LogDatabaseError(_logger, ex, requestId, "GetSalesByCategoryAsync");
            return new List<CategorySales>();
        }
    }

    // MCP Resource methods

    public async Task<IEnumerable<InventoryStatus>> GetInventoryStatusAsync()
    {
        var requestId = LoggingHelper.CreateRequestId();
        _logger.LogInformation("[{RequestId}] GetInventoryStatusAsync called", requestId);

        try
        {
            var inventoryStatuses = new List<InventoryStatus>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Use a view or direct query for real-time inventory status
            var sql = @"
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
                ORDER BY p.Category, p.ProductName";

            LoggingHelper.LogDatabaseOperationStart(_logger, requestId, "GetInventoryStatus", sql);
            var stopwatch = Stopwatch.StartNew();

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                inventoryStatuses.Add(new InventoryStatus
                {
                    ProductId = reader.GetInt32("ProductId"),
                    ProductName = reader.GetString("ProductName"),
                    Category = reader.GetString("Category"),
                    StockQuantity = reader.GetInt32("StockQuantity"),
                    ReorderLevel = reader.GetInt32("ReorderLevel"),
                    UnitPrice = reader.GetDecimal("UnitPrice"),
                    StockStatus = reader.GetString("StockStatus"),
                    RecentSales = reader.GetInt32("RecentSales"),
                    LastUpdated = reader.GetDateTime("LastUpdated")
                });
            }
            stopwatch.Stop();

            LoggingHelper.LogDatabaseOperationSuccess(_logger, requestId, "GetInventoryStatus", inventoryStatuses.Count, stopwatch.ElapsedMilliseconds);
            return inventoryStatuses;
        }
        catch (Exception ex)
        {
            LoggingHelper.LogDatabaseError(_logger, ex, requestId, "GetInventoryStatusAsync");
            return new List<InventoryStatus>();
        }
    }

    public async Task<IEnumerable<DailySummary>> GetDailySummaryAsync(DateTime? date = null)
    {
        var requestId = LoggingHelper.CreateRequestId();
        var targetDate = date ?? DateTime.Today;
        _logger.LogInformation("[{RequestId}] GetDailySummaryAsync called for date: {Date}", requestId, targetDate.ToString("yyyy-MM-dd"));

        try
        {
            var dailySummaries = new List<DailySummary>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
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
                    WHERE (@Date IS NULL OR CAST(s.SaleDate AS DATE) = @Date)
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
                ORDER BY ds.SaleDate DESC";

            LoggingHelper.LogDatabaseOperationStart(_logger, requestId, "GetDailySummary", sql);
            var stopwatch = Stopwatch.StartNew();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Date", date.HasValue ? (object)date.Value.Date : DBNull.Value);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                dailySummaries.Add(new DailySummary
                {
                    Date = reader.GetDateTime("Date"),
                    TotalTransactions = reader.GetInt32("TotalTransactions"),
                    TotalRevenue = reader.GetDecimal("TotalRevenue"),
                    UniqueProducts = reader.GetInt32("UniqueProducts"),
                    TotalItemsSold = reader.GetInt32("TotalItemsSold"),
                    AverageTransactionValue = reader.GetDecimal("AverageTransactionValue"),
                    TopCategory = reader.GetString("TopCategory"),
                    TopCategoryRevenue = reader.GetDecimal("TopCategoryRevenue")
                });
            }
            stopwatch.Stop();

            LoggingHelper.LogDatabaseOperationSuccess(_logger, requestId, "GetDailySummary", dailySummaries.Count, stopwatch.ElapsedMilliseconds);
            return dailySummaries;
        }
        catch (Exception ex)
        {
            LoggingHelper.LogDatabaseError(_logger, ex, requestId, "GetDailySummaryAsync");
            return new List<DailySummary>();
        }
    }

    public async Task<IEnumerable<Product>> GetDetailedInventoryAsync()
    {
        var requestId = LoggingHelper.CreateRequestId();
        _logger.LogInformation("[{RequestId}] GetDetailedInventoryAsync called", requestId);

        try
        {
            // For detailed inventory, we can reuse GetProductsAsync but could add more fields
            return await GetProductsAsync();
        }
        catch (Exception ex)
        {
            LoggingHelper.LogDatabaseError(_logger, ex, requestId, "GetDetailedInventoryAsync");
            return new List<Product>();
        }
    }

    // =============================================================================
    // Phase 3: Predictive Analytics Methods
    // =============================================================================

    public async Task<IEnumerable<DemandForecast>> PredictDemandAsync(int daysAhead = 7)
    {
        var requestId = LoggingHelper.CreateRequestId();
        _logger.LogInformation("[{RequestId}] PredictDemandAsync called with daysAhead: {DaysAhead}", requestId, daysAhead);

        try
        {
            var forecasts = new List<DemandForecast>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // First, calculate and insert demand forecasts using the stored procedure
            var calculateSql = "EXEC analytics.CalculateDemandForecast @DaysAhead = @DaysAhead";
            LoggingHelper.LogDatabaseOperationStart(_logger, requestId, "CalculateDemandForecast", calculateSql, ("DaysAhead", daysAhead));

            using (var calculateCommand = new SqlCommand(calculateSql, connection))
            {
                calculateCommand.Parameters.AddWithValue("@DaysAhead", daysAhead);
                await calculateCommand.ExecuteScalarAsync();
            }

            // Now retrieve the forecasts with product details
            var sql = @"
                SELECT 
                    df.ProductId,
                    p.ProductName,
                    p.Category,
                    df.ForecastDate,
                    df.PredictedDemand,
                    df.ConfidenceLevel,
                    df.TrendDirection,
                    p.StockQuantity as CurrentStock,
                    CASE 
                        WHEN df.PredictedDemand > 0 THEN p.StockQuantity + (df.PredictedDemand * 1.5)
                        ELSE p.ReorderLevel * 2
                    END as RecommendedStock
                FROM analytics.DemandForecasts df
                JOIN dbo.Products p ON df.ProductId = p.ProductId
                WHERE df.ForecastDate = CAST(GETDATE() AS DATE)
                ORDER BY df.ConfidenceLevel DESC, p.Category, p.ProductName";

            LoggingHelper.LogDatabaseOperationStart(_logger, requestId, "PredictDemand", sql);
            var stopwatch = Stopwatch.StartNew();

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                forecasts.Add(new DemandForecast
                {
                    ProductId = reader.GetInt32("ProductId"),
                    ProductName = reader.GetString("ProductName"),
                    Category = reader.GetString("Category"),
                    ForecastDate = reader.GetDateTime("ForecastDate"),
                    PredictedDemand = reader.GetDecimal("PredictedDemand"),
                    ConfidenceLevel = reader.GetDecimal("ConfidenceLevel"),
                    TrendDirection = reader.GetString("TrendDirection"),
                    CurrentStock = reader.GetDecimal("CurrentStock"),
                    RecommendedStock = reader.GetDecimal("RecommendedStock")
                });
            }
            stopwatch.Stop();

            LoggingHelper.LogDatabaseOperationSuccess(_logger, requestId, "PredictDemand", forecasts.Count, stopwatch.ElapsedMilliseconds);
            return forecasts;
        }
        catch (Exception ex)
        {
            LoggingHelper.LogDatabaseError(_logger, ex, requestId, "PredictDemandAsync");
            return new List<DemandForecast>();
        }
    }

    public async Task<DemandForecast?> PredictProductDemandAsync(int productId, int daysAhead = 7)
    {
        var requestId = LoggingHelper.CreateRequestId();
        _logger.LogInformation("[{RequestId}] PredictProductDemandAsync called for ProductId: {ProductId}, daysAhead: {DaysAhead}", requestId, productId, daysAhead);

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Calculate forecast for specific product
            var calculateSql = "EXEC analytics.CalculateDemandForecast @ProductId = @ProductId, @DaysAhead = @DaysAhead";
            using (var calculateCommand = new SqlCommand(calculateSql, connection))
            {
                calculateCommand.Parameters.AddWithValue("@ProductId", productId);
                calculateCommand.Parameters.AddWithValue("@DaysAhead", daysAhead);
                await calculateCommand.ExecuteScalarAsync();
            }

            // Retrieve the specific forecast
            var sql = @"
                SELECT 
                    df.ProductId,
                    p.ProductName,
                    p.Category,
                    df.ForecastDate,
                    df.PredictedDemand,
                    df.ConfidenceLevel,
                    df.TrendDirection,
                    p.StockQuantity as CurrentStock,
                    CASE 
                        WHEN df.PredictedDemand > 0 THEN p.StockQuantity + (df.PredictedDemand * 1.5)
                        ELSE p.ReorderLevel * 2
                    END as RecommendedStock
                FROM analytics.DemandForecasts df
                JOIN dbo.Products p ON df.ProductId = p.ProductId
                WHERE df.ProductId = @ProductId AND df.ForecastDate = CAST(GETDATE() AS DATE)";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ProductId", productId);
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new DemandForecast
                {
                    ProductId = reader.GetInt32("ProductId"),
                    ProductName = reader.GetString("ProductName"),
                    Category = reader.GetString("Category"),
                    ForecastDate = reader.GetDateTime("ForecastDate"),
                    PredictedDemand = reader.GetDecimal("PredictedDemand"),
                    ConfidenceLevel = reader.GetDecimal("ConfidenceLevel"),
                    TrendDirection = reader.GetString("TrendDirection"),
                    CurrentStock = reader.GetDecimal("CurrentStock"),
                    RecommendedStock = reader.GetDecimal("RecommendedStock")
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            LoggingHelper.LogDatabaseError(_logger, ex, requestId, "PredictProductDemandAsync");
            return null;
        }
    }

    public async Task<IEnumerable<StockoutRisk>> GetStockoutRisksAsync(int daysAhead = 14)
    {
        var requestId = LoggingHelper.CreateRequestId();
        _logger.LogInformation("[{RequestId}] GetStockoutRisksAsync called with daysAhead: {DaysAhead}", requestId, daysAhead);

        try
        {
            var risks = new List<StockoutRisk>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // First analyze stockout risks using the stored procedure
            var analyzeSql = "EXEC analytics.AnalyzeStockoutRisks @DaysAhead = @DaysAhead";
            using (var analyzeCommand = new SqlCommand(analyzeSql, connection))
            {
                analyzeCommand.Parameters.AddWithValue("@DaysAhead", daysAhead);
                await analyzeCommand.ExecuteScalarAsync();
            }

            // Retrieve stockout risks with product details
            var sql = @"
                SELECT 
                    sr.ProductId,
                    p.ProductName,
                    p.Category,
                    sr.CurrentStock,
                    sr.PredictedDemand,
                    sr.DaysOfStock,
                    sr.RiskLevel,
                    sr.RiskScore,
                    sr.RecommendedAction,
                    sr.EstimatedStockoutDate,
                    p.UnitPrice * sr.PredictedDemand as PotentialLostRevenue
                FROM analytics.StockoutRisks sr
                JOIN dbo.Products p ON sr.ProductId = p.ProductId
                WHERE sr.AnalysisDate = CAST(GETDATE() AS DATE)
                ORDER BY sr.RiskScore DESC, p.Category, p.ProductName";

            LoggingHelper.LogDatabaseOperationStart(_logger, requestId, "GetStockoutRisks", sql);
            var stopwatch = Stopwatch.StartNew();

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                risks.Add(new StockoutRisk
                {
                    ProductId = reader.GetInt32("ProductId"),
                    ProductName = reader.GetString("ProductName"),
                    Category = reader.GetString("Category"),
                    CurrentStock = reader.GetInt32("CurrentStock"),
                    PredictedDemand = reader.GetDecimal("PredictedDemand"),
                    DaysOfStock = reader.GetDecimal("DaysOfStock"),
                    RiskLevel = reader.GetString("RiskLevel"),
                    RiskScore = reader.GetDecimal("RiskScore"),
                    RecommendedAction = reader.GetString("RecommendedAction"),
                    EstimatedStockoutDate = reader.IsDBNull("EstimatedStockoutDate") ? null : reader.GetDateTime("EstimatedStockoutDate"),
                    PotentialLostRevenue = reader.GetDecimal("PotentialLostRevenue")
                });
            }
            stopwatch.Stop();

            LoggingHelper.LogDatabaseOperationSuccess(_logger, requestId, "GetStockoutRisks", risks.Count, stopwatch.ElapsedMilliseconds);
            return risks;
        }
        catch (Exception ex)
        {
            LoggingHelper.LogDatabaseError(_logger, ex, requestId, "GetStockoutRisksAsync");
            return new List<StockoutRisk>();
        }
    }

    public async Task<IEnumerable<StockoutRisk>> GetCriticalStockoutRisksAsync()
    {
        var allRisks = await GetStockoutRisksAsync(7); // 7 days ahead for critical analysis
        return allRisks.Where(r => r.RiskLevel == "HIGH" || r.RiskScore >= 70);
    }

    public async Task<IEnumerable<SeasonalPattern>> GetSeasonalTrendsAsync(string? category = null)
    {
        var requestId = LoggingHelper.CreateRequestId();
        _logger.LogInformation("[{RequestId}] GetSeasonalTrendsAsync called with category: {Category}", requestId, category ?? "All");

        try
        {
            var patterns = new List<SeasonalPattern>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT 
                    Category,
                    Month,
                    MonthName,
                    SeasonalityFactor,
                    AverageMonthlySales,
                    AverageMonthlySales * SeasonalityFactor as ExpectedSales,
                    TrendClassification as Trend
                FROM analytics.SeasonalPatterns
                WHERE (@Category IS NULL OR Category = @Category)
                ORDER BY Category, Month";

            LoggingHelper.LogDatabaseOperationStart(_logger, requestId, "GetSeasonalTrends", sql, ("Category", category ?? "All"));
            var stopwatch = Stopwatch.StartNew();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Category", category ?? (object)DBNull.Value);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                patterns.Add(new SeasonalPattern
                {
                    Category = reader.GetString("Category"),
                    Month = reader.GetByte("Month"),
                    MonthName = reader.GetString("MonthName"),
                    SeasonalityFactor = reader.GetDecimal("SeasonalityFactor"),
                    AverageMonthlySales = reader.GetDecimal("AverageMonthlySales"),
                    ExpectedSales = reader.GetDecimal("ExpectedSales"),
                    Trend = reader.GetString("Trend")
                });
            }
            stopwatch.Stop();

            LoggingHelper.LogDatabaseOperationSuccess(_logger, requestId, "GetSeasonalTrends", patterns.Count, stopwatch.ElapsedMilliseconds);
            return patterns;
        }
        catch (Exception ex)
        {
            LoggingHelper.LogDatabaseError(_logger, ex, requestId, "GetSeasonalTrendsAsync");
            return new List<SeasonalPattern>();
        }
    }

    public async Task<IEnumerable<SeasonalPattern>> GetSeasonalForecastAsync(int monthsAhead = 3)
    {
        var currentMonth = DateTime.Now.Month;
        var patterns = await GetSeasonalTrendsAsync();

        return patterns.Where(p =>
        {
            for (int i = 1; i <= monthsAhead; i++)
            {
                var targetMonth = ((currentMonth + i - 1) % 12) + 1;
                if (p.Month == targetMonth) return true;
            }
            return false;
        });
    }

    public async Task<IEnumerable<ReorderRecommendation>> GetReorderRecommendationsAsync()
    {
        var requestId = LoggingHelper.CreateRequestId();
        _logger.LogInformation("[{RequestId}] GetReorderRecommendationsAsync called", requestId);

        try
        {
            var recommendations = new List<ReorderRecommendation>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                WITH ReorderAnalysis AS (
                    SELECT 
                        p.ProductId,
                        p.ProductName,
                        p.Category,
                        p.StockQuantity,
                        p.ReorderLevel,
                        COALESCE(df.PredictedDemand, 0) as PredictedDemand,
                        COALESCE(sr.RiskScore, 0) as RiskScore,
                        COALESCE(sr.DaysOfStock, 999) as DaysOfStock,
                        CASE 
                            WHEN p.StockQuantity <= p.ReorderLevel THEN 'IMMEDIATE'
                            WHEN COALESCE(sr.DaysOfStock, 999) <= 7 THEN 'URGENT'
                            WHEN COALESCE(sr.DaysOfStock, 999) <= 14 THEN 'SCHEDULED'
                            ELSE 'MONITOR'
                        END as Priority,
                        CASE 
                            WHEN COALESCE(df.PredictedDemand, 0) > 0 THEN 
                                CEILING((df.PredictedDemand * 2) - p.StockQuantity)
                            ELSE p.ReorderLevel * 2
                        END as RecommendedQuantity
                    FROM dbo.Products p
                    LEFT JOIN analytics.DemandForecasts df ON p.ProductId = df.ProductId 
                        AND df.ForecastDate = CAST(GETDATE() AS DATE)
                    LEFT JOIN analytics.StockoutRisks sr ON p.ProductId = sr.ProductId 
                        AND sr.AnalysisDate = CAST(GETDATE() AS DATE)
                )
                SELECT 
                    ProductId,
                    ProductName,
                    Category,
                    StockQuantity as CurrentStock,
                    RecommendedQuantity,
                    Priority,
                    DaysOfStock,
                    RiskScore,
                    CASE Priority
                        WHEN 'IMMEDIATE' THEN 'Stock below reorder level - order now'
                        WHEN 'URGENT' THEN 'Will stock out within 7 days'
                        WHEN 'SCHEDULED' THEN 'Schedule reorder within 2 weeks'
                        ELSE 'Monitor inventory levels'
                    END as Reason
                FROM ReorderAnalysis
                WHERE Priority IN ('IMMEDIATE', 'URGENT', 'SCHEDULED')
                ORDER BY 
                    CASE Priority 
                        WHEN 'IMMEDIATE' THEN 1 
                        WHEN 'URGENT' THEN 2 
                        WHEN 'SCHEDULED' THEN 3 
                        ELSE 4 
                    END,
                    RiskScore DESC";

            LoggingHelper.LogDatabaseOperationStart(_logger, requestId, "GetReorderRecommendations", sql);
            var stopwatch = Stopwatch.StartNew();

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                recommendations.Add(new ReorderRecommendation
                {
                    ProductId = reader.GetInt32("ProductId"),
                    ProductName = reader.GetString("ProductName"),
                    Category = reader.GetString("Category"),
                    CurrentStock = reader.GetInt32("CurrentStock"),
                    RecommendedQuantity = reader.GetInt32("RecommendedQuantity"),
                    Priority = reader.GetString("Priority"),
                    DaysUntilStockout = reader.GetDecimal("DaysOfStock"),
                    RiskScore = reader.GetDecimal("RiskScore"),
                    Reason = reader.GetString("Reason")
                });
            }
            stopwatch.Stop();

            LoggingHelper.LogDatabaseOperationSuccess(_logger, requestId, "GetReorderRecommendations", recommendations.Count, stopwatch.ElapsedMilliseconds);
            return recommendations;
        }
        catch (Exception ex)
        {
            LoggingHelper.LogDatabaseError(_logger, ex, requestId, "GetReorderRecommendationsAsync");
            return new List<ReorderRecommendation>();
        }
    }

    public async Task<IEnumerable<ReorderRecommendation>> GetUrgentReorderRecommendationsAsync()
    {
        var allRecommendations = await GetReorderRecommendationsAsync();
        return allRecommendations.Where(r => r.Priority == "IMMEDIATE" || r.Priority == "URGENT");
    }

    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category)
    {
        var requestId = LoggingHelper.CreateRequestId();
        _logger.LogInformation("[{RequestId}] GetProductsByCategoryAsync called with category: {Category}", requestId, category);

        try
        {
            var products = new List<Product>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT ProductId, ProductName, Category, UnitPrice, StockQuantity, Supplier FROM Products WHERE Category = @Category";
            LoggingHelper.LogDatabaseOperationStart(_logger, requestId, "GetProductsByCategory", sql);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Category", category);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                products.Add(
                    new Product
                    {
                        ProductId = reader.GetInt32("ProductId"),
                        ProductName = reader.GetString("ProductName"),
                        Category = reader.GetString("Category"),
                        Price = reader.GetDecimal("UnitPrice"),
                        StockQuantity = reader.GetInt32("StockQuantity"),
                        Supplier = reader.GetString("Supplier")
                    }
                );
            }
            stopwatch.Stop();

            LoggingHelper.LogDatabaseOperationSuccess(
                _logger,
                requestId,
                "GetProductsByCategory",
                products.Count,
                stopwatch.ElapsedMilliseconds
            );
            return products;
        }
        catch (Exception ex)
        {
            LoggingHelper.LogDatabaseError(_logger, ex, requestId, "GetProductsByCategoryAsync");
            return new List<Product>();
        }
    }
}
