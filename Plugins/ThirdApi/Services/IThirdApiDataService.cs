using McpServer.Plugins.ThirdApi.Models;
using McpServer.Plugins.Interfaces;
using MongoDB.Bson;

namespace McpServer.Plugins.ThirdApi.Services;

/// <summary>
/// Interface for ThirdApi data operations
/// </summary>
public interface IThirdApiDataService : IDataService<ProcessingSummary>, IHealthCheckableDataService
{
    /// <summary>
    /// Get prices without base items using the MongoDB aggregation pipeline
    /// </summary>
    Task<IEnumerable<PriceWithoutBaseItem>> GetPricesWithoutBaseItemAsync();

    /// <summary>
    /// Get the latest processing statistics from Summary collection
    /// </summary>
    Task<ProcessingStatistics?> GetLatestStatisticsAsync();

    /// <summary>
    /// Find articles by name (case-insensitive partial match) - returns complete BSON documents
    /// </summary>
    /// <param name="name">Part of the article name to search for</param>
    Task<IEnumerable<BsonDocument>> FindArticlesByNameAsync(string name);

    /// <summary>
    /// Find article by content key (with automatic zero-padding) - returns complete BSON document
    /// </summary>
    /// <param name="contentKey">Content key (will be zero-padded to 18 digits)</param>
    Task<BsonDocument?> FindArticleByContentKeyAsync(string contentKey);

    /// <summary>
    /// Get PLU data from SAP Fiori (DynamicTableauItemListDO) sorted by content key and sequence number
    /// </summary>
    Task<IEnumerable<PluData>> GetPluDataAsync();

    /// <summary>
    /// Get articles with ingredients (INGR or IN text classes) from BaseItemDO
    /// Returns concatenated ingredient texts grouped by content key, text class, and language
    /// </summary>
    Task<IEnumerable<ArticleIngredient>> GetArticlesWithIngredientsAsync();
}