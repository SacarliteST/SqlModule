using SQLModule.Contracts.Schema.DataRecord;

namespace SQLModule.Client.DataRecord;

/// <summary>Типизированный клиент для работы со строками данных (EAV-якоря).</summary>
public interface IDataRecordClient
    : ICrudClient<CreateDataRecordRequest, UpdateDataRecordRequest, DataRecordResponse>;
