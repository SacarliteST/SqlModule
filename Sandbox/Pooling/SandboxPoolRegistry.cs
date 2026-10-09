using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SQLModule.Sandbox.Pooling;

/// <summary>Хранит пулы профилей: создаёт пул профиля один раз и проверяет конфликт границ MinSize/MaxSize.</summary>
internal sealed class SandboxPoolRegistry(ILogger logger)
{
    private readonly ConcurrentDictionary<SandboxProfileKey, ProfilePool> pools = new();

    internal IEnumerable<ProfilePool> Pools => pools.Values;

    /// <summary>Возвращает пул профиля; <see langword="false"/>, если границы профиля расходятся с созданным пулом.</summary>
    internal bool TryGetPool(SandboxWorkerProfile profile, out ProfilePool pool)
    {
        pool = pools.GetOrAdd(profile.Key, _ => new ProfilePool(profile));
        if (pool.HasLimits(profile))
        {
            return true;
        }

        logger.LogError(
            "Конфликт конфигурации профиля пула {Profile}; переданы границы {MinSize}..{MaxSize}",
            profile.Key,
            profile.MinSize,
            profile.MaxSize);
        return false;
    }
}
