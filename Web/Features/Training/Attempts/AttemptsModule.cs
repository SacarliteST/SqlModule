using Microsoft.Extensions.DependencyInjection;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Web.Common.Cqrs;
using SQLModule.Web.Features.Training.Attempts.SubmitAttempt;

namespace SQLModule.Web.Features.Training.Attempts;

internal static class AttemptsModule
{
    internal static IServiceCollection AddAttempts(this IServiceCollection services)
    {
        services.AddScoped<IResultComparer, StrictResultComparer>();
        services.AddScoped<IAttemptResultSnapshotService, AttemptResultSnapshotService>();
        services.AddScoped<IAttemptScoringReadService, AttemptScoringReadService>();
        services.AddScoped<IPhase2bValidationRuntimeReader, Phase2bValidationRuntimeReader>();
        services.AddScoped<IPhase2bSubmitAttemptService, Phase2bSubmitAttemptService>();
        services.AddScoped<IRequestHandler<SubmitAttemptCommand, Result<SubmitAttemptResponse>>, SubmitAttemptHandler>();
        services.AddScoped<IRequestHandler<GetAttemptByIdQuery, Result<AttemptResponse>>, GetAttemptByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllAttemptsQuery, Result<PageResponse<AttemptListItemResponse>>>, GetAllAttemptsHandler>();
        services.AddScoped<IRequestHandler<DeleteAttemptCommand, Result>, DeleteAttemptHandler>();
        return services;
    }
}
