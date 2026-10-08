using Microsoft.Extensions.DependencyInjection;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Training.SqlTasks.GetTeacherTaskDetails;

namespace SQLModule.Application.Training.SqlTasks;

public static class SqlTasksModule
{
    public static IServiceCollection AddSqlTasks(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<CreateSqlTaskCommand, Result<SqlTaskResponse>>, CreateSqlTaskHandler>();
        services.AddScoped<IRequestHandler<GetSqlTaskByIdQuery, Result<SqlTaskResponse>>, GetSqlTaskByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllSqlTasksQuery, Result<PageResponse<SqlTaskResponse>>>, GetAllSqlTasksHandler>();
        services.AddScoped<IRequestHandler<GetTeacherTaskDetailsQuery, Result<TeacherTaskDetailsResponse>>, GetTeacherTaskDetailsHandler>();
        services.AddScoped<IRequestHandler<UpdateSqlTaskCommand, Result>, UpdateSqlTaskHandler>();
        services.AddScoped<IRequestHandler<UpdateTaskReferenceQueryCommand, Result<UpdateTaskReferenceQueryResponse>>, UpdateTaskReferenceQueryHandler>();
        services.AddScoped<ISqlTaskPublishReadiness, SqlTaskPublishReadiness>();
        services.AddScoped<ISqlTaskDeleteRules, SqlTaskDeleteRules>();
        services.AddScoped<IRequestHandler<PublishSqlTaskCommand, Result<SqlTaskResponse>>, PublishSqlTaskHandler>();
        services.AddScoped<IRequestHandler<ArchiveSqlTaskCommand, Result<SqlTaskResponse>>, ArchiveSqlTaskHandler>();
        services.AddScoped<IRequestHandler<DeleteSqlTaskCommand, Result>, DeleteSqlTaskHandler>();
        return services;
    }
}
