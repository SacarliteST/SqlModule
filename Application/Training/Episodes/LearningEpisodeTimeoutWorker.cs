using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SQLModule.Application.Training.Episodes;

public sealed class LearningEpisodeTimeoutWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<LearningEpisodeOptions> options,
    ILogger<LearningEpisodeTimeoutWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(options.Value.CleanupIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<LearningEpisodeTimeoutProcessor>();
                await processor.ProcessAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Ошибка фоновой финализации учебных эпизодов");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }
}
