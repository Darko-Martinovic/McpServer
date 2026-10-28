using McpServer.Plugins;
using McpServer.Plugins.Interfaces;
using McpServer.Plugins.Catalog.Services;
using McpServer.Plugins.Catalog.Controllers;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace McpServer.Plugins.Catalog;

/// <summary>
/// Tool provider for the Catalog plugin
/// </summary>
public class CatalogToolProvider : ToolProviderBase
{
    public override IPluginMetadata Metadata { get; } = new CatalogPluginMetadata();

    public override void ConfigureServices(IServiceCollection services)
    {
        // Register the Catalog data service
        services.AddScoped<ICatalogDataService, CatalogDataService>();
    }

    public override IEnumerable<MethodInfo> GetMcpTools()
    {
        // Return all MCP tools from CatalogMcpTools class
        return typeof(CatalogMcpTools).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.GetCustomAttribute<ModelContextProtocol.Server.McpServerToolAttribute>() != null);
    }

    public override Type? GetControllerType()
    {
        return typeof(CatalogController);
    }

    public override Task InitializeAsync(IServiceProvider serviceProvider)
    {
        // No special initialization needed for Catalog plugin
        return Task.CompletedTask;
    }
}