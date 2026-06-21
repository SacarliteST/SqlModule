using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Training.SqlQueries;

internal static class SqlQueriesModule
{
    internal static IServiceCollection AddSqlQueries(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<CreateSqlQueryCommand, Result<SqlQueryResponse>>, CreateSqlQueryHandler>();
        services.AddScoped<IRequestHandler<GetSqlQueryByIdQuery, Result<SqlQueryResponse>>, GetSqlQueryByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllSqlQueriesQuery, Result<PageResponse<SqlQueryResponse>>>, GetAllSqlQueriesHandler>();
        services.AddScoped<IRequestHandler<UpdateSqlQueryCommand, Result>, UpdateSqlQueryHandler>();
        services.AddScoped<IRequestHandler<DeleteSqlQueryCommand, Result>, DeleteSqlQueryHandler>();
        return services;
    }
}
