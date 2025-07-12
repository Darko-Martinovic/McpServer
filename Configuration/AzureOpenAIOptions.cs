public class AzureOpenAIOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChatCompletionDeploymentName { get; set; } = string.Empty;
    public string EmbeddingDeploymentName { get; set; } = string.Empty;
    public string ChatModel => ChatCompletionDeploymentName;
}