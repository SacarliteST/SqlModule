namespace SQLModule.Sandbox.Dialects;

internal interface ISqlDialectFactory
{
    ISqlDialect? GetDialectFor(string systemName);
}
