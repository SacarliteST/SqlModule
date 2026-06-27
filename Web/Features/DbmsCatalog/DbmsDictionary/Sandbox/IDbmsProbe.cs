using SQLModule.Common.Results;

namespace SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.Sandbox;

internal interface IDbmsProbe
{
    Task<Result> ProbeAsync(DbmsProbeSpec spec, CancellationToken ct);
}
