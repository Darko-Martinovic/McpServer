using McpServer.Plugins.Interfaces;
using McpServer.Plugins.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.Reflection;

namespace McpServer.Plugins.Services;

/// <summary>
/// Service for configuring MCP server with plugins
/// </summary>
public interface IPluginMcpService
{
    /// <summary>
    /// Configure MCP server with tools from all registered plugins
    /// </summary>
    Task ConfigureMcpServerAsync(IServiceCollection services);
}

/// <summary>
/// Implementation of plugin MCP service
/// </summary>
public class PluginMcpService : IPluginMcpService
{
    private readonly IPluginDiscoveryService _pluginDiscovery;
    private readonly ILogger<PluginMcpService> _logger;

    public PluginMcpService(
        IPluginDiscoveryService pluginDiscovery,
        ILogger<PluginMcpService> logger)
    {
        _pluginDiscovery = pluginDiscovery;
        _logger = logger;
    }

    public async Task ConfigureMcpServerAsync(IServiceCollection services)
    {
        _logger.LogInformation("Configuring MCP server with plugins...");

        // Discover all plugins
        var plugins = await _pluginDiscovery.DiscoverPluginsAsync();

        _logger.LogInformation("Found {PluginCount} plugins for MCP server", plugins.Count());

        // Configure services for each plugin
        foreach (var plugin in plugins)
        {
            try
            {
                _logger.LogDebug("Configuring services for plugin {PluginName}", plugin.Metadata.Name);
                plugin.ConfigureServices(services);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to configure services for plugin {PluginName}", plugin.Metadata.Name);
            }
        }

        // Get all MCP tools from all plugins
        var allMcpTools = _pluginDiscovery.GetAllMcpTools().ToList();
        _logger.LogInformation("Registering {ToolCount} MCP tools from all plugins", allMcpTools.Count);

        // Configure MCP server with all tools
        services.AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly(); // For now, keep using assembly-based discovery

        _logger.LogInformation("MCP server configuration completed");
    }
}