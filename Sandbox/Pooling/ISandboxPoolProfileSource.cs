namespace SQLModule.Sandbox.Pooling;

/// <summary>Предоставляет разрешённые Docker-профили СУБД для прогрева локального пула.</summary>
public interface ISandboxPoolProfileSource
{
    Task<IReadOnlyCollection<SandboxDbmsSpec>> GetProfilesAsync(CancellationToken cancellationToken);
}
