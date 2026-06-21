using SQLModule.Common.Results;
using SQLModule.Domain.Schema;

namespace SQLModule.Host.Features.Schema.DataRecords;

internal static class DataRecordErrors
{
    internal static Error NotFound(Guid id) =>
        DomainErrors<DataRecord>.NotFound(id);

    internal static Error MetaTableNotFound(Guid id) =>
        DomainErrors<DataRecord>.Conflict($"Таблица '{id}' не найдена.");
}
