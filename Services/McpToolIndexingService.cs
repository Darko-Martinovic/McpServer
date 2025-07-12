using McpServer.Models;
using McpServer.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

namespace McpServer.Services;

public interface IMcpToolIndexingService
{
    Task IndexToolsAsync();
    IEnumerable<McpToolDocument> ExtractToolDocuments();
}

public class McpToolIndexingService : IMcpToolIndexingService
{
    private readonly IAzureSearchService _azureSearchService;
    private readonly ILogger<McpToolIndexingService> _logger;

    public McpToolIndexingService(
        IAzureSearchService azureSearchService,
        ILogger<McpToolIndexingService> logger)
    {
        _azureSearchService = azureSearchService;
        _logger = logger;
    }

    public async Task IndexToolsAsync()
    {
        try
        {
            _logger.LogInformation("Starting MCP tools indexing process");

            // Initialize the search index
            await _azureSearchService.InitializeIndexAsync();

            // Extract tool documents
            var toolDocuments = ExtractToolDocuments().ToList();

            if (!toolDocuments.Any())
            {
                _logger.LogWarning("No MCP tools found to index");
                return;
            }

            _logger.LogInformation("Found {Count} MCP tools to index", toolDocuments.Count);

            // Check if documents already exist
            var existingDocuments = await _azureSearchService.GetAllDocumentsAsync();

            if (existingDocuments.Any())
            {
                _logger.LogInformation("Found {Count} existing documents in search index", existingDocuments.Count());

                // Simple approach: if any documents exist, assume they need to be refreshed
                // Delete all existing documents
                await _azureSearchService.DeleteAllDocumentsAsync();
                _logger.LogInformation("Deleted existing documents for refresh");
            }

            // Upload new documents
            await _azureSearchService.UploadToolDocumentsAsync(toolDocuments);

            _logger.LogInformation("Successfully indexed {Count} MCP tools to Azure Search", toolDocuments.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index MCP tools");
            throw;
        }
    }

    public IEnumerable<McpToolDocument> ExtractToolDocuments()
    {
        var documents = new List<McpToolDocument>();
        int idCounter = 1;

        // Define the MCP tools with their metadata
        var toolDefinitions = new[]
        {
            new
            {
                FunctionName = "GetProducts",
                Description = "Retrieve all products from the supermarket inventory with optional filtering by category, name, or stock status. Returns product details including ID, name, category, price, and stock quantity.",
                Endpoint = "/api/supermarket/products",
                HttpMethod = "GET",
                Parameters = "category (optional), name (optional), minStock (optional)",
                ResponseType = "Array of Product objects with id, name, category, price, stockQuantity"
            },
            new
            {
                FunctionName = "GetSalesData",
                Description = "Retrieve sales transaction data within a specified date range. Useful for analyzing sales patterns, revenue trends, and transaction volumes.",
                Endpoint = "/api/supermarket/sales",
                HttpMethod = "GET",
                Parameters = "startDate (optional), endDate (optional)",
                ResponseType = "Array of SalesRecord objects with transactionId, productId, productName, quantity, unitPrice, totalAmount, saleDate"
            },
            new
            {
                FunctionName = "GetTotalRevenue",
                Description = "Calculate and retrieve the total revenue generated within a specified date range. Essential for financial reporting and business performance analysis.",
                Endpoint = "/api/supermarket/revenue",
                HttpMethod = "GET",
                Parameters = "startDate (optional), endDate (optional)",
                ResponseType = "Object with totalRevenue, period, transactionCount"
            },
            new
            {
                FunctionName = "GetLowStockProducts",
                Description = "Identify products with inventory levels below a specified threshold. Critical for inventory management and preventing stockouts.",
                Endpoint = "/api/supermarket/products/low-stock",
                HttpMethod = "GET",
                Parameters = "threshold (optional, default: 10)",
                ResponseType = "Array of Product objects with low stock levels"
            },
            new
            {
                FunctionName = "GetSalesByCategory",
                Description = "Analyze sales performance grouped by product categories within a date range. Valuable for category management and merchandising decisions.",
                Endpoint = "/api/supermarket/sales/by-category",
                HttpMethod = "GET",
                Parameters = "startDate (optional), endDate (optional)",
                ResponseType = "Array of CategorySales objects with categoryName, totalSales, totalRevenue, averagePrice"
            },
            new
            {
                FunctionName = "GetInventoryStatus",
                Description = "Get a comprehensive overview of current inventory status including total products, categories, stock levels, and value metrics.",
                Endpoint = "/api/supermarket/inventory/status",
                HttpMethod = "GET",
                Parameters = "None",
                ResponseType = "Object with totalProducts, totalCategories, lowStockCount, totalInventoryValue, averageStockLevel"
            },
            new
            {
                FunctionName = "GetDailySummary",
                Description = "Generate a comprehensive daily business summary including sales, revenue, top products, and key performance indicators for a specific date.",
                Endpoint = "/api/supermarket/sales/daily-summary",
                HttpMethod = "GET",
                Parameters = "date (optional, defaults to today)",
                ResponseType = "Object with date, totalSales, totalRevenue, transactionCount, topSellingProducts, averageTransactionValue"
            },
            new
            {
                FunctionName = "GetDetailedInventory",
                Description = "Retrieve comprehensive inventory details including stock levels, values, turnover rates, and category breakdown. Essential for detailed inventory analysis.",
                Endpoint = "/api/supermarket/inventory/detailed",
                HttpMethod = "GET",
                Parameters = "includeZeroStock (optional, boolean)",
                ResponseType = "Object with products array, categoryBreakdown, totalValue, stockMetrics"
            }
        };

        foreach (var tool in toolDefinitions)
        {
            documents.Add(new McpToolDocument
            {
                Id = idCounter.ToString(),
                FunctionName = tool.FunctionName,
                Description = tool.Description,
                Category = "supermarket",
                HttpMethod = tool.HttpMethod,
                Endpoint = tool.Endpoint,
                Parameters = tool.Parameters,
                ResponseType = tool.ResponseType,
                LastUpdated = DateTimeOffset.UtcNow,
                IsActive = true
            });

            idCounter++;
        }

        _logger.LogInformation("Extracted {Count} tool documents from MCP definitions", documents.Count);
        return documents;
    }
}
