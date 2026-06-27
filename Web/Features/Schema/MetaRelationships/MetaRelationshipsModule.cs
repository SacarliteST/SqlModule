using Microsoft.Extensions.DependencyInjection;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaRelationship;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.MetaRelationships;

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
