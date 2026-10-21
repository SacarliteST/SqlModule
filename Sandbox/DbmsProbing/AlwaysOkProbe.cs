namespace SQLModule.Sandbox.DbmsProbing;

/// <summary>
/// Реализация <see cref="IDbmsProbe"/> без Docker: всегда возвращает Success.
/// Предназначена для изолированного режима (флаг UseFakeSandbox) и интеграционных тестов.
/// </summary>
public sealed class AlwaysOkProbe : IDbmsProbe
{
    public Task<DbmsProbeResult> ProbeAsync(DbmsProbeSpec spec, CancellationToken ct) =>
        Task.FromResult(DbmsProbeResult.Success());
}
