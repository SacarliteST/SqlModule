using Microsoft.Extensions.DependencyInjection;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.AttributeParameterValue;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Schema.AttributeParameterValues;

public static class AttributeParameterValuesModule
{
    public static IServiceCollection AddAttributeParameterValues(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<CreateAttributeParameterValueCommand, Result<AttributeParameterValueResponse>>, CreateAttributeParameterValueHandler>();
        services.AddScoped<IRequestHandler<GetAttributeParameterValueByIdQuery, Result<AttributeParameterValueResponse>>, GetAttributeParameterValueByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllAttributeParameterValuesQuery, Result<PageResponse<AttributeParameterValueResponse>>>, GetAllAttributeParameterValuesHandler>();
        services.AddScoped<IRequestHandler<UpdateAttributeParameterValueCommand, Result>, UpdateAttributeParameterValueHandler>();
        services.AddScoped<IRequestHandler<DeleteAttributeParameterValueCommand, Result>, DeleteAttributeParameterValueHandler>();
        return services;
    }
}
