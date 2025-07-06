using Microsoft.Extensions.Logging;

namespace McpServer.Services;

/// <summary>
/// Helper class for consistent logging patterns across the MCP server
/// </summary>
public static class LoggingHelper
{
    /// <summary>
    /// Creates a unique request ID for tracking MCP client requests
    /// </summary>
    /// <returns>8-character request ID</returns>
    public static string CreateRequestId() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Logs the start of an MCP tool execution
    /// </summary>
    public static void LogMcpToolStart(ILogger logger, string requestId, string toolName, params (string key, object value)[] parameters)
    {
        var paramString = parameters.Length > 0
            ? $" with parameters: {string.Join(", ", parameters.Select(p => $"{p.key}={p.value}"))}"
            : "";

        logger.LogInformation("[{RequestId}] MCP Tool '{ToolName}' called by client{Parameters}",
            requestId, toolName, paramString);
    }

    /// <summary>
    /// Logs the successful completion of an MCP tool execution
    /// </summary>
    public static void LogMcpToolSuccess(ILogger logger, string requestId, string toolName, int resultCount, long elapsedMs)
    {
        logger.LogInformation("[{RequestId}] MCP Tool '{ToolName}' completed successfully. Returned {Count} results in {ElapsedMs}ms",
            requestId, toolName, resultCount, elapsedMs);
    }

    /// <summary>
    /// Logs the start of a database operation
    /// </summary>
    public static void LogDatabaseOperationStart(ILogger logger, string requestId, string operation, string sql, params (string key, object value)[] parameters)
    {
        var paramString = parameters.Length > 0
            ? $" with parameters: {string.Join(", ", parameters.Select(p => $"{p.key}={p.value}"))}"
            : "";

        logger.LogInformation("[{RequestId}] Executing SQL for {Operation}: {Sql}{Parameters}",
            requestId, operation, sql, paramString);
    }

    /// <summary>
    /// Logs the successful completion of a database operation
    /// </summary>
    public static void LogDatabaseOperationSuccess(ILogger logger, string requestId, string operation, int resultCount, long elapsedMs, decimal? revenue = null)
    {
        var revenueInfo = revenue.HasValue ? $", Revenue: ${revenue:N2}" : "";
        logger.LogInformation("[{RequestId}] {Operation} completed successfully. Retrieved {Count} records{RevenueInfo} in {ElapsedMs}ms",
            requestId, operation, resultCount, revenueInfo, elapsedMs);
    }

    /// <summary>
    /// Logs database operation errors
    /// </summary>
    public static void LogDatabaseError(ILogger logger, Exception ex, string requestId, string operation)
    {
        logger.LogError(ex, "[{RequestId}] Database error in {Operation}: {Message}", requestId, operation, ex.Message);
    }

    /// <summary>
    /// Logs MCP tool errors
    /// </summary>
    public static void LogMcpToolError(ILogger logger, Exception ex, string requestId, string toolName)
    {
        logger.LogError(ex, "[{RequestId}] MCP Tool '{ToolName}' failed: {Message}", requestId, toolName, ex.Message);
    }

    /// <summary>
    /// Logs validation errors (like invalid date formats)
    /// </summary>
    public static void LogValidationError(ILogger logger, string requestId, string toolName, string error, params (string key, object value)[] parameters)
    {
        var paramString = parameters.Length > 0
            ? $" Parameters: {string.Join(", ", parameters.Select(p => $"{p.key}={p.value}"))}"
            : "";

        logger.LogWarning("[{RequestId}] MCP Tool '{ToolName}' validation failed: {Error}{Parameters}",
            requestId, toolName, error, paramString);
    }
}