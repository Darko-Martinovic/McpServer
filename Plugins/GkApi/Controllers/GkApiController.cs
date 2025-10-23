using McpServer.Plugins.GkApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace McpServer.Plugins.GkApi.Controllers;

/// <summary>
/// REST API controller for GkApi operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class GkApiController : ControllerBase
{
    private readonly IGkApiDataService _dataService;
    private readonly ILogger<GkApiController> _logger;

    public GkApiController(
        IGkApiDataService dataService,
        ILogger<GkApiController> logger
    )
    {
        _dataService = dataService;
        _logger = logger;
    }

    /// <summary>
    /// Get prices without base items from Pump collection
    /// </summary>
    /// <returns>List of prices without base items</returns>
    [HttpGet("prices-without-base-item")]
    public async Task<IActionResult> GetPricesWithoutBaseItem()
    {
        try
        {
            _logger.LogInformation("REST API: GetPricesWithoutBaseItem called");

            var prices = await _dataService.GetPricesWithoutBaseItemAsync();

            return Ok(new
            {
                success = true,
                data = prices,
                count = prices.Count(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REST API: GetPricesWithoutBaseItem failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get latest processing statistics from Summary collection
    /// </summary>
    /// <returns>Latest processing statistics</returns>
    [HttpGet("latest-statistics")]
    public async Task<IActionResult> GetLatestStatistics()
    {
        try
        {
            _logger.LogInformation("REST API: GetLatestStatistics called");

            var statistics = await _dataService.GetLatestStatisticsAsync();

            if (statistics == null)
            {
                return NotFound(new { success = false, error = "No processing statistics found" });
            }

            return Ok(new
            {
                success = true,
                data = statistics,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REST API: GetLatestStatistics failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get content types summary from latest processing statistics
    /// </summary>
    /// <returns>Array of content types with counts</returns>
    [HttpGet("content-types")]
    public async Task<IActionResult> GetContentTypesSummary()
    {
        try
        {
            _logger.LogInformation("REST API: GetContentTypesSummary called");

            var statistics = await _dataService.GetLatestStatisticsAsync();

            if (statistics == null || !statistics.ContentTypes.Any())
            {
                return NotFound(new { success = false, error = "No content types found" });
            }

            return Ok(new
            {
                success = true,
                data = statistics.ContentTypes,
                count = statistics.ContentTypes.Count,
                totalDocuments = statistics.TotalDocuments,
                totalUniqueTypes = statistics.TotalUniqueTypes,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REST API: GetContentTypesSummary failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Health check for GkApi MongoDB connection
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var isHealthy = await _dataService.IsHealthyAsync();

            if (isHealthy)
            {
                return Ok(new
                {
                    success = true,
                    status = "healthy",
                    database = "GkApi",
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return StatusCode(503, new
                {
                    success = false,
                    status = "unhealthy",
                    database = "GkApi",
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REST API: Health check failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}