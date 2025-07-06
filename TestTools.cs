using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using McpServer.Services;
using McpServer.Services.Interfaces;
using McpServer.Configuration;
using Microsoft.Extensions.Options;

// Simple test to verify tools and logging work
var builder = Host.CreateApplicationBuilder();
builder.Services.Configure<ConnectionStringOptions>(options =>
{
    options.DefaultConnection = "Server=DARKO\\SQLEXPRESS;Database=SupermarketDB;Integrated Security=true;TrustServerCertificate=true;";
});
builder.Services.AddScoped<ISupermarketDataService, SupermarketDataService>();

var host = builder.Build();
var dataService = host.Services.GetRequiredService<ISupermarketDataService>();

Console.WriteLine("Testing direct tool execution...");

try
{
    var result = await McpServer.SupermarketMcpTools.GetProducts(dataService);
    Console.WriteLine($"Tool result: {result.Substring(0, Math.Min(200, result.Length))}...");

    // Check if debug file was created
    if (File.Exists("mcp-debug.txt"))
    {
        Console.WriteLine("Debug file created successfully!");
        Console.WriteLine(File.ReadAllText("mcp-debug.txt"));
    }
    else
    {
        Console.WriteLine("Debug file NOT created - tools may not be executing");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
