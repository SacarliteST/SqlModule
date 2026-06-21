using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.AttributeParameterValue;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.AttributeParameterValues;

internal static class AttributeParameterValuesModule
{
    internal static IServiceCollection AddAttributeParameterValues(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<CreateAttributeParameterValueCommand, Result<AttributeParameterValueResponse>>, CreateAttributeParameterValueHandler>();
        services.AddScoped<IRequestHandler<GetAttributeParameterValueByIdQuery, Result<AttributeParameterValueResponse>>, GetAttributeParameterValueByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllAttributeParameterValuesQuery, Result<PageResponse<AttributeParameterValueResponse>>>, GetAllAttributeParameterValuesHandler>();
        services.AddScoped<IRequestHandler<UpdateAttributeParameterValueCommand, Result>, UpdateAttributeParameterValueHandler>();
        services.AddScoped<IRequestHandler<DeleteAttributeParameterValueCommand, Result>, DeleteAttributeParameterValueHandler>();
        return services;
    }
}
