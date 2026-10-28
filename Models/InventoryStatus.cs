namespace McpServer.Models;

public class InventoryStatus
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public int ReorderLevel { get; set; }
    public string StockStatus { get; set; } = string.Empty;
    public int RecentSales { get; set; }
    public DateTime LastUpdated { get; set; }
}
