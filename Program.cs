using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using McpServer.Services;
using McpServer.Services.Interfaces;
using McpServer.Configuration;
using Serilog;

// Redirect all console output to stderr to avoid breaking MCP protocol
// This is critical for MCP servers as stdout is used for JSON-RPC communication
Console.SetOut(Console.Error);

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Ensure Logs directory exists for Serilog
Directory.CreateDirectory("Logs");

// Configure Serilog explicitly to avoid any console output issues
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("ModelContextProtocol", Serilog.Events.LogEventLevel.Information)
    .WriteTo.File(
        path: "Logs/mcpserver.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        flushToDiskInterval: TimeSpan.FromSeconds(1))
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger, dispose: true);

// Configure connection string with fallback
builder.Services.Configure<ConnectionStringOptions>(options =>
{
    var connectionSection = builder.Configuration.GetSection("ConnectionStrings");
    if (connectionSection.Exists())
    {
        connectionSection.Bind(options);
    }
    else
    {
        // Fallback connection string if config file is missing
        options.DefaultConnection = "Server=DARKO\\SQLEXPRESS;Database=SupermarketDB;Integrated Security=true;TrustServerCertificate=true;";
    }
});

builder.Services.AddScoped<ISupermarketDataService, SupermarketDataService>();

// Configure MCP server
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

try
{
    var host = builder.Build();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();

    // Log startup information (this goes to file via Serilog)
    logger.LogInformation("Supermarket MCP Server starting up... [Version with enhanced logging - Build {BuildTime}]", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
    logger.LogInformation("Database connection: {ConnectionString}",
        builder.Configuration.GetConnectionString("DefaultConnection") ?? "Using fallback connection");

    // Write startup debug info to stderr (not stdout to avoid breaking MCP)
    Console.Error.WriteLine($"[DEBUG] MCP Server with enhanced logging starting at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    Console.Error.WriteLine($"[DEBUG] Logging to: Logs/mcpserver.log");

    await host.RunAsync();
}
catch (Exception ex)
{
    // Write errors to stderr, never to stdout
    Console.Error.WriteLine($"[ERROR] MCP Server startup failed: {ex.Message}");
    Console.Error.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
    Environment.Exit(1);
}
finally
{
    Log.CloseAndFlush();
}
