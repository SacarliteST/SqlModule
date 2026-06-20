using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaAttribute;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.MetaAttributes;

internal static class MetaAttributesModule
{
    internal static IServiceCollection AddMetaAttributes(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<CreateMetaAttributeCommand, Result<MetaAttributeResponse>>, CreateMetaAttributeHandler>();
        services.AddScoped<IRequestHandler<GetMetaAttributeByIdQuery, Result<MetaAttributeResponse>>, GetMetaAttributeByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllMetaAttributesQuery, Result<PageResponse<MetaAttributeResponse>>>, GetAllMetaAttributesHandler>();
        services.AddScoped<IRequestHandler<UpdateMetaAttributeCommand, Result>, UpdateMetaAttributeHandler>();
        services.AddScoped<IRequestHandler<DeleteMetaAttributeCommand, Result>, DeleteMetaAttributeHandler>();
        return services;
    }
}
