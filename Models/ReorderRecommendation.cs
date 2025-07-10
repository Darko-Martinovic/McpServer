namespace McpServer.Models;

/// <summary>
/// Represents advanced reorder recommendations with optimization
/// </summary>
public class ReorderRecommendation
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int ReorderLevel { get; set; }
    public int RecommendedQuantity { get; set; }
    public decimal EstimatedCost { get; set; }
    public string Priority { get; set; } = string.Empty; // "IMMEDIATE", "URGENT", "SCHEDULED", "MONITOR"
    public string Reason { get; set; } = string.Empty;
    public DateTime SuggestedOrderDate { get; set; }
    public int LeadTimeDays { get; set; }
    public decimal PredictedSalesDuringLeadTime { get; set; }
    public decimal DaysUntilStockout { get; set; }
    public decimal RiskScore { get; set; }

    // Legacy properties for backward compatibility
    public int RecommendedOrderQuantity { get; set; }
}
