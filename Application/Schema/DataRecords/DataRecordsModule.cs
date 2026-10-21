using Microsoft.Extensions.DependencyInjection;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.DataRecord;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Schema.DataRecords;

public static class DataRecordsModule
{
    public static IServiceCollection AddDataRecords(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<CreateDataRecordCommand, Result<DataRecordResponse>>, CreateDataRecordHandler>();
        services.AddScoped<IRequestHandler<GetDataRecordByIdQuery, Result<DataRecordResponse>>, GetDataRecordByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllDataRecordsQuery, Result<PageResponse<DataRecordResponse>>>, GetAllDataRecordsHandler>();
        services.AddScoped<IRequestHandler<UpdateDataRecordCommand, Result>, UpdateDataRecordHandler>();
        services.AddScoped<IRequestHandler<DeleteDataRecordCommand, Result>, DeleteDataRecordHandler>();
        return services;
    }
}
