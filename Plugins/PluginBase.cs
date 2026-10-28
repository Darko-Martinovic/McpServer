using McpServer.Plugins.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using System.Reflection;

namespace McpServer.Plugins;

/// <summary>
/// Base implementation for plugin metadata
/// </summary>
public abstract class PluginMetadataBase : IPluginMetadata
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract string Version { get; }
    public abstract string Description { get; }
    public abstract string Author { get; }
    public abstract string RoutePrefix { get; }
}

/// <summary>
/// Base implementation for tool providers
/// </summary>
public abstract class ToolProviderBase : IToolProvider
{
    public abstract IPluginMetadata Metadata { get; }

    public virtual void ConfigureServices(IServiceCollection services)
    {
        // Default implementation - plugins can override
    }

    public virtual IEnumerable<MethodInfo> GetMcpTools()
    {
        // Find all static methods in this assembly with McpServerTool attribute
        var assembly = GetType().Assembly;
        return assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(method => method.GetCustomAttribute<McpServerToolAttribute>() != null);
    }

    public virtual Type? GetControllerType()
    {
        // Default implementation - look for a controller in the same assembly
        var assembly = GetType().Assembly;
        var controllerTypes = assembly.GetTypes()
            .Where(type => type.Name.EndsWith("Controller") &&
                          type.IsSubclassOf(typeof(Microsoft.AspNetCore.Mvc.ControllerBase)))
            .ToArray();

        return controllerTypes.Length == 1 ? controllerTypes[0] : null;
    }

    public virtual Task InitializeAsync(IServiceProvider serviceProvider)
    {
        // Default implementation - plugins can override for custom initialization
        return Task.CompletedTask;
    }
}