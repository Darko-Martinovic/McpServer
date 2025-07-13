using System.ComponentModel.DataAnnotations;

namespace McpServer.Configuration;

public class ApplicationModeOptions
{
    public const string SectionName = "ApplicationMode";

    /// <summary>
    /// Application startup mode: "auto", "web", "console", or "dual"
    /// - auto: Detects based on command line arguments
    /// - web: Web API mode only
    /// - console: MCP console mode only  
    /// - dual: Both Web API and MCP console mode
    /// </summary>
    [Required]
    public string Mode { get; set; } = "auto";

    /// <summary>
    /// Web API configuration
    /// </summary>
    public WebModeOptions Web { get; set; } = new();

    /// <summary>
    /// Console MCP configuration
    /// </summary>
    public ConsoleModeOptions Console { get; set; } = new();
}

public class WebModeOptions
{
    /// <summary>
    /// Enable Web API mode
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// URLs to listen on
    /// </summary>
    public string[] Urls { get; set; } = { "http://localhost:6789" };

    /// <summary>
    /// Enable Swagger/OpenAPI documentation
    /// </summary>
    public bool EnableSwagger { get; set; } = true;

    /// <summary>
    /// Enable CORS for cross-origin requests
    /// </summary>
    public bool EnableCors { get; set; } = true;

    /// <summary>
    /// HTTP port
    /// </summary>
    public int HttpPort { get; set; } = 6789;

    /// <summary>
    /// HTTPS port
    /// </summary>
    public int HttpsPort { get; set; } = 6790;
}

public class ConsoleModeOptions
{
    /// <summary>
    /// Enable MCP console mode
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Transport method for MCP
    /// </summary>
    public string Transport { get; set; } = "stdio";

    /// <summary>
    /// Enable verbose logging for MCP
    /// </summary>
    public bool VerboseLogging { get; set; } = false;
}
