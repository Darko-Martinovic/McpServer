namespace McpServer.Models;

/// <summary>
/// Represents demand forecast for a specific product
/// </summary>
public class DemandForecast
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime ForecastDate { get; set; }
    public decimal PredictedDemand { get; set; }
    public decimal ConfidenceLevel { get; set; }
    public string TrendDirection { get; set; } = string.Empty; // "Increasing", "Decreasing", "Stable"
    public decimal CurrentStock { get; set; }
    public decimal RecommendedStock { get; set; }
}
