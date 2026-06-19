using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.Attempts;

internal static class AttemptsModule
{
    internal static IServiceCollection AddAttempts(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<CreateAttemptCommand, Result<AttemptResponse>>, CreateAttemptHandler>();
        services.AddScoped<IRequestHandler<GetAttemptByIdQuery, Result<AttemptResponse>>, GetAttemptByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllAttemptsQuery, Result<PageResponse<AttemptResponse>>>, GetAllAttemptsHandler>();
        services.AddScoped<IRequestHandler<UpdateAttemptCommand, Result>, UpdateAttemptHandler>();
        services.AddScoped<IRequestHandler<DeleteAttemptCommand, Result>, DeleteAttemptHandler>();
        return services;
    }
}
