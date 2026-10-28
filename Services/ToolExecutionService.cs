using McpServer.Models;
using McpServer.Models.ToolProxy;
using McpServer.Services.Interfaces;
using McpServer.Plugins.Catalog.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace McpServer.Services;

/// <summary>
/// Service for executing MCP tools by name with automatic routing
/// </summary>
public class ToolExecutionService : IToolExecutionService
{
    private readonly ISupermarketDataService _supermarketService;
    private readonly ICatalogDataService _catalogService;
    private readonly IAzureSearchService _azureSearchService;
    private readonly ILogger<ToolExecutionService> _logger;

    // Tools that don't require any parameters
    private static readonly HashSet<string> NoParameterTools = new()
    {
        "GetArticlesWithIngredients",
        "GetContentTypesSummary",
        "GetPricesWithoutArticle",
        "GetLatestStatistics",
        "GetPluData",
        "GetProducts",
        "GetSalesData",
        "GetTotalRevenue",
        "GetLowStockProducts",
        "GetSalesByCategory",
        "GetInventoryStatus",
        "GetDailySummary",
        "GetDetailedInventory"
    };

    // Catalog/MongoDB tools
    private static readonly HashSet<string> CatalogTools = new()
    {
        "GetContentTypesSummary",
        "GetPricesWithoutArticle",
        "GetLatestStatistics",
        "FindArticlesByName",
        "FindArticleByArticleKey",
        "GetPluData",
        "GetArticlesWithIngredients"
    };

    // Supermarket tools
    private static readonly HashSet<string> SupermarketTools = new()
    {
        "GetProducts",
        "GetSalesData",
        "GetTotalRevenue",
        "GetLowStockProducts",
        "GetSalesByCategory",
        "GetInventoryStatus",
        "GetDailySummary",
        "GetDetailedInventory"
    };

    public ToolExecutionService(
        ISupermarketDataService supermarketService,
        ICatalogDataService catalogService,
        IAzureSearchService azureSearchService,
        ILogger<ToolExecutionService> logger)
    {
        _supermarketService = supermarketService;
        _catalogService = catalogService;
        _azureSearchService = azureSearchService;
        _logger = logger;
    }

