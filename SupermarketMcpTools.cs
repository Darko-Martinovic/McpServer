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

    // === RESOURCE-LIKE TOOLS (Real-time Data) ===

    [McpServerTool, Description("Get real-time inventory status with stock levels and recent sales data")]
    public static async Task<string> GetInventoryStatus(ISupermarketDataService dataService)
    {
        try
        {
            Serilog.Log.Information("MCP Tool 'GetInventoryStatus' called at {Timestamp}", DateTime.Now);

            var inventoryStatus = await dataService.GetInventoryStatusAsync();
            var result = JsonSerializer.Serialize(inventoryStatus, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            Serilog.Log.Information("MCP Tool 'GetInventoryStatus' completed successfully. Returned {Count} products", inventoryStatus.Count());
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'GetInventoryStatus' failed: {Message}", ex.Message);
            return $"Error retrieving inventory status: {ex.Message}";
        }
    }

    [McpServerTool, Description("Get daily sales summary with transactions and revenue data (today by default)")]
    public static async Task<string> GetDailySummary(
        ISupermarketDataService dataService,
        [Description("Specific date in YYYY-MM-DD format (optional, defaults to today)")] string? date = null
    )
    {
        try
        {
            Serilog.Log.Information("MCP Tool 'GetDailySummary' called with date: {Date}", date ?? "today");

            DateTime? targetDate = null;
            if (!string.IsNullOrEmpty(date))
            {
                if (!DateTime.TryParse(date, out var parsedDate))
                {
                    Serilog.Log.Warning("MCP Tool 'GetDailySummary' validation failed: Invalid date format");
                    return "Invalid date format. Please use YYYY-MM-DD format.";
                }
                targetDate = parsedDate;
            }

            var dailySummary = await dataService.GetDailySummaryAsync(targetDate);
            var result = JsonSerializer.Serialize(dailySummary, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            Serilog.Log.Information("MCP Tool 'GetDailySummary' completed successfully. Returned {Count} summaries", dailySummary.Count());
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'GetDailySummary' failed: {Message}", ex.Message);
            return $"Error retrieving daily summary: {ex.Message}";
        }
    }

    [McpServerTool, Description("Get detailed inventory information for all products")]
    public static async Task<string> GetDetailedInventory(ISupermarketDataService dataService)
    {
        try
        {
            Serilog.Log.Information("MCP Tool 'GetDetailedInventory' called at {Timestamp}", DateTime.Now);

            var detailedInventory = await dataService.GetDetailedInventoryAsync();
            var result = JsonSerializer.Serialize(detailedInventory, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            Serilog.Log.Information("MCP Tool 'GetDetailedInventory' completed successfully. Returned {Count} products", detailedInventory.Count());
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'GetDetailedInventory' failed: {Message}", ex.Message);
            return $"Error retrieving detailed inventory: {ex.Message}";
        }
    }

    // =============================================================================
    // Phase 3: Predictive Analytics Tools
    // =============================================================================

    [McpServerTool, Description("Predict product demand for upcoming days with confidence levels and trend analysis")]
    public static async Task<string> PredictDemand(
        ISupermarketDataService dataService,
        [Description("Number of days to forecast ahead (default: 7)")] int daysAhead = 7
    )
    {
        try
        {
            Serilog.Log.Information("MCP Tool 'PredictDemand' called with daysAhead: {DaysAhead}", daysAhead);

            if (daysAhead < 1 || daysAhead > 30)
            {
                Serilog.Log.Warning("MCP Tool 'PredictDemand' validation failed: Invalid daysAhead value");
                return "Invalid daysAhead value. Please use a value between 1 and 30.";
            }

            var forecasts = await dataService.PredictDemandAsync(daysAhead);
            var result = JsonSerializer.Serialize(forecasts, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            Serilog.Log.Information("MCP Tool 'PredictDemand' completed successfully. Returned {Count} forecasts", forecasts.Count());
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'PredictDemand' failed: {Message}", ex.Message);
            return $"Error predicting demand: {ex.Message}";
        }
    }

    [McpServerTool, Description("Identify products at risk of stockout with risk levels and recommended actions")]
    public static async Task<string> GetStockoutRisks(
        ISupermarketDataService dataService,
        [Description("Number of days to analyze ahead (default: 14)")] int daysAhead = 14
    )
    {
        try
        {
            Serilog.Log.Information("MCP Tool 'GetStockoutRisks' called with daysAhead: {DaysAhead}", daysAhead);

            if (daysAhead < 1 || daysAhead > 60)
            {
                Serilog.Log.Warning("MCP Tool 'GetStockoutRisks' validation failed: Invalid daysAhead value");
                return "Invalid daysAhead value. Please use a value between 1 and 60.";
            }

            var risks = await dataService.GetStockoutRisksAsync(daysAhead);
            var result = JsonSerializer.Serialize(risks, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            Serilog.Log.Information("MCP Tool 'GetStockoutRisks' completed successfully. Returned {Count} risk assessments", risks.Count());
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'GetStockoutRisks' failed: {Message}", ex.Message);
            return $"Error analyzing stockout risks: {ex.Message}";
        }
    }

    [McpServerTool, Description("Analyze seasonal sales trends and patterns by category with monthly forecasts")]
    public static async Task<string> GetSeasonalTrends(
        ISupermarketDataService dataService,
        [Description("Specific category to analyze (optional - analyzes all categories if not specified)")] string? category = null
    )
    {
        try
        {
            Serilog.Log.Information("MCP Tool 'GetSeasonalTrends' called with category: {Category}", category ?? "All");

            var trends = await dataService.GetSeasonalTrendsAsync(category);
            var result = JsonSerializer.Serialize(trends, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            Serilog.Log.Information("MCP Tool 'GetSeasonalTrends' completed successfully. Returned {Count} seasonal patterns", trends.Count());
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'GetSeasonalTrends' failed: {Message}", ex.Message);
            return $"Error analyzing seasonal trends: {ex.Message}";
        }
    }

    [McpServerTool, Description("Get intelligent reorder recommendations based on demand prediction and risk analysis")]
    public static async Task<string> GetReorderRecommendations(
        ISupermarketDataService dataService
    )
    {
        try
        {
            Serilog.Log.Information("MCP Tool 'GetReorderRecommendations' called");

            var recommendations = await dataService.GetReorderRecommendationsAsync();
            var result = JsonSerializer.Serialize(recommendations, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            Serilog.Log.Information("MCP Tool 'GetReorderRecommendations' completed successfully. Returned {Count} recommendations", recommendations.Count());
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'GetReorderRecommendations' failed: {Message}", ex.Message);
            return $"Error generating reorder recommendations: {ex.Message}";
        }
    }

    [McpServerTool, Description("Get critical items requiring immediate attention with high-priority alerts")]
    public static async Task<string> GetCriticalAlerts(
        ISupermarketDataService dataService
    )
    {
        try
        {
            Serilog.Log.Information("MCP Tool 'GetCriticalAlerts' called");

            // Combine critical stockout risks and urgent reorder recommendations
            var criticalRisks = await dataService.GetCriticalStockoutRisksAsync();
            var urgentRecommendations = await dataService.GetUrgentReorderRecommendationsAsync();

            var alerts = new
            {
                CriticalStockoutRisks = criticalRisks,
                UrgentReorderRecommendations = urgentRecommendations,
                AlertCount = criticalRisks.Count() + urgentRecommendations.Count(),
                GeneratedAt = DateTime.Now
            };

            var result = JsonSerializer.Serialize(alerts, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            Serilog.Log.Information("MCP Tool 'GetCriticalAlerts' completed successfully. Returned {CriticalRisks} critical risks and {UrgentRecommendations} urgent recommendations",
                criticalRisks.Count(), urgentRecommendations.Count());
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'GetCriticalAlerts' failed: {Message}", ex.Message);
            return $"Error generating critical alerts: {ex.Message}";
        }
    }
}
