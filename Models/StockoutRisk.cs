namespace McpServer.Models;

/// <summary>
/// Represents stockout risk analysis for products
/// </summary>
public class StockoutRisk
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public decimal PredictedDemand { get; set; }
    public decimal DaysOfStock { get; set; }
    public string RiskLevel { get; set; } = string.Empty; // "HIGH", "MEDIUM", "LOW"
    public decimal RiskScore { get; set; } // 0.0 to 100.0
    public string RecommendedAction { get; set; } = string.Empty;
    public DateTime? EstimatedStockoutDate { get; set; }
    public decimal PotentialLostRevenue { get; set; }
    
    // Legacy properties for backward compatibility
    public decimal AverageDailySales { get; set; }
    public int DaysUntilStockout { get; set; }
    public string RiskCategory { get; set; } = string.Empty; // "Critical", "High", "Medium", "Low"
    public decimal RecommendedOrderQuantity { get; set; }
}
