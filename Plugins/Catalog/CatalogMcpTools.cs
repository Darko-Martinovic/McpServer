using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using McpServer.Plugins.Catalog.Services;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace McpServer.Plugins.Catalog;

[McpServerToolType]
public static class CatalogMcpTools
{
    [McpServerTool, Description("Get prices without articles from Catalog articles collection")]
    public static async Task<string> GetPricesWithoutArticle(ICatalogDataService dataService)
    {
        try
        {
            Serilog.Log.Information("MCP Tool 'GetPricesWithoutArticle' called at {Timestamp}", DateTime.Now);

            var prices = await dataService.GetPricesWithoutArticleAsync();
            var result = JsonSerializer.Serialize(
                prices,
                new JsonSerializerOptions { WriteIndented = true }
            );

            Serilog.Log.Information(
                "MCP Tool 'GetPricesWithoutArticle' completed successfully. Returned {Count} prices",
                prices.Count()
            );
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'GetPricesWithoutArticle' failed: {Message}", ex.Message);
            return $"Error retrieving prices without article: {ex.Message}";
        }
    }

    [McpServerTool, Description("Get latest processing statistics from Catalog import_stats collection")]
    public static async Task<string> GetLatestStatistics(ICatalogDataService dataService)
    {
        try
        {
            Serilog.Log.Information("MCP Tool 'GetLatestStatistics' called at {Timestamp}", DateTime.Now);

            var statistics = await dataService.GetLatestStatisticsAsync();

            if (statistics == null)
            {
                Serilog.Log.Warning("MCP Tool 'GetLatestStatistics' found no data");
                return "No processing statistics found in import_stats collection";
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
    public static async Task<string> GetContentTypesSummary(ICatalogDataService dataService)
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

    [McpServerTool, Description("Search, find, show, get, list, display, or retrieve articles by name. Shows all articles that contain a specific text, word, or string in their name (case-insensitive partial match). Use this when user asks to find articles, show articles, get articles, list articles, search articles, or display articles that contain any text in their name like 'cola', 'pepsi', 'water', 'coca cola', etc. Works with any search term or product name.")]
    public static async Task<string> FindArticlesByName(
        ICatalogDataService dataService,
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

    [McpServerTool, Description("Find article by article key (automatically zero-padded to 8 digits) from Catalog articles collection")]
    public static async Task<string> FindArticleByArticleKey(
        ICatalogDataService dataService,
        [Description("Article key (will be automatically zero-padded to 8 digits, e.g., 1001 becomes 00001001)")] string articleKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(articleKey))
            {
                return "Error: Article key parameter is required";
            }

            Serilog.Log.Information("MCP Tool 'FindArticleByArticleKey' called with article key: {ArticleKey} at {Timestamp}",
                articleKey, DateTime.Now);

            var article = await dataService.FindArticleByArticleKeyAsync(articleKey);

            if (article == null)
            {
                var paddedKey = articleKey.PadLeft(8, '0');
                Serilog.Log.Warning("MCP Tool 'FindArticleByArticleKey' found no article with key: {ArticleKey}", paddedKey);
                return $"No article found with article key: {articleKey} (padded: {paddedKey})";
            }

            // Convert BsonDocument to JSON string
            var result = article.ToJson(new JsonWriterSettings { Indent = true });

            Serilog.Log.Information(
                "MCP Tool 'FindArticleByArticleKey' completed successfully. Found article with key: {ArticleKey}",
                articleKey
            );
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'FindArticleByArticleKey' failed: {Message}", ex.Message);
            return $"Error finding article by article key: {ex.Message}";
        }
    }

    [McpServerTool, Description("Get PLU data from Acme's back office (PosItemList). Returns PLU codes, group information, and sequence numbers sorted by article key and sequence. Used for POS item lists, product groups, and back-office integration.")]
    public static async Task<string> GetPluData(ICatalogDataService dataService)
    {
        try
        {
            Serilog.Log.Information("MCP Tool 'GetPluData' called at {Timestamp}", DateTime.Now);

            var pluData = await dataService.GetPluDataAsync();
            var result = JsonSerializer.Serialize(
                pluData,
                new JsonSerializerOptions { WriteIndented = true }
            );

            Serilog.Log.Information(
                "MCP Tool 'GetPluData' completed successfully. Returned {Count} PLU entries",
                pluData.Count()
            );
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'GetPluData' failed: {Message}", ex.Message);
            return $"Error retrieving PLU data: {ex.Message}";
        }
    }

    [McpServerTool, Description("Show articles with ingredients. Retrieves all articles that have ingredient information (the INGREDIENTS text type). Returns article article key, name, text type, language, and concatenated ingredient text. Use this when user asks about ingredients, article ingredients, product ingredients, or wants to see what ingredients articles contain.")]
    public static async Task<string> GetArticlesWithIngredients(ICatalogDataService dataService)
    {
        try
        {
            Serilog.Log.Information("MCP Tool 'GetArticlesWithIngredients' called at {Timestamp}", DateTime.Now);

            var articlesWithIngredients = await dataService.GetArticlesWithIngredientsAsync();
            var result = JsonSerializer.Serialize(
                articlesWithIngredients,
                new JsonSerializerOptions { WriteIndented = true }
            );

            Serilog.Log.Information(
                "MCP Tool 'GetArticlesWithIngredients' completed successfully. Returned {Count} articles with ingredients",
                articlesWithIngredients.Count()
            );
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "MCP Tool 'GetArticlesWithIngredients' failed: {Message}", ex.Message);
            return $"Error retrieving articles with ingredients: {ex.Message}";
        }
    }
}