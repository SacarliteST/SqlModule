namespace SQLModule.Sandbox;

/// <summary>Настройки движка песочницы (секция <c>Sandbox</c> в appsettings).</summary>
public sealed class SandboxOptions
{
    public const string SectionKey = "Sandbox";

    public int ContainerStartupTimeoutSeconds { get; init; } = 120;
    public int DefaultQueryTimeoutSeconds { get; init; } = 15;
    public int MaxRows { get; init; } = 1000;
    public int ComparisonMaxRows { get; init; } = 10000;
    public int MaxSqlLength { get; init; } = 20000;
    public SandboxPoolOptions Pool { get; init; } = new();
}

/// <summary>Настройки локального пула тёплых sandbox-контейнеров.</summary>
public sealed class SandboxPoolOptions
{
    public bool Enabled { get; init; }
    public int AcquireTimeoutSeconds { get; init; } = 15;
    public int PreparationTimeoutSeconds { get; init; } = 30;
    public int CleanupTimeoutSeconds { get; init; } = 15;
    public int ShutdownTimeoutSeconds { get; init; } = 30;
    public int HealthCheckIntervalSeconds { get; init; } = 15;
    public int RestartBackoffMaxSeconds { get; init; } = 60;
    public SandboxPoolResourceOptions Resources { get; init; } = new();

    /// <summary>Размеры пула по <c>SystemName</c>; регистр ключа не учитывается.</summary>
    public Dictionary<string, SandboxPoolProfileOptions> Profiles { get; } =
        new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Жёсткие ограничения ресурсов одного тёплого sandbox-контейнера.</summary>
public sealed class SandboxPoolResourceOptions
{
    public int MemoryLimitMegabytes { get; init; } = 512;
    public double CpuLimit { get; init; } = 1;
    public int PidsLimit { get; init; } = 256;
}

/// <summary>Минимальный и максимальный размер пула одного профиля СУБД.</summary>
public sealed class SandboxPoolProfileOptions
{
    public int MinSize { get; init; }
    public int MaxSize { get; init; } = 1;
}
