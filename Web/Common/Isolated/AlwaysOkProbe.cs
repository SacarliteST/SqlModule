using SQLModule.Common.Results;
using SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.Sandbox;

namespace SQLModule.Web.Common.Isolated;

/// <summary>
/// Реализация <see cref="IDbmsProbe"/> без Docker: всегда возвращает Success.
/// Предназначена для изолированного режима (флаг UseFakeSandbox) и интеграционных тестов.
/// </summary>
internal sealed class AlwaysOkProbe : IDbmsProbe
{
    public Task<Result> ProbeAsync(DbmsProbeSpec spec, CancellationToken ct) =>
        Task.FromResult(Result.Success());
}
