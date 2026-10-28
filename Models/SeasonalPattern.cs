namespace McpServer.Models;

/// <summary>
/// Represents seasonal sales patterns for categories
/// </summary>
public class SeasonalPattern
{
    public string Category { get; set; } = string.Empty;
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal SeasonalityFactor { get; set; }
    public decimal AverageMonthlySales { get; set; }
    public decimal ExpectedSales { get; set; }
    public string Trend { get; set; } = string.Empty; // "Peak", "Low", "Normal"
}
