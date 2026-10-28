using McpServer.Plugins.Catalog.Models;
using McpServer.Plugins.Interfaces;
using McpServer.Plugins;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Bson;

namespace McpServer.Plugins.Catalog.Services;

/// <summary>
/// Catalog data service implementation using MongoDB
/// </summary>
public class CatalogDataService : ICatalogDataService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<CatalogDataService> _logger;
    private readonly IMongoCollection<BsonDocument> _articlesCollection;
    private readonly IMongoCollection<ProcessingSummary> _summaryCollection;

    public IPluginMetadata Metadata { get; }

    public CatalogDataService(ILogger<CatalogDataService> logger)
    {
        _logger = logger;

        // Initialize metadata
        Metadata = new CatalogPluginMetadata();

        // Initialize MongoDB connection
        var connectionString = "mongodb://localhost:27017"; // Default MongoDB connection
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase("Catalog");

        _articlesCollection = _database.GetCollection<BsonDocument>("articles");
        _summaryCollection = _database.GetCollection<ProcessingSummary>("import_stats");

        _logger.LogInformation("CatalogDataService initialized with MongoDB database: Catalog");
    }

    public async Task<IEnumerable<ProcessingSummary>> GetAllAsync()
    {
        try
        {
            var summaries = await _summaryCollection.Find(Builders<ProcessingSummary>.Filter.Empty)
                .SortByDescending(s => s.ProcessedAt)
                .ToListAsync();
            return summaries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all processing summaries");
            return new List<ProcessingSummary>();
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            // Try to ping the database
            await _database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MongoDB health check failed");
            return false;
        }
    }

    public async Task<IEnumerable<PriceWithoutArticle>> GetPricesWithoutArticleAsync()
    {
        try
        {
            _logger.LogInformation("Executing prices without article aggregation pipeline");

            var pipeline = new BsonDocument[]
            {
                // First, get all Article articleKeys
                new BsonDocument("$match", new BsonDocument
                {
                    ["envelope.contentType"] = new BsonDocument("$in", new BsonArray
                    {
                        "com.acme.catalog.Article",
                        "com.acme.catalog.ItemPrice"
                    })
                }),
                
                // Group by articleKey
                new BsonDocument("$group", new BsonDocument
                {
                    ["_id"] = "$envelope.articleKey",
                    ["types"] = new BsonDocument("$addToSet", "$envelope.contentType"),
                    ["docs"] = new BsonDocument("$push", "$$ROOT")
                }),
                
                // Match only ItemPrice without Article
                new BsonDocument("$match", new BsonDocument
                {
                    ["types"] = new BsonDocument
                    {
                        ["$all"] = new BsonArray { "com.acme.catalog.ItemPrice" },
                        ["$nin"] = new BsonArray { "com.acme.catalog.Article" }
                    }
                }),
                
                // Unwind docs
                new BsonDocument("$unwind", "$docs"),
                
                // Match ItemPrice documents
                new BsonDocument("$match", new BsonDocument
                {
                    ["docs.envelope.contentType"] = "com.acme.catalog.ItemPrice"
                }),
                
                // Replace root with docs
                new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$docs")),
                
                // Extract content object
                new BsonDocument("$replaceRoot", new BsonDocument("newRoot", new BsonDocument("$mergeObjects", new BsonArray
                {
                    "$$ROOT",
                    new BsonDocument("contentObject", new BsonDocument("$arrayElemAt", new BsonArray
                    {
                        new BsonDocument("$objectToArray", "$envelope.content"),
                        0
                    }))
                }))),
                
                // Unwind prices
                new BsonDocument("$unwind", "$contentObject.v.prices"),
                
                // Extract unit object
                new BsonDocument("$replaceRoot", new BsonDocument("newRoot", new BsonDocument("$mergeObjects", new BsonArray
                {
                    "$$ROOT",
                    new BsonDocument("unit", new BsonDocument("$arrayElemAt", new BsonArray
                    {
                        new BsonDocument("$objectToArray", "$contentObject.v.prices"),
                        0
                    }))
                }))),
                
                // Unwind priceList
                new BsonDocument("$unwind", "$unit.v.priceList"),
                
                // Extract price object
                new BsonDocument("$replaceRoot", new BsonDocument("newRoot", new BsonDocument("$mergeObjects", new BsonArray
                {
                    "$$ROOT",
                    new BsonDocument("priceObject", new BsonDocument("$arrayElemAt", new BsonArray
                    {
                        new BsonDocument("$objectToArray", "$unit.v.priceList"),
                        0
                    }))
                }))),
                
                // Project final fields
                new BsonDocument("$project", new BsonDocument
                {
                    ["_id"] = 0,
                    ["articleKey"] = "$envelope.articleKey",
                    ["unitCode"] = "$unit.v.key.unitCode",
                    ["priceSource"] = "$contentObject.v.priceSource",
                    ["priceType"] = "$priceObject.v.key.priceType",
                    ["validFrom"] = new BsonDocument("$ifNull", new BsonArray { "$priceObject.v.key.validFrom", BsonNull.Value }),
                    ["validTo"] = new BsonDocument("$ifNull", new BsonArray { "$priceObject.v.validTo", BsonNull.Value }),
                    ["priceAmount"] = "$priceObject.v.priceAmount",
                    ["packQuantity"] = "$priceObject.v.packQuantity"
                })
            };

            var result = await _articlesCollection.AggregateAsync<PriceWithoutArticle>(pipeline);
            var prices = await result.ToListAsync();

            _logger.LogInformation("Found {Count} prices without articles", prices.Count);
            return prices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get prices without article");
            return new List<PriceWithoutArticle>();
        }
    }

    public async Task<ProcessingStatistics?> GetLatestStatisticsAsync()
    {
        try
        {
            _logger.LogInformation("Getting latest processing statistics from import_stats collection");

            var latestSummary = await _summaryCollection
                .Find(Builders<ProcessingSummary>.Filter.Empty)
                .SortByDescending(s => s.ProcessedAt)
                .FirstOrDefaultAsync();

            if (latestSummary != null)
            {
                _logger.LogInformation("Found latest summary processed at {ProcessedAt} with {TypeCount} content types",
                    latestSummary.ProcessedAt, latestSummary.Statistics.ContentTypes.Count);
                return latestSummary.Statistics;
            }

            _logger.LogWarning("No processing summary found in import_stats collection");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get latest statistics");
            return null;
        }
    }

    public async Task<IEnumerable<BsonDocument>> FindArticlesByNameAsync(string name)
    {
        try
        {
            _logger.LogInformation("Searching for articles with name containing: {Name}", name);

            var pipeline = new[]
            {
                // Match only Article documents
                new BsonDocument("$match", new BsonDocument
                {
                    ["envelope.contentType"] = "com.acme.catalog.Article"
                }),
                
                // Match name with regex (case-insensitive)
                new BsonDocument("$match", new BsonDocument
                {
                    ["$expr"] = new BsonDocument("$regexMatch", new BsonDocument
                    {
                        ["input"] = new BsonDocument("$getField", new BsonDocument
                        {
                            ["field"] = "name",
                            ["input"] = new BsonDocument("$getField", new BsonDocument
                            {
                                ["field"] = "com.acme.catalog.Article",
                                ["input"] = "$envelope.content"
                            })
                        }),
                        ["regex"] = name,
                        ["options"] = "i"
                    })
                })
            };

            var result = await _articlesCollection.AggregateAsync<BsonDocument>(pipeline);
            var articles = await result.ToListAsync();

            _logger.LogInformation("Found {Count} articles matching name: {Name}", articles.Count, name);
            return articles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find articles by name: {Name}", name);
            return new List<BsonDocument>();
        }
    }

    public async Task<BsonDocument?> FindArticleByArticleKeyAsync(string articleKey)
    {
        try
        {
            // Pad the article key with zeros to 8 digits
            string paddedArticleKey = articleKey.PadLeft(8, '0');

            _logger.LogInformation("Searching for article with article key: {ArticleKey} (padded: {PaddedKey})",
                articleKey, paddedArticleKey);

            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("envelope.contentType",
                    "com.acme.catalog.Article"),
                Builders<BsonDocument>.Filter.Eq("envelope.articleKey", paddedArticleKey)
            );

            var document = await _articlesCollection.Find(filter).FirstOrDefaultAsync();

            if (document == null)
            {
                _logger.LogWarning("No article found with article key: {ArticleKey}", paddedArticleKey);
                return null;
            }

            _logger.LogInformation("Found article with article key: {ArticleKey}", paddedArticleKey);
            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find article by article key: {ArticleKey}", articleKey);
            return null;
        }
    }

    public async Task<IEnumerable<PluData>> GetPluDataAsync()
    {
        try
        {
            _logger.LogInformation("Executing PLU data aggregation pipeline");

            var pipeline = new BsonDocument[]
            {
                // Match PosItemList documents
                new BsonDocument("$match", new BsonDocument
                {
                    ["envelope.contentType"] = "com.acme.catalog.PosItemList"
                }),
                
                // Extract the PosItemList from the content
                new BsonDocument("$addFields", new BsonDocument
                {
                    ["posItemList"] = new BsonDocument("$getField", new BsonDocument
                    {
                        ["field"] = "com.acme.catalog.PosItemList",
                        ["input"] = "$envelope.content"
                    })
                }),
                
                // Unwind the entries array to process each item
                new BsonDocument("$unwind", "$posItemList.entries"),
                
                // Extract the PosItemListEntry from each item
                new BsonDocument("$addFields", new BsonDocument
                {
                    ["posItemEntry"] = new BsonDocument("$getField", new BsonDocument
                    {
                        ["field"] = "com.acme.catalog.PosItemListEntry",
                        ["input"] = "$posItemList.entries"
                    })
                }),
                
                // Project the desired fields
                new BsonDocument("$project", new BsonDocument
                {
                    ["_id"] = 0,
                    ["articleKey"] = "$envelope.articleKey",
                    ["storeGroupId"] = "$posItemList.key.storeGroupId",
                    ["groupId"] = "$posItemList.key.listId",
                    ["groupDescription"] = "$posItemList.description",
                    ["itemCode"] = "$posItemEntry.itemCode",
                    ["sequenceNumber"] = "$posItemEntry.sequenceNumber",
                    ["lastUpdateTimestamp"] = "$posItemList.lastUpdateTimestamp"
                }),
                
                // Sort by article key and then by sequence number within group
                new BsonDocument("$sort", new BsonDocument
                {
                    ["articleKey"] = 1,
                    ["sequenceNumber"] = 1
                })
            };

            var result = await _articlesCollection.AggregateAsync<PluData>(pipeline);
            var pluData = await result.ToListAsync();

            _logger.LogInformation("Found {Count} PLU data entries", pluData.Count);
            return pluData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get PLU data");
            return new List<PluData>();
        }
    }

    public async Task<IEnumerable<ArticleIngredient>> GetArticlesWithIngredientsAsync()
    {
        try
        {
            _logger.LogInformation("Executing articles with ingredients aggregation pipeline");

            var pipeline = new BsonDocument[]
            {
                // Stage 1: Filter for Article content type
                new BsonDocument("$match", new BsonDocument
                {
                    ["envelope.contentType"] = "com.acme.catalog.Article"
                }),

                // Stage 2: Extract the Article into a workable field using $getField
                new BsonDocument("$addFields", new BsonDocument
                {
                    ["article"] = new BsonDocument("$getField", new BsonDocument
                    {
                        ["field"] = "com.acme.catalog.Article",
                        ["input"] = "$envelope.content"
                    }),
                    ["articleKey"] = "$envelope.articleKey"
                }),

                // Stage 3: Filter documents that have units (exists and not empty)
                new BsonDocument("$match", new BsonDocument
                {
                    ["article.units"] = new BsonDocument
                    {
                        ["$exists"] = true,
                        ["$ne"] = new BsonArray()
                    }
                }),

                // Stage 4: Unwind the units
                new BsonDocument("$unwind", "$article.units"),

                // Stage 5: Extract UnitOfMeasure content using $getField
                new BsonDocument("$addFields", new BsonDocument
                {
                    ["unitItem"] = new BsonDocument("$getField", new BsonDocument
                    {
                        ["field"] = "com.acme.catalog.UnitOfMeasure",
                        ["input"] = "$article.units"
                    })
                }),

                // Stage 6: Filter documents that have texts (exists and not empty)
                new BsonDocument("$match", new BsonDocument
                {
                    ["unitItem.texts"] = new BsonDocument
                    {
                        ["$exists"] = true,
                        ["$ne"] = new BsonArray()
                    }
                }),

                // Stage 7: Unwind the texts
                new BsonDocument("$unwind", "$unitItem.texts"),

                // Stage 8: Extract UnitOfMeasureText content using $getField
                new BsonDocument("$addFields", new BsonDocument
                {
                    ["textItem"] = new BsonDocument("$getField", new BsonDocument
                    {
                        ["field"] = "com.acme.catalog.UnitOfMeasureText",
                        ["input"] = "$unitItem.texts"
                    })
                }),

                // Stage 9: Filter for the INGREDIENTS text type only
                new BsonDocument("$match", new BsonDocument
                {
                    ["textItem.key.textType"] = new BsonDocument("$in", new BsonArray { "INGREDIENTS" })
                }),

                // Stage 10: Project needed fields for grouping
                new BsonDocument("$project", new BsonDocument
                {
                    ["articleKey"] = 1,
                    ["name"] = "$article.name",
                    ["textType"] = "$textItem.key.textType",
                    ["language"] = "$textItem.key.language",
                    ["lineNumber"] = "$textItem.key.lineNumber",
                    ["text"] = "$textItem.text"
                }),

                // Stage 11: Sort by articleKey, textType, language, and lineNumber
                new BsonDocument("$sort", new BsonDocument
                {
                    ["articleKey"] = 1,
                    ["textType"] = 1,
                    ["language"] = 1,
                    ["lineNumber"] = 1
                }),

                // Stage 12: Group by articleKey, textType, and language - concatenate texts
                new BsonDocument("$group", new BsonDocument
                {
                    ["_id"] = new BsonDocument
                    {
                        ["articleKey"] = "$articleKey",
                        ["name"] = "$name",
                        ["textType"] = "$textType",
                        ["language"] = "$language"
                    },
                    ["texts"] = new BsonDocument("$push", "$text")
                }),

                // Stage 13: Concatenate the text array with space separator
                new BsonDocument("$project", new BsonDocument
                {
                    ["_id"] = 0,
                    ["articleKey"] = "$_id.articleKey",
                    ["name"] = "$_id.name",
                    ["textType"] = "$_id.textType",
                    ["language"] = "$_id.language",
                    ["text"] = new BsonDocument("$reduce", new BsonDocument
                    {
                        ["input"] = "$texts",
                        ["initialValue"] = "",
                        ["in"] = new BsonDocument("$cond", new BsonDocument
                        {
                            ["if"] = new BsonDocument("$eq", new BsonArray { "$$value", "" }),
                            ["then"] = "$$this",
                            ["else"] = new BsonDocument("$concat", new BsonArray { "$$value", " ", "$$this" })
                        })
                    })
                }),

                // Stage 14: Final sort
                new BsonDocument("$sort", new BsonDocument
                {
                    ["articleKey"] = 1,
                    ["textType"] = 1,
                    ["language"] = 1
                })
            };

            var result = await _articlesCollection.AggregateAsync<ArticleIngredient>(pipeline);
            var articlesWithIngredients = await result.ToListAsync();

            _logger.LogInformation("Found {Count} articles with ingredients", articlesWithIngredients.Count);
            return articlesWithIngredients;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get articles with ingredients");
            return new List<ArticleIngredient>();
        }
    }
}

/// <summary>
/// Metadata for the Catalog plugin
/// </summary>
public class CatalogPluginMetadata : PluginMetadataBase
{
    public override string Id => "catalog";
    public override string Name => "Catalog Plugin";
    public override string Version => "1.0.0";
    public override string Description => "A plugin for catalog data operations using MongoDB";
    public override string Author => "McpServer Team";
    public override string RoutePrefix => "catalog";
}