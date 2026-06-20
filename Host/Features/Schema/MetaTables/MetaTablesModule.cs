using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.MetaTables;

internal static class MetaTablesModule
{
    internal static IServiceCollection AddMetaTables(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<CreateMetaTableCommand, Result<MetaTableResponse>>, CreateMetaTableHandler>();
        services.AddScoped<IRequestHandler<GetMetaTableByIdQuery, Result<MetaTableResponse>>, GetMetaTableByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllMetaTablesQuery, Result<PageResponse<MetaTableResponse>>>, GetAllMetaTablesHandler>();
        services.AddScoped<IRequestHandler<UpdateMetaTableCommand, Result>, UpdateMetaTableHandler>();
        services.AddScoped<IRequestHandler<DeleteMetaTableCommand, Result>, DeleteMetaTableHandler>();
        return services;
    }
}
