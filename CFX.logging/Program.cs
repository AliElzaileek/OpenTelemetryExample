using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CFX.OpenTelemetry;
using CFX.Logging;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        // Load configuration from appsettings.json and environment variables
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        config.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
        IConfigurationRoot tmpConfig = config.Build();
        string? globalPath = tmpConfig.GetValue<string>("GlobalConfigPath");

        ArgumentException.ThrowIfNullOrEmpty(globalPath, nameof(globalPath));

        config.AddJsonFile(globalPath, optional: false, reloadOnChange: true);
        
        config.AddEnvironmentVariables();
        config.AddCommandLine(args);
        if(hostingContext.HostingEnvironment.IsDevelopment())
        {
            config.AddUserSecrets<Program>(optional: true);
        }
    })
    .ConfigureServices((context, services) =>
    {
        // Now context.Configuration is properly configured
        services.AddApplicationTelemetry(context.Configuration);
        
        // If you want to add a hosted service that logs periodically:
        services.AddHostedService<LoggingService>();
    })
    .Build();

// Log startup message once
ILogger<Program> startupLogger = host.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("Application starting");

// Run the host until shutdown is requested
await host.RunAsync();