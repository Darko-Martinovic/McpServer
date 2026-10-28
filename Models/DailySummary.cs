namespace McpServer.Models;

public class DailySummary
{
    public DateTime Date { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TotalRevenue { get; set; }
    public int UniqueProducts { get; set; }
    public int TotalItemsSold { get; set; }
    public decimal AverageTransactionValue { get; set; }
    public string TopCategory { get; set; } = string.Empty;
    public decimal TopCategoryRevenue { get; set; }
}
