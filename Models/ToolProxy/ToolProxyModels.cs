using System.Text.Json.Serialization;

namespace McpServer.Models.ToolProxy;

/// <summary>
/// Request model for tool execution
/// </summary>
public class ToolCallRequest
{
    /// <summary>
    /// Name of the tool to execute
    /// </summary>
    [JsonPropertyName("tool")]
    public string Tool { get; set; } = string.Empty;

    /// <summary>
    /// Arguments for the tool
    /// </summary>
    [JsonPropertyName("arguments")]
    public Dictionary<string, object>? Arguments { get; set; }

    /// <summary>
    /// Original user input for parameter extraction
    /// </summary>
    [JsonPropertyName("originalUserInput")]
    public string? OriginalUserInput { get; set; }

    /// <summary>
    /// Query parameter (alternative to originalUserInput)
    /// </summary>
    [JsonPropertyName("query")]
    public string? Query { get; set; }
}

/// <summary>
/// Result of a tool execution
/// </summary>
public class ToolExecutionResult
{
    /// <summary>
    /// Name of the executed tool
    /// </summary>
    [JsonPropertyName("tool")]
    public string Tool { get; set; } = string.Empty;

    /// <summary>
    /// Execution result data
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>
    /// Whether the execution was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Error message if execution failed
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Plugin that handled the tool
    /// </summary>
    [JsonPropertyName("plugin")]
    public string? Plugin { get; set; }

    /// <summary>
    /// Whether this is a MongoDB response
    /// </summary>
    [JsonPropertyName("isMongoDb")]
    public bool IsMongoDb { get; set; }

    /// <summary>
    /// Timestamp of execution
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Request model for Azure Search
/// </summary>
public class SearchRequest
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;
}

/// <summary>
/// Multi-tool use request format
/// </summary>
public class MultiToolUseRequest
{
    [JsonPropertyName("tool_uses")]
    public List<ToolUse>? ToolUses { get; set; }

    [JsonPropertyName("query")]
    public string? Query { get; set; }

    [JsonPropertyName("originalUserInput")]
    public string? OriginalUserInput { get; set; }
}

/// <summary>
/// Individual tool use in multi-tool request
/// </summary>
public class ToolUse
{
    [JsonPropertyName("recipient_name")]
    public string RecipientName { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }
}

