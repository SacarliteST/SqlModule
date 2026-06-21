using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaRelationship;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.MetaRelationships;

internal static class MetaRelationshipsModule
{
    internal static IServiceCollection AddMetaRelationships(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<CreateMetaRelationshipCommand, Result<MetaRelationshipResponse>>, CreateMetaRelationshipHandler>();
        services.AddScoped<IRequestHandler<GetMetaRelationshipByIdQuery, Result<MetaRelationshipResponse>>, GetMetaRelationshipByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllMetaRelationshipsQuery, Result<PageResponse<MetaRelationshipResponse>>>, GetAllMetaRelationshipsHandler>();
        services.AddScoped<IRequestHandler<UpdateMetaRelationshipCommand, Result>, UpdateMetaRelationshipHandler>();
        services.AddScoped<IRequestHandler<DeleteMetaRelationshipCommand, Result>, DeleteMetaRelationshipHandler>();
        return services;
    }
}