    public async Task<ToolExecutionResult> ExecuteToolAsync(
        string toolName, 
        Dictionary<string, object>? arguments = null, 
        string? originalQuery = null)
    {
        _logger.LogInformation("Executing tool: {ToolName} with arguments: {Arguments}", 
            toolName, arguments != null ? JsonSerializer.Serialize(arguments) : "none");

        try
        {
            // Clear arguments for no-parameter tools
            if (NoParameterTools.Contains(toolName))
            {
                _logger.LogDebug("Tool {ToolName} does not require parameters - clearing any extracted params", toolName);
                arguments = null;
            }
            // Extract parameters from query if arguments are empty but query is provided
            else if ((arguments == null || arguments.Count == 0) && !string.IsNullOrEmpty(originalQuery))
            {
                arguments = ExtractParametersFromQuery(originalQuery);
                _logger.LogDebug("Extracted parameters from query: {Parameters}", JsonSerializer.Serialize(arguments));
            }

            // Determine plugin and execute
            var isCatalog = CatalogTools.Contains(toolName);
            var isSupermarket = SupermarketTools.Contains(toolName);

            object? result;
            
            if (isCatalog)
            {
                result = await ExecuteCatalogToolAsync(toolName, arguments);
            }
            else if (isSupermarket)
            {
                result = await ExecuteSupermarketToolAsync(toolName, arguments);
            }
            else
            {
                throw new InvalidOperationException($"Unknown tool: {toolName}");
            }

            return new ToolExecutionResult
            {
                Tool = toolName,
                Data = result,
                Success = true,
                Plugin = isCatalog ? "catalog" : "supermarket",
                IsMongoDb = isCatalog
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute tool: {ToolName}", toolName);
            return new ToolExecutionResult
            {
                Tool = toolName,
                Success = false,
                Error = ex.Message,
                Plugin = CatalogTools.Contains(toolName) ? "catalog" : 
                         SupermarketTools.Contains(toolName) ? "supermarket" : null
            };
        }
    }

    private async Task<object> ExecuteCatalogToolAsync(string toolName, Dictionary<string, object>? arguments)
    {
        return toolName switch
        {
            "GetContentTypesSummary" => await GetContentTypesSummaryAsync(),
            "GetPricesWithoutArticle" => await GetPricesWithoutArticleAsync(),
            "GetLatestStatistics" => await GetLatestStatisticsAsync(),
            "GetPluData" => await GetPluDataAsync(),
            "GetArticlesWithIngredients" => await GetArticlesWithIngredientsAsync(),
            "FindArticlesByName" => await FindArticlesByNameAsync(arguments),
            "FindArticleByArticleKey" => await FindArticleByArticleKeyAsync(arguments),
            _ => throw new InvalidOperationException($"Unknown Catalog tool: {toolName}")
        };
    }

    private async Task<object> ExecuteSupermarketToolAsync(string toolName, Dictionary<string, object>? arguments)
    {
        return toolName switch
        {
            "GetProducts" => await GetProductsAsync(),
            "GetSalesData" => await GetSalesDataAsync(),
            "GetTotalRevenue" => await GetTotalRevenueAsync(),
            "GetLowStockProducts" => await GetLowStockProductsAsync(),
            "GetSalesByCategory" => await GetSalesByCategoryAsync(),
            "GetInventoryStatus" => await GetInventoryStatusAsync(),
            "GetDailySummary" => await GetDailySummaryAsync(),
            "GetDetailedInventory" => await GetDetailedInventoryAsync(),
            _ => throw new InvalidOperationException($"Unknown Supermarket tool: {toolName}")
        };
    }

    #region Catalog Tool Implementations

    private async Task<object> GetContentTypesSummaryAsync()
    {
        var statistics = await _catalogService.GetLatestStatisticsAsync();
        if (statistics == null || !statistics.ContentTypes.Any())
        {
            return new { success = false, error = "No content types found" };
        }
        return new
        {
            success = true,
            data = statistics.ContentTypes,
            count = statistics.ContentTypes.Count,
            totalDocuments = statistics.TotalDocuments,
            totalUniqueTypes = statistics.TotalUniqueTypes
        };
    }

    private async Task<object> GetPricesWithoutArticleAsync()
    {
        var prices = await _catalogService.GetPricesWithoutArticleAsync();
        return new
        {
            success = true,
            data = prices,
            count = prices.Count()
        };
    }

    private async Task<object> GetLatestStatisticsAsync()
    {
        var statistics = await _catalogService.GetLatestStatisticsAsync();
        if (statistics == null)
        {
            return new { success = false, error = "No processing statistics found" };
        }
        return new
        {
            success = true,
            data = statistics
        };
    }

    private async Task<object> GetPluDataAsync()
    {
        var pluData = await _catalogService.GetPluDataAsync();
        return new
        {
            success = true,
            data = pluData,
            count = pluData.Count()
        };
    }

    private async Task<object> GetArticlesWithIngredientsAsync()
    {
        var articles = await _catalogService.GetArticlesWithIngredientsAsync();
        return new
        {
            success = true,
            data = articles,
            count = articles.Count()
        };
    }

    private async Task<object> FindArticlesByNameAsync(Dictionary<string, object>? arguments)
    {
        var name = GetStringArgument(arguments, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("FindArticlesByName requires a 'name' parameter");
        }

        var articles = await _catalogService.FindArticlesByNameAsync(name);
        var articleList = articles.ToList();
        
        // Convert BsonDocuments to JSON-serializable format
        var jsonArticles = articleList.Select(doc =>
            JsonSerializer.Deserialize<JsonElement>(
                doc.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson })
            )
        ).ToList();

