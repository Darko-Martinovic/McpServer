using McpServer.Plugins;
using McpServer.Plugins.Interfaces;
using McpServer.Plugins.GkApi.Services;
using McpServer.Plugins.GkApi.Controllers;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace McpServer.Plugins.GkApi;

/// <summary>
/// Tool provider for the GkApi plugin
/// </summary>
public class GkApiToolProvider : ToolProviderBase
{
    public override IPluginMetadata Metadata { get; } = new GkApiPluginMetadata();

    public override void ConfigureServices(IServiceCollection services)
    {
        // Register the GkApi data service
        services.AddScoped<IGkApiDataService, GkApiDataService>();
    }

    public override IEnumerable<MethodInfo> GetMcpTools()
    {
        // Return all MCP tools from GkApiMcpTools class
        return typeof(GkApiMcpTools).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.GetCustomAttribute<ModelContextProtocol.Server.McpServerToolAttribute>() != null);
    }

    public override Type? GetControllerType()
    {
        return typeof(GkApiController);
    }

    public override Task InitializeAsync(IServiceProvider serviceProvider)
    {
        // No special initialization needed for GkApi plugin
        return Task.CompletedTask;
    }
}