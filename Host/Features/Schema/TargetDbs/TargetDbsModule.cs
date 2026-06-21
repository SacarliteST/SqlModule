using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.TargetDbs;

internal static class TargetDbsModule
{
    internal static IServiceCollection AddTargetDbs(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<CreateTargetDbCommand, Result<TargetDbResponse>>, CreateTargetDbHandler>();
        services.AddScoped<IRequestHandler<GetTargetDbByIdQuery, Result<TargetDbResponse>>, GetTargetDbByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllTargetDbsQuery, Result<PageResponse<TargetDbResponse>>>, GetAllTargetDbsHandler>();
        services.AddScoped<IRequestHandler<UpdateTargetDbCommand, Result>, UpdateTargetDbHandler>();
        services.AddScoped<IRequestHandler<DeleteTargetDbCommand, Result>, DeleteTargetDbHandler>();
        return services;
    }
}
