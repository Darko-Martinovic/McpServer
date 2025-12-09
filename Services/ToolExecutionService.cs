using McpServer.Models;
using McpServer.Models.ToolProxy;
using McpServer.Services.Interfaces;
using McpServer.Plugins.ThirdApi.Services;
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
    private readonly IThirdApiDataService _thirdApiService;
    private readonly IAzureSearchService _azureSearchService;
    private readonly ILogger<ToolExecutionService> _logger;

    // Tools that don't require any parameters
    private static readonly HashSet<string> NoParameterTools = new()
    {
        "GetArticlesWithIngredients",
        "GetContentTypesSummary",
        "GetPricesWithoutBaseItem",
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

    // ThirdApi/MongoDB tools
    private static readonly HashSet<string> ThirdApiTools = new()
    {
        "GetContentTypesSummary",
        "GetPricesWithoutBaseItem",
        "GetLatestStatistics",
        "FindArticlesByName",
        "FindArticleByContentKey",
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
        IThirdApiDataService thirdApiService,
        IAzureSearchService azureSearchService,
        ILogger<ToolExecutionService> logger)
    {
        _supermarketService = supermarketService;
        _thirdApiService = thirdApiService;
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
            var isThirdApi = ThirdApiTools.Contains(toolName);
            var isSupermarket = SupermarketTools.Contains(toolName);

            object? result;
            
            if (isThirdApi)
            {
                result = await ExecuteThirdApiToolAsync(toolName, arguments);
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
                Plugin = isThirdApi ? "thirdapi" : "supermarket",
                IsMongoDb = isThirdApi
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
                Plugin = ThirdApiTools.Contains(toolName) ? "thirdapi" : 
                         SupermarketTools.Contains(toolName) ? "supermarket" : null
            };
        }
    }

    private async Task<object> ExecuteThirdApiToolAsync(string toolName, Dictionary<string, object>? arguments)
    {
        return toolName switch
        {
            "GetContentTypesSummary" => await GetContentTypesSummaryAsync(),
            "GetPricesWithoutBaseItem" => await GetPricesWithoutBaseItemAsync(),
            "GetLatestStatistics" => await GetLatestStatisticsAsync(),
            "GetPluData" => await GetPluDataAsync(),
            "GetArticlesWithIngredients" => await GetArticlesWithIngredientsAsync(),
            "FindArticlesByName" => await FindArticlesByNameAsync(arguments),
            "FindArticleByContentKey" => await FindArticleByContentKeyAsync(arguments),
            _ => throw new InvalidOperationException($"Unknown ThirdApi tool: {toolName}")
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

    #region ThirdApi Tool Implementations

    private async Task<object> GetContentTypesSummaryAsync()
    {
        var statistics = await _thirdApiService.GetLatestStatisticsAsync();
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

    private async Task<object> GetPricesWithoutBaseItemAsync()
    {
        var prices = await _thirdApiService.GetPricesWithoutBaseItemAsync();
        return new
        {
            success = true,
            data = prices,
            count = prices.Count()
        };
    }

    private async Task<object> GetLatestStatisticsAsync()
    {
        var statistics = await _thirdApiService.GetLatestStatisticsAsync();
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
        var pluData = await _thirdApiService.GetPluDataAsync();
        return new
        {
            success = true,
            data = pluData,
            count = pluData.Count()
        };
    }

    private async Task<object> GetArticlesWithIngredientsAsync()
    {
        var articles = await _thirdApiService.GetArticlesWithIngredientsAsync();
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

        var articles = await _thirdApiService.FindArticlesByNameAsync(name);
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

    private async Task<object> FindArticleByContentKeyAsync(Dictionary<string, object>? arguments)
    {
        var contentKey = GetStringArgument(arguments, "contentKey");
        if (string.IsNullOrWhiteSpace(contentKey))
        {
            throw new ArgumentException("FindArticleByContentKey requires a 'contentKey' parameter");
        }

        var article = await _thirdApiService.FindArticleByContentKeyAsync(contentKey);
        if (article == null)
        {
            return new
            {
                success = false,
                error = $"Article not found with content key: {contentKey}",
                paddedContentKey = contentKey.PadLeft(18, '0')
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
                // Check for analytics/ThirdApi query keywords
                var isAnalyticsQuery = query.ToLower().Contains("analytics") ||
                                       query.ToLower().Contains("statistics") ||
                                       query.ToLower().Contains("content") ||
                                       query.ToLower().Contains("mongodb") ||
                                       query.ToLower().Contains("prices") ||
                                       query.ToLower().Contains("summary") ||
                                       query.ToLower().Contains("ingredient");

                if (isAnalyticsQuery)
                {
                    var thirdApiTool = results.FirstOrDefault(t => 
                        t.Category?.ToLower().Contains("thirdapi") == true ||
                        t.Endpoint?.Contains("/thirdapi/") == true);
                    
                    if (thirdApiTool != null)
                    {
                        _logger.LogInformation("Selected ThirdApi tool for analytics query: {ToolName}", thirdApiTool.FunctionName);
                        return thirdApiTool;
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

        // Get ThirdApi tools
        schemas["thirdapi"] = new
        {
            plugin = "thirdapi",
            tools = ThirdApiTools.Select(t => new { name = t, requiresParameters = !NoParameterTools.Contains(t) })
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

        // Extract content key from queries like "article 7388" or "item 1615"
        var contentKeyPatterns = new[]
        {
            @"(?:article|item|product|key|id|number|code)\s+(\d+)",
            @"#(\d+)",
            @"\b(\d{4,})\b"  // 4+ digit numbers standalone
        };

        foreach (var pattern in contentKeyPatterns)
        {
            var match = Regex.Match(query, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                parameters["contentKey"] = match.Groups[1].Value;
                _logger.LogDebug("Extracted contentKey: {ContentKey}", parameters["contentKey"]);
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

        if (!parameters.ContainsKey("contentKey"))
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

