using SQLModule.Common.Results;
using SQLModule.Domain.Schema;

namespace SQLModule.Application.Schema.CellValues;

public static class CellValueErrors
{
    public static Error NotFound(Guid id) => DomainErrors<CellValue>.NotFound(id);
    public static Error DataRecordNotFound(Guid id) => DomainErrors<CellValue>.Conflict($"Строка данных с id '{id}' не найдена.");
    public static Error MetaAttributeNotFound(Guid id) => DomainErrors<CellValue>.Conflict($"Мета-атрибут с id '{id}' не найден.");
}
