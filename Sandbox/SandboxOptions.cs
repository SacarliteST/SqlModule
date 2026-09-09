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
}
