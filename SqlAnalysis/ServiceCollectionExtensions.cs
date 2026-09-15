using Microsoft.Extensions.DependencyInjection;
using SQLModule.Domain.Training.Validation;

namespace SQLModule.SqlAnalysis;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlAnalysis(this IServiceCollection services)
    {
        services.AddSingleton<ISqlSyntaxAnalyzer>(
            _ => new SqlParserSyntaxAnalyzer(SqlAnalyzerDialect.PostgreSql));
        services.AddSingleton<ISqlSyntaxAnalyzer>(
            _ => new SqlParserSyntaxAnalyzer(SqlAnalyzerDialect.MySql));
        services.AddSingleton<ISqlSyntaxAnalyzerResolver, SqlSyntaxAnalyzerResolver>();
        return services;
    }
}
