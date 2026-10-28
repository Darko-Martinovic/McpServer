using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using System.Text.Json.Serialization;

namespace McpServer.Models;

public class McpToolDocument
{
    [SimpleField(IsKey = true, IsFilterable = true)]
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [SearchableField(IsSortable = true, IsFilterable = true)]
    [JsonPropertyName("functionName")]
    public string FunctionName { get; set; } = string.Empty;

    [SearchableField(IsFilterable = true)]
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [SearchableField]
    [JsonPropertyName("category")]
    public string Category { get; set; } = "supermarket";

    [SearchableField]
    [JsonPropertyName("httpMethod")]
    public string HttpMethod { get; set; } = string.Empty;

    [SearchableField]
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    [SearchableField]
    [JsonPropertyName("parameters")]
    public string Parameters { get; set; } = string.Empty;

    [SearchableField]
    [JsonPropertyName("responseType")]
    public string ResponseType { get; set; } = string.Empty;

    [SimpleField(IsFilterable = true, IsSortable = true)]
    [JsonPropertyName("lastUpdated")]
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

    [SimpleField(IsFilterable = true)]
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}
