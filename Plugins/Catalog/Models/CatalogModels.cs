using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace McpServer.Plugins.Catalog.Models;

/// <summary>
/// Represents a price without article result
/// </summary>
public class PriceWithoutArticle
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("articleKey")]
    public string ArticleKey { get; set; } = string.Empty;

    [BsonElement("unitCode")]
    public string UomCode { get; set; } = string.Empty;

    [BsonElement("priceSource")]
    public string PriceSource { get; set; } = string.Empty;

    [BsonElement("priceType")]
    public string PriceType { get; set; } = string.Empty;

    [BsonElement("validFrom")]
    public DateTime? ValidFrom { get; set; }

    [BsonElement("validTo")]
    public DateTime? ValidTo { get; set; }

    [BsonElement("priceAmount")]
    public decimal PriceAmount { get; set; }

    [BsonElement("packQuantity")]
    public decimal PackQuantity { get; set; }
}

/// <summary>
/// Represents a content type statistic
/// </summary>
public class ContentTypeStat
{
    [BsonElement("contentType")]
    public string ContentType { get; set; } = string.Empty;

    [BsonElement("className")]
    public string ClassName { get; set; } = string.Empty;

    [BsonElement("count")]
    public int Count { get; set; }
}

/// <summary>
/// Represents the statistics section from import_stats collection
/// </summary>
public class ProcessingStatistics
{
    [BsonElement("totalUniqueTypes")]
    public int TotalUniqueTypes { get; set; }

    [BsonElement("totalDocuments")]
    public int TotalDocuments { get; set; }

    [BsonElement("contentTypes")]
    public List<ContentTypeStat> ContentTypes { get; set; } = new List<ContentTypeStat>();
}

/// <summary>
/// Represents console output information
/// </summary>
public class ConsoleOutput
{
    /// <summary>
    /// Old schema: JSON output file path
    /// </summary>
    [BsonElement("jsonSavedTo")]
    [BsonIgnoreIfNull]
    public string? JsonSavedTo { get; set; }

    /// <summary>
    /// New schema: Source file path (replaces jsonSavedTo)
    /// </summary>
    [BsonElement("sourceFile")]
    [BsonIgnoreIfNull]
    public string? SourceFile { get; set; }

    [BsonElement("mongoDbMessage")]
    public string MongoDbMessage { get; set; } = string.Empty;

    [BsonElement("documentsInsertedMessage")]
    public string DocumentsInsertedMessage { get; set; } = string.Empty;
}

/// <summary>
/// Represents a summary document from the import_stats collection
/// </summary>
public class ProcessingSummary
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("processedAt")]
    public DateTime ProcessedAt { get; set; }

    [BsonElement("inputFile")]
    public string InputFile { get; set; } = string.Empty;

    [BsonElement("outputFile")]
    public string OutputFile { get; set; } = string.Empty;

    [BsonElement("documentsInserted")]
    public int DocumentsInserted { get; set; }

    [BsonElement("statistics")]
    public ProcessingStatistics Statistics { get; set; } = new ProcessingStatistics();

    [BsonElement("consoleOutput")]
    public ConsoleOutput ConsoleOutput { get; set; } = new ConsoleOutput();
}

/// <summary>
/// Represents a Article article from the articles collection
/// </summary>
public class CatalogArticle
{
    [BsonElement("articleKey")]
    public string ArticleKey { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("itemNumber")]
    public string? ItemNumber { get; set; }

    [BsonElement("contentType")]
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>
/// Represents PLU data from Acme's back office (PosItemList)
/// </summary>
public class PluData
{
    [BsonElement("articleKey")]
    public string ArticleKey { get; set; } = string.Empty;

    [BsonElement("storeGroupId")]
    public long StoreGroupId { get; set; }

    [BsonElement("groupId")]
    public string GroupId { get; set; } = string.Empty;

    [BsonElement("groupDescription")]
    public string GroupDescription { get; set; } = string.Empty;

    [BsonElement("itemCode")]
    public string ItemCode { get; set; } = string.Empty;

    [BsonElement("sequenceNumber")]
    public int SequenceNumber { get; set; }

    [BsonElement("lastUpdateTimestamp")]
    public DateTime LastUpdateTimestamp { get; set; }
}

/// <summary>
/// Represents an article with ingredient information extracted from Article
/// </summary>
public class ArticleIngredient
{
    [BsonElement("articleKey")]
    public string ArticleKey { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("textType")]
    public string TextType { get; set; } = string.Empty;

    [BsonElement("language")]
    public string Language { get; set; } = string.Empty;

    [BsonElement("text")]
    public string Text { get; set; } = string.Empty;
}