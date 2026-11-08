using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLModule.Application.Persistence;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Episodes;

public sealed class LearningEpisodeTimeoutProcessor(
    IApplicationDbContext db,
    LearningEpisodeService episodeService,
    IOptions<LearningEpisodeOptions> options,
    TimeProvider timeProvider,
    ILogger<LearningEpisodeTimeoutProcessor> logger)
{
    public async Task<int> ProcessAsync(CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        var cutoff = now - LearningEpisodeService.HeartbeatGracePeriod;
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, ct);
        var episodes = await db.LearningEpisodes
            .Where(episode =>
                episode.Status == LearningEpisodeStatus.Active &&
                episode.LastHeartbeatAt <= cutoff)
            .OrderBy(episode => episode.LastHeartbeatAt)
            .ThenBy(episode => episode.Id)
            .Take(options.Value.BatchSize)
            .ToListAsync(ct);
        foreach (var episode in episodes)
        {
            var result = await episodeService.FinalizeEntityAsync(
                episode,
                LearningEpisodeFinalizationReason.HeartbeatTimeout,
                now,
                ct);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(result.Error!.Message);
            }
        }

        if (episodes.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);
        if (episodes.Count > 0)
        {
            logger.LogInformation(
                "По тайм-ауту heartbeat финализировано учебных эпизодов: {EpisodeCount}",
                episodes.Count);
        }

        return episodes.Count;
    }
}
