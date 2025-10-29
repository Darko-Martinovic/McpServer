using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace McpServer.Plugins.ThirdApi.Models;

/// <summary>
/// Represents a price without base item result
/// </summary>
public class PriceWithoutBaseItem
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("contentKey")]
    public string ContentKey { get; set; } = string.Empty;

    [BsonElement("uomCode")]
    public string UomCode { get; set; } = string.Empty;

    [BsonElement("priceOrigin")]
    public string PriceOrigin { get; set; } = string.Empty;

    [BsonElement("priceTypeCode")]
    public string PriceTypeCode { get; set; } = string.Empty;

    [BsonElement("priceEffectiveDate")]
    public DateTime? PriceEffectiveDate { get; set; }

    [BsonElement("priceExpirationDate")]
    public DateTime? PriceExpirationDate { get; set; }

    [BsonElement("priceAmount")]
    public decimal PriceAmount { get; set; }

    [BsonElement("packagePriceQuantity")]
    public decimal PackagePriceQuantity { get; set; }
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
/// Represents the statistics section from Summary collection
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
    [BsonElement("jsonSavedTo")]
    public string JsonSavedTo { get; set; } = string.Empty;

    [BsonElement("mongoDbMessage")]
    public string MongoDbMessage { get; set; } = string.Empty;

    [BsonElement("documentsInsertedMessage")]
    public string DocumentsInsertedMessage { get; set; } = string.Empty;
}

/// <summary>
/// Represents a summary document from the Summary collection
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
/// Represents a BaseItemDO article from the Pump collection
/// </summary>
public class BaseItemArticle
{
    [BsonElement("contentKey")]
    public string ContentKey { get; set; } = string.Empty;

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
/// Represents PLU data from SAP Fiori (DynamicTableauItemListDO)
/// </summary>
public class PluData
{
    [BsonElement("contentKey")]
    public string ContentKey { get; set; } = string.Empty;

    [BsonElement("businessUnitGroupId")]
    public long BusinessUnitGroupId { get; set; }

    [BsonElement("groupId")]
    public string GroupId { get; set; } = string.Empty;

    [BsonElement("groupDescription")]
    public string GroupDescription { get; set; } = string.Empty;

    [BsonElement("posItemId")]
    public string PosItemId { get; set; } = string.Empty;

    [BsonElement("sequenceNumber")]
    public int SequenceNumber { get; set; }

    [BsonElement("lastUpdateTimestamp")]
    public DateTime LastUpdateTimestamp { get; set; }
}