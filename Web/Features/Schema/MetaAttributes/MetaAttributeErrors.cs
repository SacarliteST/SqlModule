using SQLModule.Common.Results;
using SQLModule.Domain.Schema;

namespace SQLModule.Web.Features.Schema.MetaAttributes;

internal static class MetaAttributeErrors
{
    internal static Error NotFound(Guid id) =>
        DomainErrors<MetaAttribute>.NotFound(id);

    internal static Error MetaTableNotFound(Guid id) =>
        DomainErrors<MetaAttribute>.Conflict($"Таблица '{id}' не найдена.");

    internal static Error PhysicalTypeNotFound(Guid id) =>
        DomainErrors<MetaAttribute>.Conflict($"Физический тип '{id}' не найден.");

    internal static Error InUse =>
        DomainErrors<MetaAttribute>.Conflict("Колонка участвует в связи (FK) и не может быть удалена.");
}
