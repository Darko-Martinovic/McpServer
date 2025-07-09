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
                "SELECT ProductId, ProductName, Category, Price, StockQuantity, Supplier FROM Products";
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
                        Price = reader.GetDecimal("Price"),
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
                "SELECT ProductId, ProductName, Category, Price, StockQuantity, Supplier FROM Products WHERE StockQuantity <= @Threshold";
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
                        Price = reader.GetDecimal("Price"),
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
}
