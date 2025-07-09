using System.Data;
using McpServer.Models;

namespace McpServer.Services.Interfaces;

public interface ISupermarketDataService
{
    Task<IEnumerable<Product>> GetProductsAsync();
    Task<IEnumerable<SalesRecord>> GetSalesDataAsync(DateTime startDate, DateTime endDate);
    Task<decimal> GetTotalRevenueAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold);
    Task<IEnumerable<CategorySales>> GetSalesByCategoryAsync(DateTime startDate, DateTime endDate);

    // MCP Resource methods
    Task<IEnumerable<InventoryStatus>> GetInventoryStatusAsync();
    Task<IEnumerable<DailySummary>> GetDailySummaryAsync(DateTime? date = null);
    Task<IEnumerable<Product>> GetDetailedInventoryAsync();
}