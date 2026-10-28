using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace McpServer;

public class McpServerHostedService : BackgroundService
{
    private readonly ILogger<McpServerHostedService> _logger;

    public McpServerHostedService(ILogger<McpServerHostedService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MCP Server hosted service starting");

        // Keep the service alive until cancellation is requested
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // Expected when the application is shutting down
            _logger.LogInformation("MCP Server hosted service stopping");
        }
    }
}