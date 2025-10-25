using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using OpenAI.Chat;
using McpServer.Configuration;
using McpServer.Models;
using McpServer.Services.Interfaces;

namespace McpServer.Services;

public interface IAIConversationService
{
    Task<AIResponse> ProcessMessageAsync(string userMessage, List<ConversationMessage> conversationHistory);
    Task<List<AvailableFunction>> GetAvailableFunctionsAsync();
}

public class AIConversationService : IAIConversationService
{
    private readonly AzureOpenAIClient _client;
    private readonly IAzureSearchService _searchService;
    private readonly ISupermarketDataService _dataService;
    private readonly ILogger<AIConversationService> _logger;
    private readonly AzureOpenAIOptions _options;
    private readonly string _systemPrompt;

    public AIConversationService(IOptions<AzureOpenAIOptions> options, IAzureSearchService searchService, ISupermarketDataService dataService, ILogger<AIConversationService> logger)
    {
        _options = options.Value;
        _searchService = searchService;
        _dataService = dataService;
        _logger = logger;

        _client = new AzureOpenAIClient(new Uri(_options.Endpoint), new Azure.AzureKeyCredential(_options.ApiKey));

        _systemPrompt = "You are a helpful assistant for a supermarket system. You have access to various tools to help answer questions about products, sales, and inventory. When users ask for data, use the appropriate tools to get real information instead of providing generic responses. Always maintain context from previous messages in the conversation.";
    }

