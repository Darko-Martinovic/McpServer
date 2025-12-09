using McpServer.Services;
using McpServer.Services.Interfaces;
using McpServer.Configuration;
using McpServer.Plugins.Services;
using McpServer.Controllers;
using McpServer.Plugins.ThirdApi;
using McpServer.Plugins.ThirdApi.Controllers;
using McpServer.Plugins.ThirdApi.Services;
using Serilog;


LoadEnvironmentVariables();

// Determine application mode based on arguments and configuration
var runMode = DetermineRunMode(args);

if (runMode == ApplicationRunMode.Console)
{
    await RunMcpServerAsync(args);
}
else
{
    await RunWebApiAsync(args);
}

static async Task RunMcpServerAsync(string[] args)
{
    // MCP Server mode - redirect console output to stderr to avoid polluting JSON-RPC
    Console.SetOut(Console.Error);

    // Create a minimal host builder for console-only operation
    var builder = new HostBuilder();

    // Configure configuration sources
    builder.ConfigureAppConfiguration((context, config) =>
    {
        config.Sources.Clear();
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
              .AddEnvironmentVariables()
              .AddCommandLine(args);
    });

    // Configure services for MCP mode only
    builder.ConfigureServices((context, services) =>
    {
        // Configure common services but exclude web-specific ones
        ConfigureMcpOnlyServices(services, context.Configuration);

        // Configure plugin system
        services.AddSingleton<IPluginDiscoveryService, PluginDiscoveryService>();

        // Register plugin providers
        services.AddSingleton<SupermarketToolProvider>();
        services.AddSingleton<ThirdApiToolProvider>();

        // For now, keep the existing MCP server configuration
        // TODO: Implement dynamic tool discovery in next iteration
        services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly();

        // Add hosted service to keep the host alive
        services.AddHostedService<McpServer.McpServerHostedService>();
    });

    var host = builder.Build();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Starting Supermarket MCP Server in Console mode");
    Console.Error.WriteLine($"[DEBUG] MCP Server starting at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    Console.Error.WriteLine($"[DEBUG] Logging to: Logs/mcpserver.log");

    // Initialize plugin system for future use
    try
    {
        var pluginDiscovery = host.Services.GetRequiredService<IPluginDiscoveryService>();
        var plugins = await pluginDiscovery.DiscoverPluginsAsync();
        logger.LogInformation("Plugin system initialized with {PluginCount} plugins", plugins.Count());
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Plugin system initialization failed - continuing with standard MCP tools");
    }

    await host.RunAsync();
}

static void ConfigureMcpOnlyServices(IServiceCollection services, IConfiguration configuration)
{
    // Ensure Logs directory exists for Serilog
    Directory.CreateDirectory("Logs");

    // Configure Serilog
    Log.Logger = new LoggerConfiguration().MinimumLevel
        .Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("ModelContextProtocol", Serilog.Events.LogEventLevel.Information)
        .WriteTo.File(
            path: "Logs/mcpserver.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            flushToDiskInterval: TimeSpan.FromSeconds(1)
        )
        .Enrich.FromLogContext()
        .CreateLogger();

    services.AddLogging(builder =>
    {
        builder.ClearProviders();
        builder.AddSerilog(Log.Logger, dispose: true);
    });

    // Configure connection string with fallback
    services.Configure<ConnectionStringOptions>(options =>
    {
        var connectionSection = configuration.GetSection("ConnectionStrings");
        if (connectionSection.Exists())
        {
            connectionSection.Bind(options);
        }
        else
        {
            options.DefaultConnection =
                "Server=(localdb)\\MSSQLLocalDB;Database=SupermarketDB;Integrated Security=true;TrustServerCertificate=true;";
        }
    });

    // Configure Azure Search options (optional for MCP)
    services.Configure<AzureSearchOptions>(options =>
    {
        options.Endpoint = Environment.GetEnvironmentVariable("COGNITIVESEARCH_ENDPOINT") ?? "";
        options.ApiKey = Environment.GetEnvironmentVariable("COGNITIVESEARCH_APIKEY") ?? "";
        options.IndexName = "mcp-tools";
    });

    // Configure Azure OpenAI options (optional for MCP)
    services.Configure<AzureOpenAIOptions>(options =>
    {
        options.Endpoint = Environment.GetEnvironmentVariable("AOAI_ENDPOINT") ?? "";
        options.ApiKey = Environment.GetEnvironmentVariable("AOAI_APIKEY") ?? "";
        options.ChatCompletionDeploymentName =
            Environment.GetEnvironmentVariable("CHATCOMPLETION_DEPLOYMENTNAME") ?? "";
        options.EmbeddingDeploymentName =
            Environment.GetEnvironmentVariable("EMBEDDING_DEPLOYMENTNAME") ?? "";
    });

    // Register ONLY business services (no web services)
    services.AddScoped<ISupermarketDataService, SupermarketDataService>();

    // Register Azure services (but don't require them to start)
    services.AddScoped<IAzureSearchService, AzureSearchService>();
    services.AddScoped<IMcpToolIndexingService, McpToolIndexingService>();
    services.AddScoped<IAIConversationService, AIConversationService>();
}

static async Task RunWebApiAsync(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    ConfigureCommonServices(builder.Services, builder.Configuration);

    // Configure plugin system for Web API
    builder.Services.AddSingleton<IPluginDiscoveryService, PluginDiscoveryService>();
    builder.Services.AddSingleton<SupermarketToolProvider>();
    builder.Services.AddSingleton<ThirdApiToolProvider>();
    builder.Services.AddScoped<IPluginControllerService, PluginControllerService>();

    // Configure services for each plugin
    var supermarketProvider = new SupermarketToolProvider();
    supermarketProvider.ConfigureServices(builder.Services);

    var thirdApiProvider = new ThirdApiToolProvider();
    thirdApiProvider.ConfigureServices(builder.Services);

    // Configure URLs from appsettings
    var appModeOptions = builder.Configuration
        .GetSection("ApplicationMode")
        .Get<ApplicationModeOptions>();
    if (appModeOptions?.Web?.Urls?.Any() == true)
    {
        builder.WebHost.UseUrls(appModeOptions.Web.Urls.ToArray());
    }
    else
    {
        builder.WebHost.UseUrls("http://localhost:5000");
    }

    // Web API configuration - include plugin assemblies
    // Manually include both SupermarketController, ThirdApiController, and ToolProxyController assemblies
    builder.Services.AddControllers()
        .AddApplicationPart(typeof(ChatController).Assembly)
        .AddApplicationPart(typeof(ThirdApiController).Assembly)
        .AddApplicationPart(typeof(ToolProxyController).Assembly);
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc(
            "v1",
            new()
            {
                Title = "Supermarket MCP API",
                Version = "v1",
                Description = "REST API exposing supermarket MCP tools for React applications"
            }
        );
    });

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        });
    });

    var app = builder.Build();
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Starting Supermarket Server in Web API mode");

    // Initialize plugin system for Web API
    try
    {
        var pluginDiscovery = app.Services.GetRequiredService<IPluginDiscoveryService>();
        var plugins = await pluginDiscovery.DiscoverPluginsAsync();
        logger.LogInformation("Plugin system initialized with {PluginCount} plugins for Web API", plugins.Count());
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Plugin system initialization failed for Web API - continuing with standard controllers");
    }

    // Configure web pipeline
    // Always enable Swagger for API documentation
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Supermarket MCP API v1");
        c.RoutePrefix = "swagger";
    });

    app.UseCors();
    app.UseRouting();
    app.MapControllers();

    // Health check endpoint
    app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

    logger.LogInformation("Web API available at: http://localhost:5000");
    logger.LogInformation("Swagger UI available at: http://localhost:5000/swagger");

    // Index MCP tools to Azure Search
    try
    {
        using var scope = app.Services.CreateScope();
        var indexingService = scope.ServiceProvider.GetRequiredService<IMcpToolIndexingService>();
        await indexingService.IndexToolsAsync();
        logger.LogInformation("Successfully indexed MCP tools to Azure Search");
    }
    catch (Exception ex)
    {
        logger.LogError(
            ex,
            "Failed to index MCP tools to Azure Search - continuing without indexing"
        );
    }

    await app.RunAsync();
}

