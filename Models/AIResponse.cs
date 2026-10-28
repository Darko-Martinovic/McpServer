namespace McpServer.Models;

/// <summary>
/// Response model for AI conversation including token usage and cost information
/// </summary>
public class AIResponse
{
    /// <summary>
    /// The AI assistant's response text
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Token usage information for this response
    /// </summary>
    public TokenUsage TokensUsed { get; set; } = new();

    /// <summary>
    /// Estimated cost in USD for this API call
    /// </summary>
    public decimal EstimatedCost { get; set; }

    /// <summary>
    /// The AI model used for this response
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the response was generated
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether tool calls were used in generating this response
    /// </summary>
    public bool UsedTools { get; set; }

    /// <summary>
    /// Names of tools that were called (if any)
    /// </summary>
    public List<string> ToolsCalled { get; set; } = new();
}

/// <summary>
/// Token usage breakdown for an AI API call
/// </summary>
public class TokenUsage
{
    /// <summary>
    /// Number of tokens in the prompt (input)
    /// </summary>
    public int Prompt { get; set; }

    /// <summary>
    /// Number of tokens in the completion (output)
    /// </summary>
    public int Completion { get; set; }

    /// <summary>
    /// Total tokens used (prompt + completion)
    /// </summary>
    public int Total { get; set; }
}
