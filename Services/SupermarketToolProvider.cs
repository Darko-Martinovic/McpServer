using McpServer.Controllers;
using McpServer.Plugins;
using McpServer.Plugins.Interfaces;
using McpServer.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace McpServer.Services;

/// <summary>
/// Tool provider for the Supermarket plugin
/// </summary>
public class SupermarketToolProvider : ToolProviderBase
{
    public override IPluginMetadata Metadata { get; } = new SupermarketPluginMetadata();

    public override void ConfigureServices(IServiceCollection services)
    {
        // The SupermarketDataService is already registered in Program.cs
        // This plugin just provides the tools and controller
    }

    public override IEnumerable<MethodInfo> GetMcpTools()
    {
        // Return all MCP tools from SupermarketMcpTools class
        return typeof(SupermarketMcpTools).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.GetCustomAttribute<ModelContextProtocol.Server.McpServerToolAttribute>() != null);
    }

    public override Type? GetControllerType()
    {
        return typeof(SupermarketController);
    }

    public override Task InitializeAsync(IServiceProvider serviceProvider)
    {
        // No special initialization needed for Supermarket plugin
        return Task.CompletedTask;
    }
}