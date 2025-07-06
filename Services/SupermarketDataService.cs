using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Data;
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
}
