using McpServer.Plugins.Catalog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using System.Text.Json;
using Asp.Versioning;

namespace McpServer.Plugins.Catalog.Controllers;

/// <summary>
/// REST API controller for Catalog operations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/catalog")]
[Produces("application/json")]
public class CatalogController : ControllerBase
{
    private readonly ICatalogDataService _dataService;
    private readonly ILogger<CatalogController> _logger;

    public CatalogController(
        ICatalogDataService dataService,
        ILogger<CatalogController> logger
    )
    {
        _dataService = dataService;
        _logger = logger;
    }

    /// <summary>
    /// Get prices without articles from articles collection
    /// </summary>
    /// <returns>List of prices without articles</returns>
    [HttpGet("prices-without-article")]
    public async Task<IActionResult> GetPricesWithoutArticle()
    {
        try
        {
            _logger.LogInformation("REST API: GetPricesWithoutArticle called");

            var prices = await _dataService.GetPricesWithoutArticleAsync();

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
            _logger.LogError(ex, "REST API: GetPricesWithoutArticle failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get latest processing statistics from import_stats collection
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
    /// Health check for Catalog MongoDB connection
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
                    database = "Catalog",
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return StatusCode(503, new
                {
                    success = false,
                    status = "unhealthy",
                    database = "Catalog",
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
    /// Get articles with ingredients (the INGREDIENTS text type)
    /// </summary>
    /// <returns>List of articles with concatenated ingredient text</returns>
    [HttpGet("articles/ingredients")]
    public async Task<IActionResult> GetArticlesWithIngredients()
    {
        try
        {
            _logger.LogInformation("REST API: GetArticlesWithIngredients called");

            var articlesWithIngredients = await _dataService.GetArticlesWithIngredientsAsync();

            return Ok(new
            {
                success = true,
                data = articlesWithIngredients,
                count = articlesWithIngredients.Count(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REST API: GetArticlesWithIngredients failed");
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
    /// Find article by article key (with automatic zero-padding to 8 digits)
    /// </summary>
    /// <param name="articleKey">Article key (will be zero-padded to 8 digits)</param>
    /// <returns>Article details</returns>
    [HttpGet("articles/{articleKey}")]
    public async Task<IActionResult> FindArticleByArticleKey(string articleKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(articleKey))
            {
                return BadRequest(new { success = false, error = "Article key parameter is required" });
            }

            _logger.LogInformation("REST API: FindArticleByArticleKey called with article key: {ArticleKey}", articleKey);

            var article = await _dataService.FindArticleByArticleKeyAsync(articleKey);

            if (article == null)
            {
                return NotFound(new
                {
                    success = false,
                    error = $"Article not found with article key: {articleKey}",
                    paddedArticleKey = articleKey.PadLeft(8, '0')
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
            _logger.LogError(ex, "REST API: FindArticleByArticleKey failed");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get PLU data from Acme's back office (PosItemList)
    /// </summary>
    /// <returns>List of PLU data entries sorted by article key and sequence number</returns>
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