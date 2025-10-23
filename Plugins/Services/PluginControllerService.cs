using McpServer.Plugins.Interfaces;
using McpServer.Plugins.Services;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace McpServer.Plugins.Services;

/// <summary>
/// Service for dynamically registering controllers from plugins
/// </summary>
public interface IPluginControllerService
{
    /// <summary>
    /// Register controllers from all discovered plugins
    /// </summary>
    void RegisterControllersFromPlugins(IServiceCollection services);
}

/// <summary>
/// Implementation of plugin controller service
/// </summary>
public class PluginControllerService : IPluginControllerService
{
    private readonly IPluginDiscoveryService _pluginDiscovery;
    private readonly ILogger<PluginControllerService> _logger;

    public PluginControllerService(
        IPluginDiscoveryService pluginDiscovery,
        ILogger<PluginControllerService> logger)
    {
        _pluginDiscovery = pluginDiscovery;
        _logger = logger;
    }

    public void RegisterControllersFromPlugins(IServiceCollection services)
    {
        _logger.LogInformation("Registering controllers from plugins...");

        // Get all registered plugins
        var plugins = _pluginDiscovery.GetRegisteredPlugins();
        var controllerCount = 0;

        foreach (var plugin in plugins)
        {
            try
            {
                var controllerType = plugin.GetControllerType();
                if (controllerType != null)
                {
                    _logger.LogDebug("Registering controller {ControllerName} from plugin {PluginName}",
                        controllerType.Name, plugin.Metadata.Name);

                    // Add the controller's assembly to the application parts
                    // This allows ASP.NET Core to discover and use the controller
                    var assembly = controllerType.Assembly;

                    // Configure MVC to include this assembly
                    services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
                    {
                        // The application part registration will be done during AddControllers()
                    });

                    controllerCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to register controller from plugin {PluginName}", plugin.Metadata.Name);
            }
        }

        _logger.LogInformation("Registered {ControllerCount} controllers from plugins", controllerCount);
    }
}

/// <summary>
/// Extension methods for configuring plugin controllers
/// </summary>
public static class PluginControllerExtensions
{
    /// <summary>
    /// Add controllers and configure them to include plugins
    /// </summary>
    public static IServiceCollection AddControllersWithPlugins(this IServiceCollection services)
    {
        // First, get the plugin discovery service and discover plugins
        var serviceProvider = services.BuildServiceProvider();
        var pluginDiscovery = serviceProvider.GetService<IPluginDiscoveryService>();

        if (pluginDiscovery != null)
        {
            // Get assemblies that contain controllers from plugins
            var pluginAssemblies = pluginDiscovery.GetRegisteredPlugins()
                .Select(plugin => plugin.GetControllerType()?.Assembly)
                .Where(assembly => assembly != null)
                .Distinct()
                .Cast<Assembly>()
                .ToList();

            // Add controllers with plugin assemblies
            var mvcBuilder = services.AddControllers();

            foreach (var assembly in pluginAssemblies)
            {
                mvcBuilder.AddApplicationPart(assembly);
            }

            return services;
        }

        // Fallback to standard controller registration
        services.AddControllers();
        return services;
    }
}