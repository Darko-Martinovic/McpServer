using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using McpServer.Services.Interfaces;

[McpServerToolType]
public static class SupermarketMcpTools
{
    [McpServerTool, Description("Get all products in the supermarket inventory")]
    public static async Task<string> GetProducts(ISupermarketDataService dataService)
    {
        try
        {
            // Log to Serilog
            Serilog.Log.Information("MCP Tool 'GetProducts' called at {Timestamp}", DateTime.Now);

            // Also write to a simple debug file to verify tool execution
            await File.AppendAllTextAsync(
                "debug-tool-calls.txt",
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: GetProducts called\n"
            );

            var products = await dataService.GetProductsAsync();
            var result = JsonSerializer.Serialize(
                products,
                new JsonSerializerOptions { WriteIndented = true }
            );

            Serilog.Log.Information(
                "MCP Tool 'GetProducts' completed successfully. Returned {Count} products",
                products.Count()
            );
            await File.AppendAllTextAsync(
                "debug-tool-calls.txt",
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: GetProducts completed with {products.Count()} products\n"
            );

            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'GetProducts' failed: {Message}", ex.Message);
            await File.AppendAllTextAsync(
                "debug-tool-calls.txt",
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: GetProducts failed: {ex.Message}\n"
            );
            return $"Error retrieving products: {ex.Message}";
        }
    }

    [McpServerTool, Description("Get sales data for a specific date range")]
    public static async Task<string> GetSalesData(
        ISupermarketDataService dataService,
        [Description("Start date in YYYY-MM-DD format")] string startDate,
        [Description("End date in YYYY-MM-DD format")] string endDate
    )
    {
        try
        {
            Serilog.Log.Information(
                "MCP Tool 'GetSalesData' called with startDate: {StartDate}, endDate: {EndDate}",
                startDate,
                endDate
            );

            if (
                !DateTime.TryParse(startDate, out var start)
                || !DateTime.TryParse(endDate, out var end)
            )
            {
                Serilog.Log.Warning(
                    "MCP Tool 'GetSalesData' validation failed: Invalid date format"
                );
                return "Invalid date format. Please use YYYY-MM-DD format.";
            }

            var salesData = await dataService.GetSalesDataAsync(start, end);
            var result = JsonSerializer.Serialize(
                salesData,
                new JsonSerializerOptions { WriteIndented = true }
            );
            Serilog.Log.Information(
                "MCP Tool 'GetSalesData' completed successfully. Returned {Count} records",
                salesData.Count()
            );
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'GetSalesData' failed: {Message}", ex.Message);
            return $"Error retrieving sales data: {ex.Message}";
        }
    }

    [McpServerTool, Description("Get total revenue for a specific date range")]
    public static async Task<string> GetTotalRevenue(
        ISupermarketDataService dataService,
        [Description("Start date in YYYY-MM-DD format")] string startDate,
        [Description("End date in YYYY-MM-DD format")] string endDate
    )
    {
        try
        {
            Serilog.Log.Information(
                "MCP Tool 'GetTotalRevenue' called with startDate: {StartDate}, endDate: {EndDate}",
                startDate,
                endDate
            );

            if (
                !DateTime.TryParse(startDate, out var start)
                || !DateTime.TryParse(endDate, out var end)
            )
            {
                Serilog.Log.Warning(
                    "MCP Tool 'GetTotalRevenue' validation failed: Invalid date format"
                );
                return "Invalid date format. Please use YYYY-MM-DD format.";
            }

            var revenue = await dataService.GetTotalRevenueAsync(start, end);
            var result = $"Total revenue from {startDate} to {endDate}: ${revenue:N2}";
            Serilog.Log.Information(
                "MCP Tool 'GetTotalRevenue' completed successfully. Revenue: ${Revenue:N2}",
                revenue
            );
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'GetTotalRevenue' failed: {Message}", ex.Message);
            return $"Error calculating revenue: {ex.Message}";
        }
    }

    [McpServerTool, Description("Get products with low stock levels")]
    public static async Task<string> GetLowStockProducts(
        ISupermarketDataService dataService,
        [Description("Stock threshold level")] int threshold = 10
    )
    {
        try
        {
            Serilog.Log.Information(
                "MCP Tool 'GetLowStockProducts' called with threshold: {Threshold}",
                threshold
            );

            var lowStockProducts = await dataService.GetLowStockProductsAsync(threshold);
            var result = JsonSerializer.Serialize(
                lowStockProducts,
                new JsonSerializerOptions { WriteIndented = true }
            );
            Serilog.Log.Information(
                "MCP Tool 'GetLowStockProducts' completed successfully. Returned {Count} products",
                lowStockProducts.Count()
            );
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'GetLowStockProducts' failed: {Message}", ex.Message);
            return $"Error retrieving low stock products: {ex.Message}";
        }
    }

    [McpServerTool, Description("Get sales performance by product category for a date range")]
    public static async Task<string> GetSalesByCategory(
        ISupermarketDataService dataService,
        [Description("Start date in YYYY-MM-DD format")] string startDate,
        [Description("End date in YYYY-MM-DD format")] string endDate
    )
    {
        try
        {
            Serilog.Log.Information(
                "MCP Tool 'GetSalesByCategory' called with startDate: {StartDate}, endDate: {EndDate}",
                startDate,
                endDate
            );

            if (
                !DateTime.TryParse(startDate, out var start)
                || !DateTime.TryParse(endDate, out var end)
            )
            {
                Serilog.Log.Warning(
                    "MCP Tool 'GetSalesByCategory' validation failed: Invalid date format"
                );
                return "Invalid date format. Please use YYYY-MM-DD format.";
            }

            var categorySales = await dataService.GetSalesByCategoryAsync(start, end);
            var result = JsonSerializer.Serialize(
                categorySales,
                new JsonSerializerOptions { WriteIndented = true }
            );
            Serilog.Log.Information(
                "MCP Tool 'GetSalesByCategory' completed successfully. Returned {Count} categories",
                categorySales.Count()
            );
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'GetSalesByCategory' failed: {Message}", ex.Message);
            return $"Error retrieving category sales: {ex.Message}";
        }
    }
}
