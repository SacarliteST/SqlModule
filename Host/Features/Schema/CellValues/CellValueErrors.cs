using SQLModule.Common.Results;
using SQLModule.Domain.Schema;

namespace SQLModule.Host.Features.Schema.CellValues;

internal static class CellValueErrors
{
    internal static Error NotFound(Guid id) => DomainErrors<CellValue>.NotFound(id);
    internal static Error DataRecordNotFound(Guid id) => DomainErrors<CellValue>.Conflict($"Строка данных с id '{id}' не найдена.");
    internal static Error MetaAttributeNotFound(Guid id) => DomainErrors<CellValue>.Conflict($"Мета-атрибут с id '{id}' не найден.");
}
