using Microsoft.Extensions.DependencyInjection;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.CellValue;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.CellValues;

internal static class CellValuesModule
{
    internal static IServiceCollection AddCellValues(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<CreateCellValueCommand, Result<CellValueResponse>>, CreateCellValueHandler>();
        services.AddScoped<IRequestHandler<GetCellValueByIdQuery, Result<CellValueResponse>>, GetCellValueByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllCellValuesQuery, Result<PageResponse<CellValueResponse>>>, GetAllCellValuesHandler>();
        services.AddScoped<IRequestHandler<UpdateCellValueCommand, Result<CellValueResponse>>, UpdateCellValueHandler>();
        services.AddScoped<IRequestHandler<DeleteCellValueCommand, Result>, DeleteCellValueHandler>();
        return services;
    }
}
