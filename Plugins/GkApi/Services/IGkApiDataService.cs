using McpServer.Plugins.GkApi.Models;
using McpServer.Plugins.Interfaces;

namespace McpServer.Plugins.GkApi.Services;

/// <summary>
/// Interface for GkApi data operations
/// </summary>
public interface IGkApiDataService : IDataService<ProcessingSummary>, IHealthCheckableDataService
{
    /// <summary>
    /// Get prices without base items using the MongoDB aggregation pipeline
    /// </summary>
    Task<IEnumerable<PriceWithoutBaseItem>> GetPricesWithoutBaseItemAsync();

    /// <summary>
    /// Get the latest processing statistics from Summary collection
    /// </summary>
    Task<ProcessingStatistics?> GetLatestStatisticsAsync();
}