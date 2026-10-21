using SQLModule.Common.Results;
using SQLModule.Domain.Schema;

namespace SQLModule.Application.Schema.MetaAttributes;

public static class MetaAttributeErrors
{
    public static Error NotFound(Guid id) =>
        DomainErrors<MetaAttribute>.NotFound(id);

    public static Error MetaTableNotFound(Guid id) =>
        DomainErrors<MetaAttribute>.Conflict($"Таблица '{id}' не найдена.");

    public static Error PhysicalTypeNotFound(Guid id) =>
        DomainErrors<MetaAttribute>.Conflict($"Физический тип '{id}' не найден.");

    public static Error InUse =>
        DomainErrors<MetaAttribute>.Conflict("Колонка участвует в связи (FK) и не может быть удалена.");
}
