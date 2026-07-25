using Microsoft.Extensions.DependencyInjection;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlQueries;

internal static class SqlQueriesModule
{
    internal static IServiceCollection AddSqlQueries(this IServiceCollection services)
    {
        services.AddScoped<ISqlQueryValidationRunner, SqlQueryValidationRunner>();
        services.AddScoped<IRequestHandler<CreateSqlQueryCommand, Result<SqlQueryResponse>>, CreateSqlQueryHandler>();
        services.AddScoped<IRequestHandler<GetSqlQueryByIdQuery, Result<SqlQueryResponse>>, GetSqlQueryByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllSqlQueriesQuery, Result<PageResponse<SqlQueryResponse>>>, GetAllSqlQueriesHandler>();
        services.AddScoped<IRequestHandler<ValidateSqlQueryCommand, Result<ValidateSqlQueryResponse>>, ValidateSqlQueryHandler>();
        services.AddScoped<IRequestHandler<UpdateSqlQueryCommand, Result>, UpdateSqlQueryHandler>();
        services.AddScoped<IRequestHandler<DeleteSqlQueryCommand, Result>, DeleteSqlQueryHandler>();
        return services;
    }
}
