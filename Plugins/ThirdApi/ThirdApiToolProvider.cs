using McpServer.Plugins;
using McpServer.Plugins.Interfaces;
using McpServer.Plugins.ThirdApi.Services;
using McpServer.Plugins.ThirdApi.Controllers;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace McpServer.Plugins.ThirdApi;

/// <summary>
/// Tool provider for the ThirdApi plugin
/// </summary>
public class ThirdApiToolProvider : ToolProviderBase
{
    public override IPluginMetadata Metadata { get; } = new ThirdApiPluginMetadata();

    public override void ConfigureServices(IServiceCollection services)
    {
        // Register the ThirdApi data service
        services.AddScoped<IThirdApiDataService, ThirdApiDataService>();
    }

    public override IEnumerable<MethodInfo> GetMcpTools()
    {
        // Return all MCP tools from ThirdApiMcpTools class
        return typeof(ThirdApiMcpTools).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.GetCustomAttribute<ModelContextProtocol.Server.McpServerToolAttribute>() != null);
    }

    public override Type? GetControllerType()
    {
        return typeof(ThirdApiController);
    }

    public override Task InitializeAsync(IServiceProvider serviceProvider)
    {
        // No special initialization needed for ThirdApi plugin
        return Task.CompletedTask;
    }
}