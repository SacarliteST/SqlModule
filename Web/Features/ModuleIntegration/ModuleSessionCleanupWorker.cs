using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SQLModule.Web.Features.ModuleIntegration;

internal sealed class ModuleSessionCleanupWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ModuleIntegrationOptions> options,
    ILogger<ModuleSessionCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromMinutes(options.Value.Lifecycle.CleanupIntervalMinutes));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ModuleSessionCleanupProcessor>();
                var removed = await processor.CleanupAsync(stoppingToken);
                if (removed > 0)
                {
                    logger.LogInformation(
                        "Удалено устаревших platform-сессий: {RemovedCount}.",
                        removed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Ошибка очистки устаревших platform-сессий.");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }
}
