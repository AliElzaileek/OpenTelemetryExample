﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CFX.Opentelemetry;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        string currentDir = Directory.GetCurrentDirectory();
        DirectoryInfo? solutionDir = Directory.GetParent(currentDir)?.Parent?.Parent?.Parent;
        if (solutionDir == null)
        {
            throw new DirectoryNotFoundException("Could not find solution directory");
        }

        string globalSettingsPath = Path.Combine(solutionDir.FullName, "globalSettings.json");
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
ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application starting");
logger.LogWarning("This is a test warning");
logger.LogError(new Exception("Test exception"), "Error occurred");

await host.RunAsync();