using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.ParameterDefinition;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.DbmsCatalog.ParameterDefinitions;

internal static class ParameterDefinitionsModule
{
    internal static IServiceCollection AddParameterDefinitions(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<CreateParameterDefinitionCommand, Result<ParameterDefinitionResponse>>, CreateParameterDefinitionHandler>();
        services.AddScoped<IRequestHandler<GetParameterDefinitionByIdQuery, Result<ParameterDefinitionResponse>>, GetParameterDefinitionByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllParameterDefinitionsQuery, Result<PageResponse<ParameterDefinitionResponse>>>, GetAllParameterDefinitionsHandler>();
        services.AddScoped<IRequestHandler<UpdateParameterDefinitionCommand, Result>, UpdateParameterDefinitionHandler>();
        services.AddScoped<IRequestHandler<DeleteParameterDefinitionCommand, Result>, DeleteParameterDefinitionHandler>();
        return services;
    }
}
