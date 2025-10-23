using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Plugins.Interfaces;

/// <summary>
/// Base interface for data services in plugins
/// </summary>
public interface IDataService
{
    /// <summary>
    /// Plugin metadata
    /// </summary>
    IPluginMetadata Metadata { get; }
}

/// <summary>
/// Generic data service interface for typed operations
/// </summary>
/// <typeparam name="T">The primary entity type this service manages</typeparam>
public interface IDataService<T> : IDataService where T : class
{
    /// <summary>
    /// Get all entities of type T
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();
}

/// <summary>
/// Interface for data services that support health checks
/// </summary>
public interface IHealthCheckableDataService : IDataService
{
    /// <summary>
    /// Check if the data service is healthy and can connect to its data source
    /// </summary>
    Task<bool> IsHealthyAsync();
}