namespace McpServer.Plugins.Interfaces;

/// <summary>
/// Metadata interface for plugin identification and configuration
/// </summary>
public interface IPluginMetadata
{
    /// <summary>
    /// Unique identifier for the plugin
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Human-readable name of the plugin
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Plugin version
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Plugin description
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Plugin author
    /// </summary>
    string Author { get; }

    /// <summary>
    /// API route prefix for REST endpoints (e.g., "supermarket")
    /// </summary>
    string RoutePrefix { get; }
}