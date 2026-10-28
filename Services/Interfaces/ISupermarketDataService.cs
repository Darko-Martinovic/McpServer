using System.Data;
using McpServer.Models;
using McpServer.Plugins.Interfaces;

namespace McpServer.Services.Interfaces;

public interface ISupermarketDataService : IDataService<Product>, IHealthCheckableDataService
{
    Task<IEnumerable<Product>> GetProductsAsync();
    Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category);
    Task<IEnumerable<Product>> GetProductsBySupplierAsync(string supplier);
    Task<IEnumerable<SalesRecord>> GetSalesDataAsync(DateTime startDate, DateTime endDate);
    Task<decimal> GetTotalRevenueAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold);
    Task<IEnumerable<CategorySales>> GetSalesByCategoryAsync(DateTime startDate, DateTime endDate);

    // MCP Resource methods
    Task<IEnumerable<InventoryStatus>> GetInventoryStatusAsync();
    Task<IEnumerable<DailySummary>> GetDailySummaryAsync(DateTime? date = null);
    Task<IEnumerable<Product>> GetDetailedInventoryAsync();

    // Phase 3: Predictive Analytics methods
    Task<IEnumerable<DemandForecast>> PredictDemandAsync(int daysAhead = 7);
    Task<DemandForecast?> PredictProductDemandAsync(int productId, int daysAhead = 7);
    Task<IEnumerable<StockoutRisk>> GetStockoutRisksAsync(int daysAhead = 14);
    Task<IEnumerable<StockoutRisk>> GetCriticalStockoutRisksAsync();
    Task<IEnumerable<SeasonalPattern>> GetSeasonalTrendsAsync(string? category = null);
    Task<IEnumerable<SeasonalPattern>> GetSeasonalForecastAsync(int monthsAhead = 3);
    Task<IEnumerable<ReorderRecommendation>> GetReorderRecommendationsAsync();
    Task<IEnumerable<ReorderRecommendation>> GetUrgentReorderRecommendationsAsync();
}