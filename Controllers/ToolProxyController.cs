using McpServer.Models.ToolProxy;
using McpServer.Services;
using McpServer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using System.Text.Json;

namespace McpServer.Controllers;

/// <summary>
/// Unified tool proxy controller - replaces Node.js middleware
/// Handles tool routing, parameter extraction, and Azure Search integration
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
[Produces("application/json")]
public class ToolProxyController : ControllerBase
{
    private readonly IToolExecutionService _toolExecutionService;
    private readonly IAzureSearchService _azureSearchService;
    private readonly ILogger<ToolProxyController> _logger;

    public ToolProxyController(
        IToolExecutionService toolExecutionService,
        IAzureSearchService azureSearchService,
        ILogger<ToolProxyController> logger)
    {
        _toolExecutionService = toolExecutionService;
        _azureSearchService = azureSearchService;
        _logger = logger;
    }

    /// <summary>
    /// Execute an MCP tool by name
    /// </summary>
    /// <param name="request">Tool call request with tool name and optional arguments</param>
    /// <returns>Tool execution result</returns>
    [HttpPost("tool")]
    public async Task<IActionResult> CallTool([FromBody] ToolCallRequest request)
    {
        _logger.LogInformation("Received tool call request: {Tool}", request.Tool);

        try
        {
            // Handle multi_tool_use wrapper
            if (request.Tool == "multi_tool_use")
            {
                return await HandleMultiToolUseAsync(request);
            }

            // Get original query for parameter extraction
            var originalQuery = request.OriginalUserInput ?? request.Query;

            // Execute the tool
            var result = await _toolExecutionService.ExecuteToolAsync(
                request.Tool,
                request.Arguments,
                originalQuery
            );

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(500, result);
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid arguments for tool: {Tool}", request.Tool);
            return BadRequest(new ToolExecutionResult
            {
                Tool = request.Tool,
                Success = false,
                Error = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool: {Tool}", request.Tool);
            return StatusCode(500, new ToolExecutionResult
            {
                Tool = request.Tool,
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Search for tools using Azure Cognitive Search
    /// </summary>
    /// <param name="request">Search request with query</param>
    /// <returns>Search results from Azure Search</returns>
    [HttpPost("search")]
    public async Task<IActionResult> SearchTools([FromBody] SearchRequest request)
    {
        _logger.LogInformation("Received search request: {Query}", request.Query);

        try
        {
            var results = await _azureSearchService.SearchToolsAsync(request.Query);

            _logger.LogInformation("Search returned {Count} results", results.Count);

            return Ok(new
            {
                value = results,
                count = results.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching tools: {Query}", request.Query);
            return StatusCode(500, new { error = "Search error", details = ex.Message });
        }
    }

    /// <summary>
    /// Get tool schemas for all plugins
    /// </summary>
    /// <returns>Combined schemas from all plugins</returns>
    [HttpGet("tools/schema")]
    public async Task<IActionResult> GetToolSchemas()
    {
        _logger.LogInformation("Received schema request");

        try
        {
            var schemas = await _toolExecutionService.GetToolSchemasAsync();
            return Ok(schemas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tool schemas");
            return StatusCode(500, new { error = "Schema error", details = ex.Message });
        }
    }

    /// <summary>
    /// Health check for the tool proxy
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("proxy/health")]
    public IActionResult GetProxyHealth()
    {
        return Ok(new
        {
            status = "healthy",
            service = "tool-proxy",
            timestamp = DateTime.UtcNow,
            message = "Tool proxy is running in .NET (Node.js proxy replaced)"
        });
    }

    /// <summary>
    /// Handle multi_tool_use wrapper requests
    /// </summary>
    private async Task<IActionResult> HandleMultiToolUseAsync(ToolCallRequest request)
    {
        _logger.LogInformation("Handling multi_tool_use request");

        try
        {
            // Handle query-based tool discovery
            var query = request.Query ?? request.OriginalUserInput;
            if (!string.IsNullOrEmpty(query))
            {
                _logger.LogInformation("Processing multi_tool_use with query: {Query}", query);

                var searchResult = await _toolExecutionService.SearchForToolAsync(query);

                if (searchResult != null && !string.IsNullOrEmpty(searchResult.FunctionName))
                {
                    _logger.LogInformation("Found tool: {ToolName} for query: {Query}", 
                        searchResult.FunctionName, query);

                    var extractedParams = _toolExecutionService.ExtractParametersFromQuery(
                        request.OriginalUserInput ?? query
                    );

                    var result = await _toolExecutionService.ExecuteToolAsync(
                        searchResult.FunctionName,
                        extractedParams.Any() ? extractedParams : null,
                        query
                    );

                    return Ok(result);
                }
                else
                {
                    return BadRequest(new ToolExecutionResult
                    {
                        Tool = "multi_tool_use",
                        Success = false,
                        Error = $"No suitable tool found for query: {query}"
                    });
                }
            }

            // Handle tool_uses array format
            if (request.Arguments?.TryGetValue("tool_uses", out var toolUsesObj) == true)
            {
                var toolUsesJson = JsonSerializer.Serialize(toolUsesObj);
                var toolUses = JsonSerializer.Deserialize<List<ToolUse>>(toolUsesJson);

                if (toolUses != null && toolUses.Any())
                {
                    var results = new List<ToolExecutionResult>();

                    foreach (var toolUse in toolUses)
                    {
                        var actualTool = toolUse.RecipientName
                            .Replace("functions.", "")
                            .Replace("search_azure_cognitive", "GetDetailedInventory");

                        _logger.LogInformation("Processing tool: {Tool}", actualTool);

                        var result = await _toolExecutionService.ExecuteToolAsync(
                            actualTool,
                            toolUse.Parameters
                        );

                        results.Add(result);
                    }

                    return Ok(new
                    {
                        tool = "multi_tool_use",
                        data = results
                    });
                }
            }

            return BadRequest(new ToolExecutionResult
            {
                Tool = "multi_tool_use",
                Success = false,
                Error = "multi_tool_use requires either 'query' or 'tool_uses' parameter"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling multi_tool_use");
            return StatusCode(500, new ToolExecutionResult
            {
                Tool = "multi_tool_use",
                Success = false,
                Error = ex.Message
            });
        }
    }
}

