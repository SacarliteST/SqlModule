using SQLModule.Sandbox;

namespace SQLModule.Sandbox.Dialects;

internal sealed class SqlDialectFactory(IEnumerable<ISqlDialect> dialectList)
    : ISqlDialectFactory, ISqlSyntaxFactory
{
    private static readonly Dictionary<string, string> Aliases =
        new(StringComparer.OrdinalIgnoreCase) { ["mariadb"] = "mysql" };

    private readonly IReadOnlyDictionary<string, ISqlDialect> dialects =
        dialectList.ToDictionary(d => d.SystemName, StringComparer.OrdinalIgnoreCase);

    public ISqlDialect? GetDialectFor(string systemName) => Resolve(systemName);

    public ISqlSyntax? For(string systemName) => Resolve(systemName);

    private ISqlDialect? Resolve(string systemName)
    {
        var key = Aliases.TryGetValue(systemName, out var alias) ? alias : systemName;
        return dialects.GetValueOrDefault(key);
    }
}
