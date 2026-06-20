using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.PhysicalType;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.DbmsCatalog.PhysicalTypes;

internal static class PhysicalTypesModule
{
    internal static IServiceCollection AddPhysicalTypes(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<CreatePhysicalTypeCommand, Result<PhysicalTypeResponse>>, CreatePhysicalTypeHandler>();
        services.AddScoped<IRequestHandler<GetPhysicalTypeByIdQuery, Result<PhysicalTypeResponse>>, GetPhysicalTypeByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllPhysicalTypesQuery, Result<PageResponse<PhysicalTypeResponse>>>, GetAllPhysicalTypesHandler>();
        services.AddScoped<IRequestHandler<UpdatePhysicalTypeCommand, Result>, UpdatePhysicalTypeHandler>();
        services.AddScoped<IRequestHandler<DeletePhysicalTypeCommand, Result>, DeletePhysicalTypeHandler>();
        return services;
    }
}
