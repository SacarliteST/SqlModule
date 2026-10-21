using Microsoft.Extensions.DependencyInjection;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Schema.MetaTables;

public static class MetaTablesModule
{
    public static IServiceCollection AddMetaTables(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<CreateMetaTableCommand, Result<MetaTableResponse>>, CreateMetaTableHandler>();
        services.AddScoped<IRequestHandler<GetMetaTableByIdQuery, Result<MetaTableResponse>>, GetMetaTableByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllMetaTablesQuery, Result<PageResponse<MetaTableResponse>>>, GetAllMetaTablesHandler>();
        services.AddScoped<IRequestHandler<UpdateMetaTableCommand, Result>, UpdateMetaTableHandler>();
        services.AddScoped<IRequestHandler<DeleteMetaTableCommand, Result>, DeleteMetaTableHandler>();
        return services;
    }
}
