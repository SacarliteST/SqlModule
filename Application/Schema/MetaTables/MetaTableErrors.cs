using SQLModule.Common.Results;
using SQLModule.Domain.Schema;

namespace SQLModule.Application.Schema.MetaTables;

public static class MetaTableErrors
{
    public static Error NotFound(Guid id) => DomainErrors<MetaTable>.NotFound(id);
    public static Error TargetDbNotFound(Guid id) => DomainErrors<MetaTable>.Conflict($"Целевая БД '{id}' не найдена.");
    public static Error InUse => DomainErrors<MetaTable>.Conflict("Таблица используется связями (FK) и не может быть удалена.");
}
