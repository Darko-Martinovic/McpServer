using McpServer.Plugins.GkApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using System.Text.Json;

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

    /// <summary>
    /// Find articles by name (case-insensitive partial match)
    /// </summary>
    /// <param name="name">Part of the article name to search for</param>
    /// <returns>List of matching articles</returns>
    [HttpGet("articles/search")]
    public async Task<IActionResult> FindArticlesByName([FromQuery] string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { success = false, error = "Name parameter is required" });
            }

            _logger.LogInformation("REST API: FindArticlesByName called with name: {Name}", name);

            var articles = await _dataService.FindArticlesByNameAsync(name);

            // Convert BsonDocuments to JSON objects for proper serialization
            var articleList = articles.ToList();
            var jsonArticles = articleList.Select(doc =>
                JsonSerializer.Deserialize<JsonElement>(
                    doc.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson })
                )
            ).ToList();

            return Ok(new
            {
                success = true,
                data = jsonArticles,
                count = jsonArticles.Count,
                searchTerm = name,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REST API: FindArticlesByName failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Find article by content key (with automatic zero-padding to 18 digits)
    /// </summary>
    /// <param name="contentKey">Content key (will be zero-padded to 18 digits)</param>
    /// <returns>Article details</returns>
    [HttpGet("articles/{contentKey}")]
    public async Task<IActionResult> FindArticleByContentKey(string contentKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(contentKey))
            {
                return BadRequest(new { success = false, error = "Content key parameter is required" });
            }

            _logger.LogInformation("REST API: FindArticleByContentKey called with content key: {ContentKey}", contentKey);

            var article = await _dataService.FindArticleByContentKeyAsync(contentKey);

            if (article == null)
            {
                return NotFound(new
                {
                    success = false,
                    error = $"Article not found with content key: {contentKey}",
                    paddedContentKey = contentKey.PadLeft(18, '0')
                });
            }

            // Convert BsonDocument to JSON object for proper serialization
            var jsonArticle = JsonSerializer.Deserialize<JsonElement>(
                article.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson })
            );

            return Ok(new
            {
                success = true,
                data = jsonArticle,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REST API: FindArticleByContentKey failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get PLU data from SAP Fiori (DynamicTableauItemListDO)
    /// </summary>
    /// <returns>List of PLU data entries sorted by content key and sequence number</returns>
    [HttpGet("plu-data")]
    public async Task<IActionResult> GetPluData()
    {
        try
        {
            _logger.LogInformation("REST API: GetPluData called");

            var pluData = await _dataService.GetPluDataAsync();

            return Ok(new
            {
                success = true,
                data = pluData,
                count = pluData.Count(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REST API: GetPluData failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}