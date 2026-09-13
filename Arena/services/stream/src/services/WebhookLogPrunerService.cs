using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamService.Repositories;

namespace StreamService.Services;

//hard: Background service periodically calls PurgeExpiredLogsAsync to resolve CWE-400 unbounded table growth
public class WebhookLogPrunerService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<WebhookLogPrunerService> _logger;

    public WebhookLogPrunerService(IServiceProvider services, ILogger<WebhookLogPrunerService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IWebhookLogRepository>();
                var rows = await repo.PurgeExpiredLogsAsync(7);
                if (rows > 0)
                {
                    _logger.LogInformation("Pruned {Count} expired webhook deduplication logs.", rows);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to prune expired webhook logs.");
            }

            //hard: Runs once every 24 hours
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}