using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CFX.Opentelemetry;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        var currentDir = Directory.GetCurrentDirectory();
        var solutionDir = Directory.GetParent(currentDir)?.Parent?.Parent;
        if (solutionDir == null)
        {
            throw new DirectoryNotFoundException("Could not find solution directory");
        }

        var globalSettingsPath = Path.Combine(solutionDir.FullName, "globalSettings.json");
        config.AddJsonFile(globalSettingsPath, optional: false, reloadOnChange: true);
        
        config.AddEnvironmentVariables();
        config.AddCommandLine(args);
    })
    .ConfigureServices((context, services) =>
    {
        // Now context.Configuration is properly configured
        services.AddApplicationTelemetry(context.Configuration);
        // Other services
    })
    .Build();

// Get logger and test logging
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application starting");
logger.LogWarning("This is a test warning");
logger.LogError(new Exception("Test exception"), "Error occurred");

await host.RunAsync();