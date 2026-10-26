using Microsoft.Extensions.DependencyInjection;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Training.Attempts.GetAttemptFilterOptions;
using SQLModule.Application.Training.Attempts.SubmitAttempt;
using SQLModule.Application.Training.Bank;

namespace SQLModule.Application.Training.Attempts;

public static class AttemptsModule
{
    public static IServiceCollection AddAttempts(this IServiceCollection services)
    {
        services.AddScoped<IResultComparer, StrictResultComparer>();
        services.AddScoped<IAttemptResultSnapshotService, AttemptResultSnapshotService>();
        services.AddScoped<IAttemptScoringReadService, AttemptScoringReadService>();
        services.AddScoped<IValidationRuntimeReader, ValidationRuntimeReader>();
        services.AddScoped<ISubmitAttemptWorkflow, SessionAttemptWorkflow>();
        services.AddScoped<AttemptEvaluationEngine>();
        services.AddSingleton<ITrainingAttemptGate, TrainingAttemptGate>();
        services.AddScoped<IRequestHandler<SubmitAttemptCommand, Result<SubmitAttemptResponse>>, SubmitAttemptHandler>();
        services.AddScoped<IRequestHandler<GetAttemptByIdQuery, Result<AttemptResponse>>, GetAttemptByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllAttemptsQuery, Result<PageResponse<AttemptListItemResponse>>>, GetAllAttemptsHandler>();
        services.AddScoped<IRequestHandler<DeleteAttemptCommand, Result>, DeleteAttemptHandler>();
        services.AddScoped<IRequestHandler<GetAttemptStudentFilterOptionsQuery, Result<PageResponse<AttemptStudentFilterOptionResponse>>>, GetAttemptStudentFilterOptionsHandler>();
        services.AddScoped<IRequestHandler<GetAttemptTopicFilterOptionsQuery, Result<PageResponse<AttemptTopicFilterOptionResponse>>>, GetAttemptTopicFilterOptionsHandler>();
        services.AddScoped<IRequestHandler<GetAttemptTaskFilterOptionsQuery, Result<PageResponse<AttemptTaskFilterOptionResponse>>>, GetAttemptTaskFilterOptionsHandler>();
        return services;
    }
}
