using McpServer.Models;
using McpServer.Services.Interfaces;
using McpServer.Plugins.Services;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;
using System.ComponentModel;
using ModelContextProtocol.Server;

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
    private readonly IPluginDiscoveryService _pluginDiscoveryService;

    public McpToolIndexingService(
        IAzureSearchService azureSearchService,
        ILogger<McpToolIndexingService> logger,
        IPluginDiscoveryService pluginDiscoveryService)
    {
        _azureSearchService = azureSearchService;
        _logger = logger;
        _pluginDiscoveryService = pluginDiscoveryService;
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

        try
        {
            // Get all registered plugins
            var plugins = _pluginDiscoveryService.GetRegisteredPlugins();

            foreach (var plugin in plugins)
            {
                var pluginName = plugin.Metadata.Name;
                var pluginId = plugin.Metadata.Id;
                var routePrefix = plugin.Metadata.RoutePrefix;

                _logger.LogDebug("Extracting tools from plugin: {PluginName} ({PluginId})", pluginName, pluginId);

                // Get MCP tools from the plugin
                var mcpTools = plugin.GetMcpTools();

                foreach (var method in mcpTools)
                {
                    try
                    {
                        var mcpAttribute = method.GetCustomAttribute<McpServerToolAttribute>();
                        var descriptionAttribute = method.GetCustomAttribute<DescriptionAttribute>();

                        if (mcpAttribute != null)
                        {
                            // Extract method information
                            var functionName = method.Name;
                            var description = descriptionAttribute?.Description ?? $"MCP tool: {functionName}";

                            // Generate endpoint based on plugin route prefix and method name
                            var endpoint = GenerateEndpointFromMethod(routePrefix, method);

                            // Extract parameters
                            var parameters = ExtractMethodParameters(method);

                            // Generate response type information
                            var responseType = GenerateResponseTypeInfo(method);

                            documents.Add(new McpToolDocument
                            {
                                Id = idCounter.ToString(),
                                FunctionName = functionName,
                                Description = description,
                                Category = pluginId,
                                HttpMethod = "GET", // Most MCP tools are read operations
                                Endpoint = endpoint,
                                Parameters = parameters,
                                ResponseType = responseType,
                                LastUpdated = DateTimeOffset.UtcNow,
                                IsActive = true
                            });

                            idCounter++;
                            _logger.LogDebug("Added tool: {FunctionName} from plugin {PluginId}", functionName, pluginId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract tool metadata for method {MethodName} in plugin {PluginId}",
                            method.Name, pluginId);
                    }
                }
            }

            _logger.LogInformation("Extracted {Count} tool documents from {PluginCount} plugins",
                documents.Count, plugins.Count());
            return documents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract tool documents from plugins");
            return new List<McpToolDocument>();
        }
    }

    private string GenerateEndpointFromMethod(string routePrefix, MethodInfo method)
    {
        // Map MCP tool method names to actual REST controller endpoints
        var methodName = method.Name;

        // Use explicit mapping for known methods instead of automatic kebab-case conversion
        var endpointMap = new Dictionary<string, string>
        {
            // Supermarket Plugin endpoints
            {"GetProducts", "products"},
            {"GetSalesData", "sales"},
            {"GetTotalRevenue", "revenue"},
            {"GetLowStockProducts", "products/low-stock"},
            {"GetSalesByCategory", "sales/by-category"},
            {"GetInventoryStatus", "inventory/status"},
            {"GetDailySummary", "sales/daily-summary"},
            {"GetDetailedInventory", "inventory/detailed"},
            
            // GkApi Plugin endpoints
            {"GetPricesWithoutBaseItem", "prices-without-base-item"},
            {"GetLatestStatistics", "latest-statistics"},
            {"GetContentTypesSummary", "content-types"},
            {"FindArticlesByName", "articles/search"},
            {"FindArticleByContentKey", "articles/{contentKey}"},
            {"GetPluData", "plu-data"}
        };

        // Use explicit mapping if available, otherwise fall back to kebab-case conversion
        var endpoint = endpointMap.ContainsKey(methodName)
            ? endpointMap[methodName]
            : string.Concat(methodName.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x : x.ToString())).ToLowerInvariant();

        return $"/api/{routePrefix}/{endpoint}";
    }
    private string ExtractMethodParameters(MethodInfo method)
    {
        var parameters = method.GetParameters()
            .Skip(1) // Skip the first parameter (usually the data service)
            .Select(p =>
            {
                var descAttr = p.GetCustomAttribute<DescriptionAttribute>();
                var paramInfo = $"{p.Name} ({p.ParameterType.Name})";
                if (descAttr != null)
                {
                    paramInfo += $" - {descAttr.Description}";
                }
                return paramInfo;
            });

        return string.Join(", ", parameters);
    }

    private string GenerateResponseTypeInfo(MethodInfo method)
    {
        var returnType = method.ReturnType;

        // Handle Task<T> return types
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            returnType = returnType.GetGenericArguments()[0];
        }

        return returnType.Name switch
        {
            "String" => "JSON string containing the result data",
            _ => $"JSON object of type {returnType.Name}"
        };
    }
}
