namespace SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.Sandbox;

internal sealed class DbmsCatalogOptions
{
    public const string SectionKey = "DbmsCatalog";

    public bool ProbeEnabled { get; init; } = true;
    public int ProbeTimeoutSeconds { get; init; } = 120;
    public TimeSpan ProbeCacheTtl { get; init; } = TimeSpan.FromHours(1);
}