static void ConfigureCommonServices(IServiceCollection services, IConfiguration configuration)
{
    // Ensure Logs directory exists for Serilog
    Directory.CreateDirectory("Logs");

    // Configure Serilog
    Log.Logger = new LoggerConfiguration().MinimumLevel
        .Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("ModelContextProtocol", Serilog.Events.LogEventLevel.Information)
        .WriteTo.File(
            path: "Logs/mcpserver.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            flushToDiskInterval: TimeSpan.FromSeconds(1)
        )
        .Enrich.FromLogContext()
        .CreateLogger();

    services.AddLogging(builder =>
    {
        builder.ClearProviders();
        builder.AddSerilog(Log.Logger, dispose: true);
    });

    // Configure application mode options
    services.Configure<ApplicationModeOptions>(
        configuration.GetSection(ApplicationModeOptions.SectionName)
    );

    // Configure connection string with fallback
    services.Configure<ConnectionStringOptions>(options =>
    {
        var connectionSection = configuration.GetSection("ConnectionStrings");
        if (connectionSection.Exists())
        {
            connectionSection.Bind(options);
        }
        else
        {
            options.DefaultConnection =
                "Server=DARKO\\SQLEXPRESS;Database=SupermarketDB;Integrated Security=true;TrustServerCertificate=true;";
        }
    });

    // Configure Azure Search options
    services.Configure<AzureSearchOptions>(options =>
    {
        options.Endpoint = Environment.GetEnvironmentVariable("COGNITIVESEARCH_ENDPOINT") ?? "";
        options.ApiKey = Environment.GetEnvironmentVariable("COGNITIVESEARCH_APIKEY") ?? "";
        options.IndexName = "mcp-tools";
    });

    // Configure Azure OpenAI options
    services.Configure<AzureOpenAIOptions>(options =>
    {
        options.Endpoint = Environment.GetEnvironmentVariable("AOAI_ENDPOINT") ?? "";
        options.ApiKey = Environment.GetEnvironmentVariable("AOAI_APIKEY") ?? "";
        options.ChatCompletionDeploymentName =
            Environment.GetEnvironmentVariable("CHATCOMPLETION_DEPLOYMENTNAME") ?? "";
        options.EmbeddingDeploymentName =
            Environment.GetEnvironmentVariable("EMBEDDING_DEPLOYMENTNAME") ?? "";
    });

    // Register business services
    services.AddScoped<ISupermarketDataService, SupermarketDataService>();

    // Register Azure services
    services.AddScoped<IAzureSearchService, AzureSearchService>();
    services.AddScoped<IMcpToolIndexingService, McpToolIndexingService>();
    services.AddScoped<IAIConversationService, AIConversationService>();

    // Register Tool Execution Service (replaces Node.js proxy)
    services.AddScoped<IToolExecutionService, ToolExecutionService>();
}

static ApplicationRunMode DetermineRunMode(string[] args)
{
    // Check command line arguments first
    if (args.Contains("--web") || args.Contains("--webapi"))
        return ApplicationRunMode.Web;
    if (args.Contains("--console") || args.Contains("--mcp"))
        return ApplicationRunMode.Console;

    // Check if running with "dotnet run" (likely for MCP)
    var commandLine = Environment.CommandLine;
    if (commandLine.Contains("dotnet run") || commandLine.Contains("run --project"))
        return ApplicationRunMode.Console;

    // Default to web mode when run as executable
    return ApplicationRunMode.Web;
}

static void LoadEnvironmentVariables()
{
    var envFilePath = Path.Combine(Directory.GetCurrentDirectory(), ".env");

    if (!File.Exists(envFilePath))
    {
        Console.Error.WriteLine($"Warning: .env file not found at {envFilePath}");
        return;
    }

    foreach (var line in File.ReadAllLines(envFilePath))
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            continue;

        var parts = line.Split('=', 2);
        if (parts.Length == 2)
        {
            var key = parts[0].Trim();
            var value = parts[1].Trim();
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}

enum ApplicationRunMode
{
    Console, // MCP mode only
    Web // Web API mode only
}
