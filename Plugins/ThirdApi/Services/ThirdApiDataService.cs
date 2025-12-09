using McpServer.Plugins.ThirdApi.Models;
using McpServer.Plugins.Interfaces;
using McpServer.Plugins;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Bson;

namespace McpServer.Plugins.ThirdApi.Services;

/// <summary>
/// ThirdApi data service implementation using MongoDB
/// </summary>
public class ThirdApiDataService : IThirdApiDataService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<ThirdApiDataService> _logger;
    private readonly IMongoCollection<BsonDocument> _pumpCollection;
    private readonly IMongoCollection<ProcessingSummary> _summaryCollection;

    public IPluginMetadata Metadata { get; }

    public ThirdApiDataService(ILogger<ThirdApiDataService> logger)
    {
        _logger = logger;

        // Initialize metadata
        Metadata = new ThirdApiPluginMetadata();

        // Initialize MongoDB connection
        var connectionString = "mongodb://localhost:27017"; // Default MongoDB connection
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase("ThirdApi");

        _pumpCollection = _database.GetCollection<BsonDocument>("Pump");
        _summaryCollection = _database.GetCollection<ProcessingSummary>("Summary");

        _logger.LogInformation("ThirdApiDataService initialized with MongoDB database: ThirdApi");
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

    public async Task<IEnumerable<PriceWithoutBaseItem>> GetPricesWithoutBaseItemAsync()
    {
        try
        {
            _logger.LogInformation("Executing prices without base item aggregation pipeline");

            var pipeline = new BsonDocument[]
            {
                // First, get all BaseItemDO contentKeys
                new BsonDocument("$match", new BsonDocument
                {
                    ["transportElement.contentType"] = new BsonDocument("$in", new BsonArray
                    {
                        "com.gk_software.gkr.api.server.md.item.dto.dom.BaseItemDO",
                        "com.gk_software.gkr.api.server.md.item_prices.dto.dom.ItemPricesDO"
                    })
                }),
                
                // Group by contentKey
                new BsonDocument("$group", new BsonDocument
                {
                    ["_id"] = "$transportElement.contentKey",
                    ["types"] = new BsonDocument("$addToSet", "$transportElement.contentType"),
                    ["docs"] = new BsonDocument("$push", "$$ROOT")
                }),
                
                // Match only ItemPricesDO without BaseItemDO
                new BsonDocument("$match", new BsonDocument
                {
                    ["types"] = new BsonDocument
                    {
                        ["$all"] = new BsonArray { "com.gk_software.gkr.api.server.md.item_prices.dto.dom.ItemPricesDO" },
                        ["$nin"] = new BsonArray { "com.gk_software.gkr.api.server.md.item.dto.dom.BaseItemDO" }
                    }
                }),
                
                // Unwind docs
                new BsonDocument("$unwind", "$docs"),
                
                // Match ItemPricesDO documents
                new BsonDocument("$match", new BsonDocument
                {
                    ["docs.transportElement.contentType"] = "com.gk_software.gkr.api.server.md.item_prices.dto.dom.ItemPricesDO"
                }),
                
                // Replace root with docs
                new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$docs")),
                
                // Extract content object
                new BsonDocument("$replaceRoot", new BsonDocument("newRoot", new BsonDocument("$mergeObjects", new BsonArray
                {
                    "$$ROOT",
                    new BsonDocument("contentObject", new BsonDocument("$arrayElemAt", new BsonArray
                    {
                        new BsonDocument("$objectToArray", "$transportElement.content"),
                        0
                    }))
                }))),
                
                // Unwind uomItemPriceList
                new BsonDocument("$unwind", "$contentObject.v.uomItemPriceList"),
                
                // Extract uom object
                new BsonDocument("$replaceRoot", new BsonDocument("newRoot", new BsonDocument("$mergeObjects", new BsonArray
                {
                    "$$ROOT",
                    new BsonDocument("uomObject", new BsonDocument("$arrayElemAt", new BsonArray
                    {
                        new BsonDocument("$objectToArray", "$contentObject.v.uomItemPriceList"),
                        0
                    }))
                }))),
                
                // Unwind priceList
                new BsonDocument("$unwind", "$uomObject.v.priceList"),
                
                // Extract price object
                new BsonDocument("$replaceRoot", new BsonDocument("newRoot", new BsonDocument("$mergeObjects", new BsonArray
                {
                    "$$ROOT",
                    new BsonDocument("priceObject", new BsonDocument("$arrayElemAt", new BsonArray
                    {
                        new BsonDocument("$objectToArray", "$uomObject.v.priceList"),
                        0
                    }))
                }))),
                
                // Project final fields
                new BsonDocument("$project", new BsonDocument
                {
                    ["_id"] = 0,
                    ["contentKey"] = "$transportElement.contentKey",
                    ["uomCode"] = "$uomObject.v.key.uomCode",
                    ["priceOrigin"] = "$contentObject.v.priceOrigin",
                    ["priceTypeCode"] = "$priceObject.v.key.priceTypeCode",
                    ["priceEffectiveDate"] = new BsonDocument("$ifNull", new BsonArray { "$priceObject.v.key.priceEffectiveDate", BsonNull.Value }),
                    ["priceExpirationDate"] = new BsonDocument("$ifNull", new BsonArray { "$priceObject.v.priceExpirationDate", BsonNull.Value }),
                    ["priceAmount"] = "$priceObject.v.priceAmount",
                    ["packagePriceQuantity"] = "$priceObject.v.packagePriceQuantity"
                })
            };

            var result = await _pumpCollection.AggregateAsync<PriceWithoutBaseItem>(pipeline);
            var prices = await result.ToListAsync();

            _logger.LogInformation("Found {Count} prices without base items", prices.Count);
            return prices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get prices without base item");
            return new List<PriceWithoutBaseItem>();
        }
    }

    public async Task<ProcessingStatistics?> GetLatestStatisticsAsync()
    {
        try
        {
            _logger.LogInformation("Getting latest processing statistics from Summary collection");

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

            _logger.LogWarning("No processing summary found in Summary collection");
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
                // Match only BaseItemDO documents
                new BsonDocument("$match", new BsonDocument
                {
                    ["transportElement.contentType"] = "com.gk_software.gkr.api.server.md.item.dto.dom.BaseItemDO"
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
                                ["field"] = "com.gk_software.gkr.api.server.md.item.dto.dom.BaseItemDO",
                                ["input"] = "$transportElement.content"
                            })
                        }),
                        ["regex"] = name,
                        ["options"] = "i"
                    })
                })
            };

            var result = await _pumpCollection.AggregateAsync<BsonDocument>(pipeline);
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

    public async Task<BsonDocument?> FindArticleByContentKeyAsync(string contentKey)
    {
        try
        {
            // Pad the content key with zeros to 18 digits
            string paddedContentKey = contentKey.PadLeft(18, '0');

            _logger.LogInformation("Searching for article with content key: {ContentKey} (padded: {PaddedKey})",
                contentKey, paddedContentKey);

            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("transportElement.contentType",
                    "com.gk_software.gkr.api.server.md.item.dto.dom.BaseItemDO"),
                Builders<BsonDocument>.Filter.Eq("transportElement.contentKey", paddedContentKey)
            );

            var document = await _pumpCollection.Find(filter).FirstOrDefaultAsync();

            if (document == null)
            {
                _logger.LogWarning("No article found with content key: {ContentKey}", paddedContentKey);
                return null;
            }

            _logger.LogInformation("Found article with content key: {ContentKey}", paddedContentKey);
            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find article by content key: {ContentKey}", contentKey);
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
                // Match DynamicTableauItemListDO documents
                new BsonDocument("$match", new BsonDocument
                {
                    ["transportElement.contentType"] = "com.gk_software.gkr.api.server.md.dynamic_tableau.dto.dom.DynamicTableauItemListDO"
                }),
                
                // Extract the DynamicTableauItemListDO from the content
                new BsonDocument("$addFields", new BsonDocument
                {
                    ["tableauItemList"] = new BsonDocument("$getField", new BsonDocument
                    {
                        ["field"] = "com.gk_software.gkr.api.server.md.dynamic_tableau.dto.dom.DynamicTableauItemListDO",
                        ["input"] = "$transportElement.content"
                    })
                }),
                
                // Unwind the dynamicTableauItemListItemList array to process each item
                new BsonDocument("$unwind", "$tableauItemList.dynamicTableauItemListItemList"),
                
                // Extract the DynamicTableauItemListItemDO from each item
                new BsonDocument("$addFields", new BsonDocument
                {
                    ["tableauItem"] = new BsonDocument("$getField", new BsonDocument
                    {
                        ["field"] = "com.gk_software.gkr.api.server.md.dynamic_tableau.dto.dom.DynamicTableauItemListItemDO",
                        ["input"] = "$tableauItemList.dynamicTableauItemListItemList"
                    })
                }),
                
                // Project the desired fields
                new BsonDocument("$project", new BsonDocument
                {
                    ["_id"] = 0,
                    ["contentKey"] = "$transportElement.contentKey",
                    ["businessUnitGroupId"] = "$tableauItemList.key.businessUnitGroupId",
                    ["groupId"] = "$tableauItemList.key.itemListId",
                    ["groupDescription"] = "$tableauItemList.description",
                    ["posItemId"] = "$tableauItem.posItemId",
                    ["sequenceNumber"] = "$tableauItem.sequenceNumber",
                    ["lastUpdateTimestamp"] = "$tableauItemList.lastUpdateTimestamp"
                }),
                
                // Sort by content key and then by sequence number within group
                new BsonDocument("$sort", new BsonDocument
                {
                    ["contentKey"] = 1,
                    ["sequenceNumber"] = 1
                })
            };

            var result = await _pumpCollection.AggregateAsync<PluData>(pipeline);
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
                // Stage 1: Filter for BaseItemDO content type
                new BsonDocument("$match", new BsonDocument
                {
                    ["transportElement.contentType"] = "com.gk_software.gkr.api.server.md.item.dto.dom.BaseItemDO"
                }),

                // Stage 2: Extract the BaseItemDO into a workable field using $getField
                new BsonDocument("$addFields", new BsonDocument
                {
                    ["baseItem"] = new BsonDocument("$getField", new BsonDocument
                    {
                        ["field"] = "com.gk_software.gkr.api.server.md.item.dto.dom.BaseItemDO",
                        ["input"] = "$transportElement.content"
                    }),
                    ["contentKey"] = "$transportElement.contentKey"
                }),

                // Stage 3: Filter documents that have uomItemList (exists and not empty)
                new BsonDocument("$match", new BsonDocument
                {
                    ["baseItem.uomItemList"] = new BsonDocument
                    {
                        ["$exists"] = true,
                        ["$ne"] = new BsonArray()
                    }
                }),

                // Stage 4: Unwind the uomItemList
                new BsonDocument("$unwind", "$baseItem.uomItemList"),

                // Stage 5: Extract UomItemDO content using $getField
                new BsonDocument("$addFields", new BsonDocument
                {
                    ["uomItem"] = new BsonDocument("$getField", new BsonDocument
                    {
                        ["field"] = "com.gk_software.gkr.api.server.md.item.dto.dom.UomItemDO",
                        ["input"] = "$baseItem.uomItemList"
                    })
                }),

                // Stage 6: Filter documents that have uomItemTextList (exists and not empty)
                new BsonDocument("$match", new BsonDocument
                {
                    ["uomItem.uomItemTextList"] = new BsonDocument
                    {
                        ["$exists"] = true,
                        ["$ne"] = new BsonArray()
                    }
                }),

                // Stage 7: Unwind the uomItemTextList
                new BsonDocument("$unwind", "$uomItem.uomItemTextList"),

                // Stage 8: Extract UomItemTextDO content using $getField
                new BsonDocument("$addFields", new BsonDocument
                {
                    ["textItem"] = new BsonDocument("$getField", new BsonDocument
                    {
                        ["field"] = "com.gk_software.gkr.api.server.md.item.dto.dom.UomItemTextDO",
                        ["input"] = "$uomItem.uomItemTextList"
                    })
                }),

                // Stage 9: Filter for INGR or IN text classes only
                new BsonDocument("$match", new BsonDocument
                {
                    ["textItem.key.textClass"] = new BsonDocument("$in", new BsonArray { "INGR", "IN" })
                }),

                // Stage 10: Project needed fields for grouping
                new BsonDocument("$project", new BsonDocument
                {
                    ["contentKey"] = 1,
                    ["name"] = "$baseItem.name",
                    ["textClass"] = "$textItem.key.textClass",
                    ["languageId"] = "$textItem.key.languageId",
                    ["textNumber"] = "$textItem.key.textNumber",
                    ["text"] = "$textItem.text"
                }),

                // Stage 11: Sort by contentKey, textClass, languageId, and textNumber
                new BsonDocument("$sort", new BsonDocument
                {
                    ["contentKey"] = 1,
                    ["textClass"] = 1,
                    ["languageId"] = 1,
                    ["textNumber"] = 1
                }),

                // Stage 12: Group by contentKey, textClass, and languageId - concatenate texts
                new BsonDocument("$group", new BsonDocument
                {
                    ["_id"] = new BsonDocument
                    {
                        ["contentKey"] = "$contentKey",
                        ["name"] = "$name",
                        ["textClass"] = "$textClass",
                        ["languageId"] = "$languageId"
                    },
                    ["texts"] = new BsonDocument("$push", "$text")
                }),

                // Stage 13: Concatenate the text array with space separator
                new BsonDocument("$project", new BsonDocument
                {
                    ["_id"] = 0,
                    ["contentKey"] = "$_id.contentKey",
                    ["name"] = "$_id.name",
                    ["textClass"] = "$_id.textClass",
                    ["languageId"] = "$_id.languageId",
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
                    ["contentKey"] = 1,
                    ["textClass"] = 1,
                    ["languageId"] = 1
                })
            };

            var result = await _pumpCollection.AggregateAsync<ArticleIngredient>(pipeline);
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
/// Metadata for the ThirdApi plugin
/// </summary>
public class ThirdApiPluginMetadata : PluginMetadataBase
{
    public override string Id => "thirdapi";
    public override string Name => "Third API Plugin";
    public override string Version => "1.0.0";
    public override string Description => "A plugin for Third API data operations using MongoDB";
    public override string Author => "McpServer Team";
    public override string RoutePrefix => "thirdapi";
}