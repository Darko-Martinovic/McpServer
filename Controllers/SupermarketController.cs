using McpServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using McpServer.Services.Interfaces;
using McpServer.Services;
using System.ComponentModel.DataAnnotations;

namespace McpServer.Controllers;

/// <summary>
/// REST API controller exposing supermarket MCP tools as HTTP endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SupermarketController : ControllerBase
{
    private readonly ISupermarketDataService _dataService;
    private readonly IAzureSearchService _azureSearchService;
    private readonly ILogger<SupermarketController> _logger;

    public SupermarketController(
        ISupermarketDataService dataService,
        IAzureSearchService azureSearchService,
        ILogger<SupermarketController> logger
    )
    {
        _dataService = dataService;
        _azureSearchService = azureSearchService;
        _logger = logger;
    }

    /// <summary>
    /// Get all products in the supermarket inventory
    /// </summary>
    /// <returns>List of all products</returns>
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts()
    {
        try
        {
            _logger.LogInformation("REST API: GetProducts called");
            var products = await _dataService.GetProductsAsync();

            return Ok(
                new
                {
                    success = true,
                    data = products,
                    count = products.Count(),
                    timestamp = DateTime.UtcNow
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REST API: GetProducts failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    // Add this to your SupermarketController
    [HttpGet("tools/schema")]
    public IActionResult GetToolsSchema() // Remove async since we're not awaiting anything
    {
        var schema = new
        {
            indexName = "mcp-tools",
            fields = new object[] // Explicitly type the array as object[]
            {
                new
                {
                    name = "id",
                    type = "Edm.String",
                    key = true,
                    searchable = false
                },
                new
                {
                    name = "functionName",
                    type = "Edm.String",
                    searchable = true,
                    filterable = true
                },
                new
                {
                    name = "description",
                    type = "Edm.String",
                    searchable = true
                },
                new
                {
                    name = "endpoint",
                    type = "Edm.String",
                    searchable = true
                },
                new
                {
                    name = "httpMethod",
                    type = "Edm.String",
                    filterable = true
                },
                new
                {
                    name = "parameters",
                    type = "Edm.String",
                    searchable = true
                },
                new
                {
                    name = "responseType",
                    type = "Edm.String",
                    searchable = true
                },
                new
                {
                    name = "category",
                    type = "Edm.String",
                    filterable = true
                },
                new
                {
                    name = "isActive",
                    type = "Edm.Boolean",
                    filterable = true
                },
                new
                {
                    name = "lastUpdated",
                    type = "Edm.DateTimeOffset",
                    sortable = true
                }
            }
        };

        // Use the same response format as other endpoints instead of ApiResponse
        return Ok(
            new
            {
                success = true,
                data = schema,
                message = "MCP tools index schema",
                timestamp = DateTime.UtcNow
            }
        );
    }

    /// <summary>
    /// Get sales data for a specific date range
    /// </summary>
    /// <param name="startDate">Start date in YYYY-MM-DD format</param>
    /// <param name="endDate">End date in YYYY-MM-DD format</param>
    /// <returns>Sales data for the specified period</returns>
    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesData(
        [FromQuery][Required] string startDate,
        [FromQuery][Required] string endDate
    )
    {
        try
        {
            if (
                !DateTime.TryParse(startDate, out var start)
                || !DateTime.TryParse(endDate, out var end)
            )
            {
                return BadRequest(
                    new { success = false, error = "Invalid date format. Use YYYY-MM-DD format." }
                );
            }

            _logger.LogInformation(
                "REST API: GetSalesData called for {StartDate} to {EndDate}",
                startDate,
                endDate
            );
            var salesData = await _dataService.GetSalesDataAsync(start, end);

            return Ok(
                new
                {
                    success = true,
                    data = salesData,
                    count = salesData.Count(),
                    dateRange = new { startDate, endDate },
                    timestamp = DateTime.UtcNow
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REST API: GetSalesData failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Calculate total revenue for a date range
    /// </summary>
    /// <param name="startDate">Start date in YYYY-MM-DD format</param>
    /// <param name="endDate">End date in YYYY-MM-DD format</param>
    /// <returns>Total revenue for the specified period</returns>
    [HttpGet("revenue")]
    public async Task<IActionResult> GetTotalRevenue(
        [FromQuery][Required] string startDate,
        [FromQuery][Required] string endDate
    )
    {
        try
        {
            if (
                !DateTime.TryParse(startDate, out var start)
                || !DateTime.TryParse(endDate, out var end)
            )
            {
                return BadRequest(
                    new { success = false, error = "Invalid date format. Use YYYY-MM-DD format." }
                );
            }

            _logger.LogInformation(
                "REST API: GetTotalRevenue called for {StartDate} to {EndDate}",
                startDate,
                endDate
            );
            var totalRevenue = await _dataService.GetTotalRevenueAsync(start, end);

            return Ok(
                new
                {
                    success = true,
                    data = new
                    {
                        totalRevenue,
                        startDate,
                        endDate
                    },
                    timestamp = DateTime.UtcNow
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REST API: GetTotalRevenue failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get products with stock levels below threshold
    /// </summary>
    /// <param name="threshold">Stock threshold (default: 10)</param>
    /// <returns>List of low stock products</returns>
    [HttpGet("products/low-stock")]
    public async Task<IActionResult> GetLowStockProducts([FromQuery] int threshold = 10)
    {
        try
        {
            _logger.LogInformation(
                "REST API: GetLowStockProducts called with threshold {Threshold}",
                threshold
            );
            var lowStockProducts = await _dataService.GetLowStockProductsAsync(threshold);

            return Ok(
                new
                {
                    success = true,
                    data = lowStockProducts,
                    count = lowStockProducts.Count(),
                    threshold,
                    timestamp = DateTime.UtcNow
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REST API: GetLowStockProducts failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get sales performance by product category
    /// </summary>
    /// <param name="startDate">Start date in YYYY-MM-DD format</param>
    /// <param name="endDate">End date in YYYY-MM-DD format</param>
    /// <returns>Sales data grouped by category</returns>
    [HttpGet("sales/by-category")]
    public async Task<IActionResult> GetSalesByCategory(
        [FromQuery][Required] string startDate,
        [FromQuery][Required] string endDate
    )
    {
        try
        {
            if (
                !DateTime.TryParse(startDate, out var start)
                || !DateTime.TryParse(endDate, out var end)
            )
            {
                return BadRequest(
                    new { success = false, error = "Invalid date format. Use YYYY-MM-DD format." }
                );
            }

            _logger.LogInformation(
                "REST API: GetSalesByCategory called for {StartDate} to {EndDate}",
                startDate,
                endDate
            );
            var categorySales = await _dataService.GetSalesByCategoryAsync(start, end);

            return Ok(
                new
                {
                    success = true,
                    data = categorySales,
                    count = categorySales.Count(),
                    dateRange = new { startDate, endDate },
                    timestamp = DateTime.UtcNow
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REST API: GetSalesByCategory failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get real-time inventory status with stock levels and recent sales
    /// </summary>
    /// <returns>Inventory status for all products</returns>
    [HttpGet("inventory/status")]
    public async Task<IActionResult> GetInventoryStatus()
    {
        try
        {
            _logger.LogInformation("REST API: GetInventoryStatus called");
            var inventoryStatus = await _dataService.GetInventoryStatusAsync();

            return Ok(
                new
                {
                    success = true,
                    data = inventoryStatus,
                    count = inventoryStatus.Count(),
                    timestamp = DateTime.UtcNow
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REST API: GetInventoryStatus failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get daily sales summary
    /// </summary>
    /// <param name="date">Date in YYYY-MM-DD format (optional, defaults to today)</param>
    /// <returns>Daily sales summary</returns>
    [HttpGet("sales/daily-summary")]
    public async Task<IActionResult> GetDailySummary([FromQuery] string? date = null)
    {
        try
        {
            DateTime targetDate = DateTime.Today;
            if (!string.IsNullOrEmpty(date))
            {
                if (!DateTime.TryParse(date, out targetDate))
                {
                    return BadRequest(
                        new
                        {
                            success = false,
                            error = "Invalid date format. Use YYYY-MM-DD format."
                        }
                    );
                }
            }

            _logger.LogInformation(
                "REST API: GetDailySummary called for {Date}",
                targetDate.ToString("yyyy-MM-dd")
            );
            var dailySummary = await _dataService.GetDailySummaryAsync(targetDate);

            return Ok(
                new
                {
                    success = true,
                    data = dailySummary,
                    date = targetDate.ToString("yyyy-MM-dd"),
                    timestamp = DateTime.UtcNow
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REST API: GetDailySummary failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get detailed inventory information for all products or filtered by category
    /// </summary>
    /// <param name="category">Optional category filter</param>
    /// <returns>Detailed inventory data</returns>
    [HttpGet("inventory/detailed")]
    public async Task<IActionResult> GetDetailedInventory([FromQuery] string? category = null)
    {
        try
        {
            _logger.LogInformation("REST API: GetDetailedInventory called with category: {Category}", category ?? "all");

            IEnumerable<Product> detailedInventory;
            if (!string.IsNullOrEmpty(category))
            {
                detailedInventory = await _dataService.GetProductsByCategoryAsync(category);
            }
            else
            {
                detailedInventory = await _dataService.GetDetailedInventoryAsync();
            }

            return Ok(
                new
                {
                    success = true,
                    data = detailedInventory,
                    count = detailedInventory.Count(),
                    category = category ?? "all",
                    timestamp = DateTime.UtcNow
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REST API: GetDetailedInventory failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get indexed MCP tools from Azure Search (for testing)
    /// </summary>
    /// <returns>List of indexed MCP tools</returns>
    [HttpGet("tools/indexed")]
    public async Task<IActionResult> GetIndexedTools()
    {
        try
        {
            _logger.LogInformation("REST API: GetIndexedTools called");

            var tools = await _azureSearchService.GetAllDocumentsAsync();

            return Ok(
                new
                {
                    success = true,
                    data = tools,
                    count = tools.Count(),
                    timestamp = DateTime.UtcNow
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REST API: GetIndexedTools failed");
            return StatusCode(
                500,
                new
                {
                    success = false,
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                }
            );
        }
    }

    /// <summary>
    /// Get products filtered by category
    /// </summary>
    /// <param name="category">Category to filter by</param>
    /// <returns>List of products in the specified category</returns>
    [HttpGet("products/category/{category}")]
    public async Task<IActionResult> GetProductsByCategory(string category)
    {
        try
        {
            _logger.LogInformation("REST API: GetProductsByCategory called with category: {Category}", category);
            var products = await _dataService.GetProductsByCategoryAsync(category);

            return Ok(
                new
                {
                    success = true,
                    data = products,
                    count = products.Count(),
                    category = category,
                    timestamp = DateTime.UtcNow
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REST API: GetProductsByCategory failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}
