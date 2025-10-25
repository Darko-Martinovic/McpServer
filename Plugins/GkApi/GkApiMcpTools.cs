using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using McpServer.Plugins.GkApi.Services;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace McpServer.Plugins.GkApi;

[McpServerToolType]
public static class GkApiMcpTools
{
    [McpServerTool, Description("Get prices without base items from GkApi Pump collection")]
    public static async Task<string> GetPricesWithoutBaseItem(IGkApiDataService dataService)
    {
        try
        {
            Serilog.Log.Information("MCP Tool 'GetPricesWithoutBaseItem' called at {Timestamp}", DateTime.Now);

            var prices = await dataService.GetPricesWithoutBaseItemAsync();
            var result = JsonSerializer.Serialize(
                prices,
                new JsonSerializerOptions { WriteIndented = true }
            );

            Serilog.Log.Information(
                "MCP Tool 'GetPricesWithoutBaseItem' completed successfully. Returned {Count} prices",
                prices.Count()
            );
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'GetPricesWithoutBaseItem' failed: {Message}", ex.Message);
            return $"Error retrieving prices without base item: {ex.Message}";
        }
    }

    [McpServerTool, Description("Get latest processing statistics from GkApi Summary collection")]
    public static async Task<string> GetLatestStatistics(IGkApiDataService dataService)
    {
        try
        {
            Serilog.Log.Information("MCP Tool 'GetLatestStatistics' called at {Timestamp}", DateTime.Now);

            var statistics = await dataService.GetLatestStatisticsAsync();

            if (statistics == null)
            {
                Serilog.Log.Warning("MCP Tool 'GetLatestStatistics' found no data");
                return "No processing statistics found in Summary collection";
            }

            var result = JsonSerializer.Serialize(
                statistics,
                new JsonSerializerOptions { WriteIndented = true }
            );

            Serilog.Log.Information(
                "MCP Tool 'GetLatestStatistics' completed successfully. Returned statistics with {TypeCount} content types",
                statistics.ContentTypes.Count
            );
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'GetLatestStatistics' failed: {Message}", ex.Message);
            return $"Error retrieving latest statistics: {ex.Message}";
        }
    }

    [McpServerTool, Description("Get content types summary from latest processing statistics")]
    public static async Task<string> GetContentTypesSummary(IGkApiDataService dataService)
    {
        try
        {
            Serilog.Log.Information("MCP Tool 'GetContentTypesSummary' called at {Timestamp}", DateTime.Now);

            var statistics = await dataService.GetLatestStatisticsAsync();

            if (statistics == null || !statistics.ContentTypes.Any())
            {
                Serilog.Log.Warning("MCP Tool 'GetContentTypesSummary' found no content types");
                return "No content types found in latest processing statistics";
            }

            // Return just the contentTypes array as requested
            var result = JsonSerializer.Serialize(
                statistics.ContentTypes,
                new JsonSerializerOptions { WriteIndented = true }
            );

            Serilog.Log.Information(
                "MCP Tool 'GetContentTypesSummary' completed successfully. Returned {TypeCount} content types",
                statistics.ContentTypes.Count
            );
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'GetContentTypesSummary' failed: {Message}", ex.Message);
            return $"Error retrieving content types summary: {ex.Message}";
        }
    }

    [McpServerTool, Description("Search, find, show, get, list, display, or retrieve articles by name. Shows all articles that contain a specific text, word, or string in their name (case-insensitive partial match). Use this when user asks to find articles, show articles, get articles, list articles, search articles, or display articles that contain any text in their name like 'cola', 'pepsi', 'water', 'masti', 'coca cola', etc. Works with any search term or product name.")]
    public static async Task<string> FindArticlesByName(
        IGkApiDataService dataService,
        [Description("Part of the article name to search for (case-insensitive)")] string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Error: Name parameter is required";
            }

            Serilog.Log.Information("MCP Tool 'FindArticlesByName' called with name: {Name} at {Timestamp}",
                name, DateTime.Now);

            var articles = await dataService.FindArticlesByNameAsync(name);

            // Convert BsonDocuments to JSON string
            var articleList = articles.ToList();
            var result = new BsonArray(articleList).ToJson(new JsonWriterSettings { Indent = true });

            Serilog.Log.Information(
                "MCP Tool 'FindArticlesByName' completed successfully. Found {Count} articles",
                articleList.Count
            );
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'FindArticlesByName' failed: {Message}", ex.Message);
            return $"Error finding articles by name: {ex.Message}";
        }
    }

    [McpServerTool, Description("Find article by content key (automatically zero-padded to 18 digits) from GkApi Pump collection")]
    public static async Task<string> FindArticleByContentKey(
        IGkApiDataService dataService,
        [Description("Content key (will be automatically zero-padded to 18 digits, e.g., 1615 becomes 000000000000001615)")] string contentKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(contentKey))
            {
                return "Error: Content key parameter is required";
            }

            Serilog.Log.Information("MCP Tool 'FindArticleByContentKey' called with content key: {ContentKey} at {Timestamp}",
                contentKey, DateTime.Now);

            var article = await dataService.FindArticleByContentKeyAsync(contentKey);

            if (article == null)
            {
                var paddedKey = contentKey.PadLeft(18, '0');
                Serilog.Log.Warning("MCP Tool 'FindArticleByContentKey' found no article with key: {ContentKey}", paddedKey);
                return $"No article found with content key: {contentKey} (padded: {paddedKey})";
            }

            // Convert BsonDocument to JSON string
            var result = article.ToJson(new JsonWriterSettings { Indent = true });

            Serilog.Log.Information(
                "MCP Tool 'FindArticleByContentKey' completed successfully. Found article with key: {ContentKey}",
                contentKey
            );
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'FindArticleByContentKey' failed: {Message}", ex.Message);
            return $"Error finding article by content key: {ex.Message}";
        }
    }
}