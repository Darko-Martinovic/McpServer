using McpServer.Models;
using McpServer.Models.ToolProxy;

namespace McpServer.Services.Interfaces;

/// <summary>
/// Service for executing MCP tools by name with automatic routing
/// </summary>
public interface IToolExecutionService
{
    /// <summary>
    /// Execute a tool by name with the provided arguments
    /// </summary>
    /// <param name="toolName">The name of the tool to execute</param>
    /// <param name="arguments">Optional arguments for the tool</param>
    /// <param name="originalQuery">Original user query for parameter extraction</param>
    /// <returns>Tool execution result</returns>
    Task<ToolExecutionResult> ExecuteToolAsync(string toolName, Dictionary<string, object>? arguments = null, string? originalQuery = null);

    /// <summary>
    /// Search for the best matching tool based on a query
    /// </summary>
    /// <param name="query">User query to search for</param>
    /// <returns>Best matching tool document or null</returns>
    Task<McpToolDocument?> SearchForToolAsync(string query);

    /// <summary>
    /// Get all available tool schemas
    /// </summary>
    /// <returns>Dictionary of plugin schemas</returns>
    Task<Dictionary<string, object>> GetToolSchemasAsync();

    /// <summary>
    /// Extract parameters from a natural language query
    /// </summary>
    /// <param name="query">The user query</param>
    /// <returns>Extracted parameters</returns>
    Dictionary<string, object> ExtractParametersFromQuery(string query);
}

