namespace McpServer.Configuration;

public class AzureSearchOptions
{
    public const string SectionName = "AzureSearch";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string IndexName { get; set; } = "mcp-tools";
}

public class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChatCompletionDeploymentName { get; set; } = string.Empty;
    public string EmbeddingDeploymentName { get; set; } = string.Empty;
}