    public async Task<AIResponse> ProcessMessageAsync(string userMessage, List<ConversationMessage> conversationHistory)
    {
        try
        {
            _logger.LogInformation("Processing message: {Message}", userMessage);

            var chatClient = _client.GetChatClient(_options.ChatModel);
            var toolsCalled = new List<string>();
            bool usedTools = false;

            // Build conversation messages
            var messages = new List<ChatMessage> { new SystemChatMessage(_systemPrompt) };

            foreach (var historyItem in conversationHistory)
            {
                if (historyItem.Role == "user")
                    messages.Add(new UserChatMessage(historyItem.Content));
                else if (historyItem.Role == "assistant")
                    messages.Add(new AssistantChatMessage(historyItem.Content));
            }

            messages.Add(new UserChatMessage(userMessage));

            // Define available functions for the AI
            var chatCompletionOptions = new ChatCompletionOptions();
            chatCompletionOptions.Tools.Add(CreateGetProductsTool());
            chatCompletionOptions.Tools.Add(CreateGetSalesDataTool());
            chatCompletionOptions.Tools.Add(CreateGetInventoryStatusTool());

            var chatCompletion = await chatClient.CompleteChatAsync(messages, chatCompletionOptions);

            int totalPromptTokens = chatCompletion.Value.Usage.InputTokenCount;
            int totalCompletionTokens = chatCompletion.Value.Usage.OutputTokenCount;

            // Check if AI wants to call a function
            if (chatCompletion.Value.FinishReason == ChatFinishReason.ToolCalls)
            {
                usedTools = true;
                var toolCall = chatCompletion.Value.ToolCalls[0];
                toolsCalled.Add(toolCall.FunctionName);

                _logger.LogInformation("AI is calling function: {FunctionName}", toolCall.FunctionName);

                // Execute the function call
                var functionResult = await ExecuteFunctionAsync(toolCall.FunctionName, toolCall.FunctionArguments);

                // Add function call and result to conversation
                messages.Add(new AssistantChatMessage(chatCompletion.Value));
                messages.Add(new ToolChatMessage(toolCall.Id, functionResult));

                // Get final response from AI with function result
                var finalCompletion = await chatClient.CompleteChatAsync(messages);

                // Add tokens from second call
                totalPromptTokens += finalCompletion.Value.Usage.InputTokenCount;
                totalCompletionTokens += finalCompletion.Value.Usage.OutputTokenCount;

                var finalResponse = finalCompletion.Value.Content[0].Text;

                _logger.LogInformation("AI Response with function data: {Response}", finalResponse);

                return CreateAIResponse(
                    finalResponse,
                    totalPromptTokens,
                    totalCompletionTokens,
                    usedTools,
                    toolsCalled
                );
            }
            else
            {
                var textResponse = chatCompletion.Value.Content[0].Text;
                _logger.LogInformation("AI Response: {Response}", textResponse);

                return CreateAIResponse(
                    textResponse,
                    totalPromptTokens,
                    totalCompletionTokens,
                    usedTools,
                    toolsCalled
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            throw;
        }
    }

    public async Task<List<AvailableFunction>> GetAvailableFunctionsAsync()
    {
        try
        {
            var searchResults = await _searchService.SearchToolsAsync("*");

            return searchResults.Select(tool => new AvailableFunction
            {
                Name = tool.FunctionName,
                Description = tool.Description,
                Parameters = string.IsNullOrEmpty(tool.Parameters) ? new List<FunctionParameter>() : ParseParameters(tool.Parameters)
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available functions");
            return new List<AvailableFunction>();
        }
    }

    private List<FunctionParameter> ParseParameters(string parametersJson)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(parametersJson);
            var parameters = new List<FunctionParameter>();

            if (jsonDoc.RootElement.TryGetProperty("properties", out var properties))
            {
                foreach (var property in properties.EnumerateObject())
                {
                    var param = new FunctionParameter
                    {
                        Name = property.Name,
                        Type = property.Value.TryGetProperty("type", out var typeElement) ? typeElement.GetString() ?? "string" : "string",
                        Description = property.Value.TryGetProperty("description", out var descElement) ? descElement.GetString() ?? "" : "",
                        Required = false
                    };
                    parameters.Add(param);
                }
            }

            return parameters;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse parameters JSON: {Json}", parametersJson);
            return new List<FunctionParameter>();
        }
    }

    private ChatTool CreateGetProductsTool()
    {
        return ChatTool.CreateFunctionTool(
            functionName: "GetProducts",
            functionDescription: "Retrieve all products from the supermarket inventory with optional filtering by category, name, or stock status",
            functionParameters: BinaryData.FromString("""
            {
                "type": "object",
                "properties": {
                    "category": {
                        "type": "string",
                        "description": "Filter products by category (optional)"
                    },
                    "name": {
                        "type": "string", 
                        "description": "Filter products by name (optional)"
                    }
                }
            }
            """)
        );
    }

    private ChatTool CreateGetSalesDataTool()
    {
        return ChatTool.CreateFunctionTool(
            functionName: "GetSalesData",
            functionDescription: "Retrieve sales transaction data within a specified date range",
            functionParameters: BinaryData.FromString("""
            {
                "type": "object",
                "properties": {
                    "startDate": {
                        "type": "string",
                        "description": "Start date for sales data (optional, format: YYYY-MM-DD)"
                    },
                    "endDate": {
                        "type": "string",
                        "description": "End date for sales data (optional, format: YYYY-MM-DD)"
                    }
                }
            }
            """)
        );
    }

    private ChatTool CreateGetInventoryStatusTool()
    {
        return ChatTool.CreateFunctionTool(
            functionName: "GetInventoryStatus",
            functionDescription: "Get a comprehensive overview of current inventory status including total products, categories, stock levels, and value metrics",
            functionParameters: BinaryData.FromString("""
            {
                "type": "object",
                "properties": {}
            }
            """)
        );
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, BinaryData functionArguments)
    {
        try
        {
            _logger.LogInformation("Executing function: {FunctionName} with args: {Args}", functionName, functionArguments.ToString());

            var args = JsonDocument.Parse(functionArguments.ToString());

            switch (functionName)
            {
                case "GetProducts":
                    var category = args.RootElement.TryGetProperty("category", out var catProp) ? catProp.GetString() : null;
                    var name = args.RootElement.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                    var allProducts = await _dataService.GetProductsAsync();

                    // Apply filters if provided
                    var filteredProducts = allProducts.AsEnumerable();
                    if (!string.IsNullOrEmpty(category))
                        filteredProducts = filteredProducts.Where(p => p.Category.Contains(category, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(name))
                        filteredProducts = filteredProducts.Where(p => p.ProductName.Contains(name, StringComparison.OrdinalIgnoreCase));

                    return JsonSerializer.Serialize(filteredProducts.ToList(), new JsonSerializerOptions { WriteIndented = true });

                case "GetSalesData":
                    var startDate = args.RootElement.TryGetProperty("startDate", out var startProp) && DateTime.TryParse(startProp.GetString(), out var start) ? start : DateTime.Now.AddDays(-30);
                    var endDate = args.RootElement.TryGetProperty("endDate", out var endProp) && DateTime.TryParse(endProp.GetString(), out var end) ? end : DateTime.Now;
                    var sales = await _dataService.GetSalesDataAsync(startDate, endDate);
                    return JsonSerializer.Serialize(sales, new JsonSerializerOptions { WriteIndented = true });

                case "GetInventoryStatus":
                    var inventory = await _dataService.GetInventoryStatusAsync();
                    return JsonSerializer.Serialize(inventory, new JsonSerializerOptions { WriteIndented = true });

                default:
                    return $"Function {functionName} is not implemented";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function: {FunctionName}", functionName);
            return $"Error executing function: {ex.Message}";
        }
    }

    private AIResponse CreateAIResponse(
        string content,
        int promptTokens,
        int completionTokens,
        bool usedTools,
        List<string> toolsCalled)
    {
        var totalTokens = promptTokens + completionTokens;

        // Calculate cost based on model pricing
        var cost = CalculateCost(_options.ChatModel, promptTokens, completionTokens);

        return new AIResponse
        {
            Content = content,
            TokensUsed = new TokenUsage
            {
                Prompt = promptTokens,
                Completion = completionTokens,
                Total = totalTokens
            },
            EstimatedCost = cost,
            Model = _options.ChatModel,
            Timestamp = DateTime.UtcNow,
            UsedTools = usedTools,
            ToolsCalled = toolsCalled
        };
    }

    private decimal CalculateCost(string model, int promptTokens, int completionTokens)
    {
        // Pricing per 1K tokens (update these based on your Azure OpenAI pricing)
        // Default to GPT-4o pricing
        decimal promptCostPer1K = 0.0025m;  // $2.50 per 1M input tokens
        decimal completionCostPer1K = 0.010m; // $10.00 per 1M output tokens

        // Adjust pricing based on model
        if (model.Contains("gpt-4o", StringComparison.OrdinalIgnoreCase))
        {
            promptCostPer1K = 0.0025m;
            completionCostPer1K = 0.010m;
        }
        else if (model.Contains("gpt-4", StringComparison.OrdinalIgnoreCase))
        {
            promptCostPer1K = 0.03m;   // $30 per 1M
            completionCostPer1K = 0.06m; // $60 per 1M
        }
        else if (model.Contains("gpt-3.5", StringComparison.OrdinalIgnoreCase))
        {
            promptCostPer1K = 0.0005m;  // $0.50 per 1M
            completionCostPer1K = 0.0015m; // $1.50 per 1M
        }

        var promptCost = (promptTokens / 1000.0m) * promptCostPer1K;
        var completionCost = (completionTokens / 1000.0m) * completionCostPer1K;

        return promptCost + completionCost;
    }
}

public class ConversationMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets the display name for the speaker based on role
    /// </summary>
    public string SpeakerName => Role.ToLower() switch
    {
        "user" => "You",
        "assistant" => "AI",
        "system" => "System",
        _ => Role
    };

    /// <summary>
    /// Returns a formatted message with speaker label
    /// </summary>
    public string FormattedMessage => $"{SpeakerName}: {Content}";
}

public class AvailableFunction
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<FunctionParameter> Parameters { get; set; } = new();
}

public class FunctionParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; }
}
