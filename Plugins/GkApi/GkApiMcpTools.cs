using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using McpServer.Plugins.GkApi.Services;

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
}