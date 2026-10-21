using SQLModule.Common.Results;
using SQLModule.Domain.Schema;

namespace SQLModule.Application.Schema.DataRecords;

public static class DataRecordErrors
{
    public static Error NotFound(Guid id) =>
        DomainErrors<DataRecord>.NotFound(id);

    public static Error MetaTableNotFound(Guid id) =>
        DomainErrors<DataRecord>.Conflict($"Таблица '{id}' не найдена.");
}
