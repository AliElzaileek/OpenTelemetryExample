using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CFX.Logging;

public class LoggingService : BackgroundService
{
    private readonly ILogger<LoggingService> _logger;

    public LoggingService(ILogger<LoggingService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Service running at: {time}", DateTimeOffset.Now);
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            _logger.LogWarning("This is a test warning: {Info}", "Warning info");
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            _logger.LogError("This is a test error: {Info}", "Error info");
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            _logger.LogCritical("This is a critical error test: {Info}", "Critical Error info");
            // Wait for a period before logging again
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}