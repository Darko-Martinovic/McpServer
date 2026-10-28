using McpServer.Plugins;

namespace McpServer.Services;

/// <summary>
/// Metadata for the Supermarket plugin
/// </summary>
public class SupermarketPluginMetadata : PluginMetadataBase
{
    public override string Id => "supermarket";
    public override string Name => "Supermarket Management Plugin";
    public override string Version => "1.0.0";
    public override string Description => "A plugin for managing supermarket inventory, sales, and analytics";
    public override string Author => "McpServer Team";
    public override string RoutePrefix => "supermarket";
}