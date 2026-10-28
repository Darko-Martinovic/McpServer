using McpServer.Plugins.Catalog.Models;
using McpServer.Plugins.Interfaces;
using MongoDB.Bson;

namespace McpServer.Plugins.Catalog.Services;

/// <summary>
/// Interface for Catalog data operations
/// </summary>
public interface ICatalogDataService : IDataService<ProcessingSummary>, IHealthCheckableDataService
{
    /// <summary>
    /// Get prices without articles using the MongoDB aggregation pipeline
    /// </summary>
    Task<IEnumerable<PriceWithoutArticle>> GetPricesWithoutArticleAsync();

    /// <summary>
    /// Get the latest processing statistics from import_stats collection
    /// </summary>
    Task<ProcessingStatistics?> GetLatestStatisticsAsync();

    /// <summary>
    /// Find articles by name (case-insensitive partial match) - returns complete BSON documents
    /// </summary>
    /// <param name="name">Part of the article name to search for</param>
    Task<IEnumerable<BsonDocument>> FindArticlesByNameAsync(string name);

    /// <summary>
    /// Find article by article key (with automatic zero-padding) - returns complete BSON document
    /// </summary>
    /// <param name="articleKey">Article key (will be zero-padded to 8 digits)</param>
    Task<BsonDocument?> FindArticleByArticleKeyAsync(string articleKey);

    /// <summary>
    /// Get PLU data from Acme's back office (PosItemList) sorted by article key and sequence number
    /// </summary>
    Task<IEnumerable<PluData>> GetPluDataAsync();

    /// <summary>
    /// Get articles with ingredients (the INGREDIENTS text type) from Article
    /// Returns concatenated ingredient texts grouped by article key, text type, and language
    /// </summary>
    Task<IEnumerable<ArticleIngredient>> GetArticlesWithIngredientsAsync();
}