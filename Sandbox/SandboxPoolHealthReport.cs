namespace SQLModule.Sandbox;

/// <summary>Безопасное агрегированное состояние пула для health endpoint.</summary>
public sealed record SandboxPoolHealthReport(
    bool IsHealthy,
    bool IsEnabled,
    IReadOnlyCollection<string> UnavailableProfiles);

/// <summary>Предоставляет текущее состояние обязательных профилей sandbox-пула.</summary>
public interface ISandboxPoolHealthMonitor
{
    SandboxPoolHealthReport GetReport();
}
