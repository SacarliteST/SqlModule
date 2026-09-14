namespace SQLModule.Sandbox.Pooling;

internal enum SandboxWorkerState
{
    /// <summary>Контейнер создаётся и ещё не может принимать аренды.</summary>
    Starting,

    /// <summary>Воркер исправен, очищен и доступен для новой аренды.</summary>
    Ready,

    /// <summary>Воркер эксклюзивно закреплён за одной активной арендой.</summary>
    Leased,

    /// <summary>После аренды выполняется очистка перед возвратом в пул.</summary>
    Recycling,

    /// <summary>Воркер нельзя переиспользовать; он ожидает удаления.</summary>
    Unhealthy,

    /// <summary>Контейнер удалён, а воркер окончательно выведен из пула.</summary>
    Disposed,
}
