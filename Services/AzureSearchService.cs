using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using McpServer.Configuration;
using McpServer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace McpServer.Services;

public interface IAzureSearchService
{
    Task InitializeIndexAsync();
    Task<bool> IndexExistsAsync();
    Task UploadToolDocumentsAsync(IEnumerable<McpToolDocument> documents);
    Task DeleteAllDocumentsAsync();
    Task<IEnumerable<McpToolDocument>> GetAllDocumentsAsync();
    Task<List<McpToolDocument>> SearchToolsAsync(string searchText);
}

public class AzureSearchService : IAzureSearchService
{
    private readonly SearchIndexClient _indexClient;
    private readonly SearchClient _searchClient;
    private readonly AzureSearchOptions _options;
    private readonly ILogger<AzureSearchService> _logger;

    public AzureSearchService(IOptions<AzureSearchOptions> options, ILogger<AzureSearchService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var credential = new AzureKeyCredential(_options.ApiKey);
        _indexClient = new SearchIndexClient(new Uri(_options.Endpoint), credential);
        _searchClient = new SearchClient(new Uri(_options.Endpoint), _options.IndexName, credential);
    }

    public async Task<bool> IndexExistsAsync()
    {
        try
        {
            await _indexClient.GetIndexAsync(_options.IndexName);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }

    public async Task InitializeIndexAsync()
    {
        try
        {
            _logger.LogInformation("Checking if Azure Search index '{IndexName}' exists", _options.IndexName);

            if (await IndexExistsAsync())
            {
                _logger.LogInformation("Index '{IndexName}' already exists", _options.IndexName);
                return;
            }

            _logger.LogInformation("Creating Azure Search index '{IndexName}'", _options.IndexName);

            var definition = new SearchIndex(_options.IndexName)
            {
                Fields =
                {
                    new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                    new SearchableField("functionName") { IsSortable = true, IsFilterable = true },
                    new SearchableField("description") { IsFilterable = true },
                    new SearchableField("category"),
                    new SearchableField("httpMethod"),
                    new SearchableField("endpoint"),
                    new SearchableField("parameters"),
                    new SearchableField("responseType"),
                    new SimpleField("lastUpdated", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                    new SimpleField("isActive", SearchFieldDataType.Boolean) { IsFilterable = true }
                }
            };

            await _indexClient.CreateIndexAsync(definition);
            _logger.LogInformation("Successfully created index '{IndexName}'", _options.IndexName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure Search index '{IndexName}'", _options.IndexName);
            throw;
        }
    }

    public async Task UploadToolDocumentsAsync(IEnumerable<McpToolDocument> documents)
    {
        try
        {
            var documentList = documents.ToList();
            _logger.LogInformation("Uploading {Count} tool documents to Azure Search", documentList.Count);

            if (!documentList.Any())
            {
                _logger.LogWarning("No documents to upload");
                return;
            }

            var batch = IndexDocumentsBatch.Upload(documentList);
            var result = await _searchClient.IndexDocumentsAsync(batch);

            var successCount = result.Value.Results.Count(r => r.Succeeded);
            var failureCount = result.Value.Results.Count(r => !r.Succeeded);

            _logger.LogInformation("Upload completed: {SuccessCount} succeeded, {FailureCount} failed",
                successCount, failureCount);

            if (failureCount > 0)
            {
                foreach (var failure in result.Value.Results.Where(r => !r.Succeeded))
                {
                    _logger.LogError("Failed to upload document {Key}: {ErrorMessage}",
                        failure.Key, failure.ErrorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload tool documents to Azure Search");
            throw;
        }
    }

    public async Task DeleteAllDocumentsAsync()
    {
        try
        {
            _logger.LogInformation("Deleting all documents from Azure Search index '{IndexName}'", _options.IndexName);

            // Get all document IDs first
            var searchOptions = new SearchOptions
            {
                Select = { "id" },
                Size = 1000
            };

            var searchResponse = await _searchClient.SearchAsync<McpToolDocument>("*", searchOptions);
            var documentIds = new List<string>();

            await foreach (var docResult in searchResponse.Value.GetResultsAsync())
            {
                documentIds.Add(docResult.Document.Id);
            }

            if (!documentIds.Any())
            {
                _logger.LogInformation("No documents found to delete");
                return;
            }

            // Delete documents by ID
            var deleteActions = documentIds.Select(id =>
                IndexDocumentsAction.Delete("id", id)).ToList();

            var batch = IndexDocumentsBatch.Create(deleteActions.ToArray());
            var result = await _searchClient.IndexDocumentsAsync(batch);

            _logger.LogInformation("Deleted {Count} documents from Azure Search", documentIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete documents from Azure Search");
            throw;
        }
    }

    public async Task<IEnumerable<McpToolDocument>> GetAllDocumentsAsync()
    {
        try
        {
            var searchOptions = new SearchOptions
            {
                Size = 1000,
                IncludeTotalCount = true
            };

            var searchResult = await _searchClient.SearchAsync<McpToolDocument>("*", searchOptions);
            var documents = new List<McpToolDocument>();

            await foreach (var result in searchResult.Value.GetResultsAsync())
            {
                documents.Add(result.Document);
            }

            _logger.LogInformation("Retrieved {Count} documents from Azure Search", documents.Count);
            return documents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve documents from Azure Search");
            throw;
        }
    }

    public async Task<List<McpToolDocument>> SearchToolsAsync(string searchText)
    {
        try
        {
            var searchOptions = new SearchOptions()
            {
                IncludeTotalCount = true,
                Size = 50
            };

            var response = await _searchClient.SearchAsync<McpToolDocument>(searchText, searchOptions);
            var results = new List<McpToolDocument>();

            await foreach (var result in response.Value.GetResultsAsync())
            {
                results.Add(result.Document);
            }

            _logger.LogInformation("Found {Count} tools matching search '{SearchText}'", results.Count, searchText);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching tools with text: {SearchText}", searchText);
            throw;
        }
    }
}
