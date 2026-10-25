using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SQLModule.Application.Behaviors;
using SQLModule.Application.Auth;
using SQLModule.Application.Training.Student;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Contracts.Training.Student;
using SQLModule.Common.Results;
using SQLModule.Application.Cqrs;
using SQLModule.Application.DbmsCatalog.DbmsDictionary;
using SQLModule.Application.DbmsCatalog.ParameterDefinitions;
using SQLModule.Application.DbmsCatalog.PhysicalTypes;
using SQLModule.Application.ModuleIntegration;
using SQLModule.Application.Schema.AttributeParameterValues;
using SQLModule.Application.Schema.CellValues;
using SQLModule.Application.Schema.DataRecords;
using SQLModule.Application.Schema.MetaAttributes;
using SQLModule.Application.Schema.MetaRelationships;
using SQLModule.Application.Schema.MetaTables;
using SQLModule.Application.Schema.TargetDbs;
using SQLModule.Application.Schema.SchemaBuilder;
using SQLModule.Application.TaskMaterialization;
using SQLModule.Application.Training.SqlQueries;
using SQLModule.Application.Training.SqlTasks;
using SQLModule.Application.Training.Topics;
using SQLModule.Application.Training.Attempts;
using SQLModule.Application.Training.Validation;
using SQLModule.Application.Training.Bank;
using SQLModule.Sandbox.Pooling;

namespace SQLModule.Application;

/// <summary>Точка подключения прикладного слоя: CQRS-ядро и обработчики сценариев.</summary>
public static class ApplicationExtensions
{
    /// <summary>Регистрирует <see cref="ISender"/>, pipeline-поведения и обработчики сценариев.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AttemptResultSnapshotsOptions>(
            configuration.GetSection(AttemptResultSnapshotsOptions.SectionKey));
        services.AddOptions<TaskBankOptions>()
            .Bind(configuration.GetSection(TaskBankOptions.SectionKey))
            .Validate(options => options.HintMaxLength > 0,
                "TaskBank:HintMaxLength должен быть больше нуля.")
            .Validate(options => options.SolutionExplanationMaxLength > 0,
                "TaskBank:SolutionExplanationMaxLength должен быть больше нуля.")
            .Validate(options => options.SubmissionIntervalSeconds > 0,
                "TaskBank:SubmissionIntervalSeconds должен быть больше нуля.")
            .Validate(options => options.RetryAfterSeconds > 0,
                "TaskBank:RetryAfterSeconds должен быть больше нуля.")
            .ValidateOnStart();
        services.AddModuleIntegration(configuration);
        services.AddScoped<ISender, Sender>();
        services.AddScoped<IRequestHandler<StandaloneLoginCommand, Result<StandaloneAuthResult>>, StandaloneLoginHandler>();
        services.AddScoped<IRequestHandler<StandaloneRefreshCommand, Result<StandaloneAuthResult>>, StandaloneRefreshHandler>();
        services.AddScoped<IRequestHandler<StandaloneLogoutCommand, Result>, StandaloneLogoutHandler>();
        services.AddScoped<IRequestHandler<GetStudentTopicsQuery, Result<PageResponse<StudentTopicResponse>>>, GetStudentTopicsHandler>();
        services.AddScoped<IRequestHandler<GetStudentTasksQuery, Result<PageResponse<StudentTaskResponse>>>, GetStudentTasksHandler>();
        services.AddScoped<IRequestHandler<GetStudentTaskByIdQuery, Result<StudentTaskDetailsResponse>>, GetStudentTaskByIdHandler>();
        services.AddScoped<IRequestHandler<GetStudentTaskSchemaQuery, Result<StudentTaskSchemaResponse>>, GetStudentTaskSchemaHandler>();
        services.AddScoped<IRequestHandler<GetStudentTaskTableRowsQuery, Result<TableRowsResponse>>, GetStudentTaskTableRowsHandler>();
        services.AddScoped<IRequestHandler<GetStudentAttemptsQuery, Result<PageResponse<StudentAttemptListItemResponse>>>, GetStudentAttemptsHandler>();
        services.AddScoped<IRequestHandler<GetStudentAttemptByIdQuery, Result<StudentAttemptResponse>>, GetStudentAttemptByIdHandler>();
        services.AddScoped<IStudentTaskAccess, StudentTaskAccess>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped<ISqlQueryValidationRunner, SqlQueryValidationRunner>();
        services.AddScoped<ISandboxPoolProfileSource, DbmsSandboxPoolProfileSource>();
        services.AddScoped<ITaskValidationSnapshotFactory, TaskValidationSnapshotFactory>();
        return services
            .AddTaskMaterializer()
            .AddSqlTasks()
            .AddDbmsDictionaries()
            .AddPhysicalTypes()
            .AddParameterDefinitions()
            .AddTargetDbs()
            .AddMetaTables()
            .AddMetaAttributes()
            .AddMetaRelationships()
            .AddDataRecords()
            .AddCellValues()
            .AddAttributeParameterValues()
            .AddTopics()
            .AddSqlQueries()
            .AddTaskValidation()
            .AddSchemaBuilder()
            .AddAttempts();
    }
}
