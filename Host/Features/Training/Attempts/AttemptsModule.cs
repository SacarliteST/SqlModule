using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Features.Training.Attempts.SubmitAttempt;

namespace SQLModule.Host.Features.Training.Attempts;

internal static class AttemptsModule
{
    internal static IServiceCollection AddAttempts(this IServiceCollection services)
    {
        services.AddScoped<IResultComparer, StrictResultComparer>();
        services.AddScoped<IRequestHandler<SubmitAttemptCommand, Result<SubmitAttemptResponse>>, SubmitAttemptHandler>();
        services.AddScoped<IRequestHandler<GetAttemptByIdQuery, Result<AttemptResponse>>, GetAttemptByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllAttemptsQuery, Result<PageResponse<AttemptResponse>>>, GetAllAttemptsHandler>();
        services.AddScoped<IRequestHandler<DeleteAttemptCommand, Result>, DeleteAttemptHandler>();
        return services;
    }
}
