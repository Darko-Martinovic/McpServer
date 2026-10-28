using Microsoft.AspNetCore.Mvc;
using McpServer.Services;
using McpServer.Models;
using Asp.Versioning;

namespace McpServer.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IAIConversationService _conversationService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IAIConversationService conversationService,
        ILogger<ChatController> logger
    )
    {
        _conversationService = conversationService;
        _logger = logger;
    }

    [HttpPost("message")]
    public async Task<IActionResult> ProcessMessage([FromBody] ChatRequest request)
    {
        try
        {
            _logger.LogInformation("Processing chat message: {Message}", request.Message);

            var response = await _conversationService.ProcessMessageAsync(
                request.Message,
                request.History ?? new List<ConversationMessage>()
            );

            return Ok(
                new
                {
                    Success = true,
                    Speaker = "AI",
                    Response = response.Content,
                    UserMessage = request.Message,
                    UserSpeaker = "You",
                    Timestamp = DateTime.UtcNow,
                    // Token usage and cost information
                    TokensUsed = response.TokensUsed,
                    EstimatedCost = response.EstimatedCost,
                    Model = response.Model,
                    UsedTools = response.UsedTools,
                    ToolsCalled = response.ToolsCalled
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(
                500,
                new ChatResponse
                {
                    Success = false,
                    Speaker = "AI",
                    Response =
                        "I apologize, but I encountered an error while processing your request.",
                    UserMessage = request.Message,
                    UserSpeaker = "You",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                }
            );
        }
    }

    [HttpGet("functions")]
    public async Task<IActionResult> GetAvailableFunctions()
    {
        try
        {
            var functions = await _conversationService.GetAvailableFunctionsAsync();
            return Ok(
                new
                {
                    success = true,
                    functions = functions,
                    count = functions.Count,
                    timestamp = DateTime.UtcNow
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available functions");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpPost("conversation")]
    public async Task<IActionResult> GetFormattedConversation([FromBody] ChatRequest request)
    {
        try
        {
            var conversation = new List<string>();

            // Add conversation history with speaker labels
            if (request.History != null)
            {
                foreach (var message in request.History)
                {
                    conversation.Add(message.FormattedMessage);
                }
            }

            // Add current user message
            conversation.Add($"You: {request.Message}");

            // Get AI response
            var response = await _conversationService.ProcessMessageAsync(
                request.Message,
                request.History ?? new List<ConversationMessage>()
            );
            conversation.Add($"AI: {response.Content}");

            return Ok(
                new
                {
                    success = true,
                    conversation = conversation,
                    formattedConversation = string.Join("\n\n", conversation),
                    timestamp = DateTime.UtcNow,
                    // Include token usage for the entire conversation
                    tokensUsed = response.TokensUsed,
                    estimatedCost = response.EstimatedCost,
                    model = response.Model
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting formatted conversation");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ConversationMessage>? History { get; set; }
}

public class ChatResponse
{
    public bool Success { get; set; }
    public string Speaker { get; set; } = "AI";
    public string Response { get; set; } = string.Empty;
    public string UserMessage { get; set; } = string.Empty;
    public string UserSpeaker { get; set; } = "You";
    public string? Error { get; set; }
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Returns a formatted conversation display with speaker labels
    /// </summary>
    public string FormattedConversation => $"{UserSpeaker}: {UserMessage}\n\n{Speaker}: {Response}";
}
