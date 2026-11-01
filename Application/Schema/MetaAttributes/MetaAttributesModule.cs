using Microsoft.Extensions.DependencyInjection;
using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaAttribute;

namespace SQLModule.Application.Schema.MetaAttributes;

public static class MetaAttributesModule
{
    public static IServiceCollection AddMetaAttributes(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<CreateMetaAttributeCommand, Result<MetaAttributeResponse>>, CreateMetaAttributeHandler>();
        services.AddScoped<IRequestHandler<GetMetaAttributeByIdQuery, Result<MetaAttributeResponse>>, GetMetaAttributeByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllMetaAttributesQuery, Result<PageResponse<MetaAttributeResponse>>>, GetAllMetaAttributesHandler>();
        services.AddScoped<IRequestHandler<UpdateMetaAttributeCommand, Result>, UpdateMetaAttributeHandler>();
        services.AddScoped<IRequestHandler<DeleteMetaAttributeCommand, Result>, DeleteMetaAttributeHandler>();
        return services;
    }
}