        return new
        {
            success = true,
            data = jsonArticles,
            count = jsonArticles.Count,
            searchTerm = name
        };
    }

    private async Task<object> FindArticleByArticleKeyAsync(Dictionary<string, object>? arguments)
    {
        var articleKey = GetStringArgument(arguments, "articleKey");
        if (string.IsNullOrWhiteSpace(articleKey))
        {
            throw new ArgumentException("FindArticleByArticleKey requires a 'articleKey' parameter");
        }

        var article = await _catalogService.FindArticleByArticleKeyAsync(articleKey);
        if (article == null)
        {
            return new
            {
                success = false,
                error = $"Article not found with article key: {articleKey}",
                paddedArticleKey = articleKey.PadLeft(8, '0')
            };
        }

        var jsonArticle = JsonSerializer.Deserialize<JsonElement>(
            article.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson })
        );

        return new
        {
            success = true,
            data = jsonArticle
        };
    }

    #endregion

    #region Supermarket Tool Implementations

    private async Task<object> GetProductsAsync()
    {
        var products = await _supermarketService.GetProductsAsync();
        return new
        {
            success = true,
            data = products,
            count = products.Count()
        };
    }

    private async Task<object> GetSalesDataAsync()
    {
        // Default to last 30 days of sales data
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-30);
        var sales = await _supermarketService.GetSalesDataAsync(startDate, endDate);
        return new
        {
            success = true,
            data = sales,
            count = sales.Count(),
            dateRange = new { startDate, endDate }
        };
    }

    private async Task<object> GetTotalRevenueAsync()
    {
        // Default to last 30 days of revenue
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-30);
        var revenue = await _supermarketService.GetTotalRevenueAsync(startDate, endDate);
        return new
        {
            success = true,
            data = new { totalRevenue = revenue },
            dateRange = new { startDate, endDate }
        };
    }

    private async Task<object> GetLowStockProductsAsync()
    {
        // Default threshold of 10 units
        const int defaultThreshold = 10;
        var products = await _supermarketService.GetLowStockProductsAsync(defaultThreshold);
        return new
        {
            success = true,
            data = products,
            count = products.Count(),
            threshold = defaultThreshold
        };
    }

    private async Task<object> GetSalesByCategoryAsync()
    {
        // Default to last 30 days
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-30);
        var sales = await _supermarketService.GetSalesByCategoryAsync(startDate, endDate);
        return new
        {
            success = true,
            data = sales,
            count = sales.Count(),
            dateRange = new { startDate, endDate }
        };
    }

    private async Task<object> GetInventoryStatusAsync()
    {
        var inventory = await _supermarketService.GetInventoryStatusAsync();
        return new
        {
            success = true,
            data = inventory,
            count = inventory.Count()
        };
    }

    private async Task<object> GetDailySummaryAsync()
    {
        var summary = await _supermarketService.GetDailySummaryAsync();
        return new
        {
            success = true,
            data = summary,
            count = summary.Count()
        };
    }

    private async Task<object> GetDetailedInventoryAsync()
    {
        var inventory = await _supermarketService.GetDetailedInventoryAsync();
        return new
        {
            success = true,
            data = inventory,
            count = inventory.Count()
        };
    }

    #endregion

    public async Task<McpToolDocument?> SearchForToolAsync(string query)
    {
        try
        {
            _logger.LogInformation("Searching for tool with query: {Query}", query);
            
            var results = await _azureSearchService.SearchToolsAsync(query);
            
            if (results.Any())
            {
                // Check for analytics/Catalog query keywords
                var isAnalyticsQuery = query.ToLower().Contains("analytics") ||
                                       query.ToLower().Contains("statistics") ||
                                       query.ToLower().Contains("content") ||
                                       query.ToLower().Contains("mongodb") ||
                                       query.ToLower().Contains("prices") ||
                                       query.ToLower().Contains("summary") ||
                                       query.ToLower().Contains("ingredient");

                if (isAnalyticsQuery)
                {
                    var catalogTool = results.FirstOrDefault(t => 
                        t.Category?.ToLower().Contains("catalog") == true ||
                        t.Endpoint?.Contains("/catalog/") == true);
                    
                    if (catalogTool != null)
                    {
                        _logger.LogInformation("Selected Catalog tool for analytics query: {ToolName}", catalogTool.FunctionName);
                        return catalogTool;
                    }
                }

                var bestTool = results.FirstOrDefault(t => !string.IsNullOrEmpty(t.FunctionName) && !string.IsNullOrEmpty(t.Endpoint))
                               ?? results.First();
                
                _logger.LogInformation("Selected tool: {ToolName}", bestTool.FunctionName);
                return bestTool;
            }

            _logger.LogWarning("No tools found for query: {Query}", query);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for tool with query: {Query}", query);
            return null;
        }
    }

    public async Task<Dictionary<string, object>> GetToolSchemasAsync()
    {
        var schemas = new Dictionary<string, object>();

        // Get Supermarket tools
        schemas["supermarket"] = new
        {
            plugin = "supermarket",
            tools = SupermarketTools.Select(t => new { name = t, requiresParameters = !NoParameterTools.Contains(t) })
        };

        // Get Catalog tools
        schemas["catalog"] = new
        {
            plugin = "catalog",
            tools = CatalogTools.Select(t => new { name = t, requiresParameters = !NoParameterTools.Contains(t) })
        };

        return await Task.FromResult(schemas);
    }

    public Dictionary<string, object> ExtractParametersFromQuery(string query)
    {
        var parameters = new Dictionary<string, object>();

        if (string.IsNullOrWhiteSpace(query))
        {
            return parameters;
        }

        _logger.LogDebug("Attempting to extract parameters from: {Query}", query);

        // Extract article key from queries like "article 1002" or "item 1001"
        var articleKeyPatterns = new[]
        {
            @"(?:article|item|product|key|id|number|code)\s+(\d+)",
            @"#(\d+)",
            @"\b(\d{4,})\b"  // 4+ digit numbers standalone
        };

        foreach (var pattern in articleKeyPatterns)
        {
            var match = Regex.Match(query, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                parameters["articleKey"] = match.Groups[1].Value;
                _logger.LogDebug("Extracted articleKey: {ArticleKey}", parameters["articleKey"]);
                break;
            }
        }

        // Extract name search terms from queries
        var namePatterns = new[]
        {
            @"(?:named?|called?)\s+['""]?([a-z0-9\s]+)['""]?",
            @"(?:with|containing?).*?['""]?([a-z0-9\s]+)['""]?.*?(?:in|name)",
            @"(?:search|find|show|get).*?(?:for|with)\s+['""]?([a-z0-9\s]+)['""]?",
            @"(?:have|has|contains?)\s+['""]?([a-z0-9\s]+)['""]?\s+in",
            @"['""]([a-z0-9\s]+)['""]",
            @"(?:search|find|show|get|list|display)\s+(?:articles?|items?|products?)\s+([a-z0-9\s]+)",
            @"articles?\s+([a-z0-9]+)$"
        };

        if (!parameters.ContainsKey("articleKey"))
        {
            foreach (var pattern in namePatterns)
            {
                var match = Regex.Match(query, pattern, RegexOptions.IgnoreCase);
                if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                {
                    var extractedName = match.Groups[1].Value.Trim();
                    var skipWords = new[] { "with", "containing", "named", "called", "by", "that", "contain", "in", "their", "the" };
                    
                    if (!skipWords.Contains(extractedName.ToLower()))
                    {
                        parameters["name"] = extractedName;
                        _logger.LogDebug("Extracted name: {Name}", extractedName);
                        break;
                    }
                }
            }
        }

        _logger.LogDebug("Final extracted parameters: {Parameters}", JsonSerializer.Serialize(parameters));
        return parameters;
    }

    private static string? GetStringArgument(Dictionary<string, object>? arguments, string key)
    {
        if (arguments == null || !arguments.TryGetValue(key, out var value))
        {
            return null;
        }

        return value switch
        {
            string s => s,
            JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString(),
            _ => value.ToString()
        };
    }
}

