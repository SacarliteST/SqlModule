using SQLModule.Common.Results;
using SQLModule.Domain.Schema;

namespace SQLModule.Web.Features.Schema.MetaTables;

internal static class MetaTableErrors
{
    internal static Error NotFound(Guid id) => DomainErrors<MetaTable>.NotFound(id);
    internal static Error TargetDbNotFound(Guid id) => DomainErrors<MetaTable>.Conflict($"Целевая БД '{id}' не найдена.");
    internal static Error InUse => DomainErrors<MetaTable>.Conflict("Таблица используется связями (FK) и не может быть удалена.");
}
