namespace McpServer.Models;

public class CategorySales
{
    public string Category { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public int TotalQuantity { get; set; }
} 