using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace McpServer.Plugins.Interfaces;

/// <summary>
/// Interface for plugin providers that can register tools and services
/// </summary>
public interface IToolProvider
{
    /// <summary>
    /// Plugin metadata
    /// </summary>
    IPluginMetadata Metadata { get; }

    /// <summary>
    /// Configure services for dependency injection
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Get all MCP tool methods from this provider
    /// </summary>
    /// <returns>Collection of methods decorated with McpServerTool attribute</returns>
    IEnumerable<MethodInfo> GetMcpTools();

    /// <summary>
    /// Get the controller type for REST API endpoints
    /// </summary>
    /// <returns>Controller type, or null if no controller is provided</returns>
    Type? GetControllerType();

    /// <summary>
    /// Initialize the plugin (called after services are configured)
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving dependencies</param>
    Task InitializeAsync(IServiceProvider serviceProvider);
}