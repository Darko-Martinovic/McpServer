using McpServer.Plugins.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace McpServer.Plugins.Services;

/// <summary>
/// Service for discovering and managing plugins
/// </summary>
public interface IPluginDiscoveryService
{
    /// <summary>
    /// Discover and register all plugins from the current assembly and any loaded assemblies
    /// </summary>
    Task<IEnumerable<IToolProvider>> DiscoverPluginsAsync();

    /// <summary>
    /// Register a specific plugin
    /// </summary>
    void RegisterPlugin(IToolProvider plugin);

    /// <summary>
    /// Get all registered plugins
    /// </summary>
    IEnumerable<IToolProvider> GetRegisteredPlugins();

    /// <summary>
    /// Get all MCP tools from all registered plugins
    /// </summary>
    IEnumerable<MethodInfo> GetAllMcpTools();
}

/// <summary>
/// Implementation of plugin discovery service
/// </summary>
public class PluginDiscoveryService : IPluginDiscoveryService
{
    private readonly ILogger<PluginDiscoveryService> _logger;
    private readonly List<IToolProvider> _registeredPlugins;

    public PluginDiscoveryService(ILogger<PluginDiscoveryService> logger)
    {
        _logger = logger;
        _registeredPlugins = new List<IToolProvider>();
    }

    public async Task<IEnumerable<IToolProvider>> DiscoverPluginsAsync()
    {
        _logger.LogInformation("Starting plugin discovery...");

        var discoveredPlugins = new List<IToolProvider>();

        // Get all loaded assemblies
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .ToList();

        foreach (var assembly in assemblies)
        {
            try
            {
                var plugins = await DiscoverPluginsInAssemblyAsync(assembly);
                discoveredPlugins.AddRange(plugins);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to discover plugins in assembly {AssemblyName}",
                    assembly.FullName);
            }
        }

        _logger.LogInformation("Discovered {Count} plugins", discoveredPlugins.Count);

        // Register all discovered plugins
        foreach (var plugin in discoveredPlugins)
        {
            RegisterPlugin(plugin);
        }

        return discoveredPlugins;
    }

    public void RegisterPlugin(IToolProvider plugin)
    {
        if (_registeredPlugins.Any(p => p.Metadata.Id == plugin.Metadata.Id))
        {
            _logger.LogWarning("Plugin with ID {PluginId} is already registered", plugin.Metadata.Id);
            return;
        }

        _registeredPlugins.Add(plugin);
        _logger.LogInformation("Registered plugin: {PluginName} ({PluginId}) v{Version}",
            plugin.Metadata.Name, plugin.Metadata.Id, plugin.Metadata.Version);
    }

    public IEnumerable<IToolProvider> GetRegisteredPlugins()
    {
        return _registeredPlugins.AsReadOnly();
    }

    public IEnumerable<MethodInfo> GetAllMcpTools()
    {
        return _registeredPlugins.SelectMany(plugin => plugin.GetMcpTools());
    }

    private async Task<IEnumerable<IToolProvider>> DiscoverPluginsInAssemblyAsync(Assembly assembly)
    {
        var plugins = new List<IToolProvider>();

        try
        {
            // Look for types that implement IToolProvider
            var pluginTypes = assembly.GetTypes()
                .Where(type => typeof(IToolProvider).IsAssignableFrom(type) &&
                              !type.IsInterface && !type.IsAbstract)
                .ToList();

            foreach (var pluginType in pluginTypes)
            {
                try
                {
                    var plugin = Activator.CreateInstance(pluginType) as IToolProvider;
                    if (plugin != null)
                    {
                        plugins.Add(plugin);
                        _logger.LogDebug("Discovered plugin type {TypeName} in assembly {AssemblyName}",
                            pluginType.Name, assembly.GetName().Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to instantiate plugin type {TypeName}", pluginType.Name);
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            _logger.LogWarning(ex, "Failed to load types from assembly {AssemblyName}", assembly.FullName);
        }

        return plugins;
    }
}