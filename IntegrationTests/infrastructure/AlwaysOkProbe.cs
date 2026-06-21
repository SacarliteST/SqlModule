using SQLModule.Common.Results;
using SQLModule.Host.Features.DbmsCatalog.DbmsDictionary.Sandbox;

namespace SQLModule.IntegrationTests.infrastructure;

internal sealed class AlwaysOkProbe : IDbmsProbe
{
    public Task<Result> ProbeAsync(DbmsProbeSpec spec, CancellationToken ct)
        => Task.FromResult(Result.Success());
}
