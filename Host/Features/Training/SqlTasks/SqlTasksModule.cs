using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Training.SqlTasks;

internal static class SqlTasksModule
{
    internal static IServiceCollection AddSqlTasks(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<CreateSqlTaskCommand, Result<SqlTaskResponse>>, CreateSqlTaskHandler>();
        services.AddScoped<IRequestHandler<GetSqlTaskByIdQuery, Result<SqlTaskResponse>>, GetSqlTaskByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllSqlTasksQuery, Result<PageResponse<SqlTaskResponse>>>, GetAllSqlTasksHandler>();
        services.AddScoped<IRequestHandler<UpdateSqlTaskCommand, Result>, UpdateSqlTaskHandler>();
        services.AddScoped<IRequestHandler<DeleteSqlTaskCommand, Result>, DeleteSqlTaskHandler>();
        return services;
    }
}
