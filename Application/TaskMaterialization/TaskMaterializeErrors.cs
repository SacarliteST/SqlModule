using SQLModule.Common.Results;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Schema;
using DomainTargetDb = SQLModule.Domain.Schema.TargetDb;

namespace SQLModule.Application.TaskMaterialization;

public static class TaskMaterializeErrors
{
    public static Error TargetDbNotFound(Guid id) =>
        DomainErrors<DomainTargetDb>.Conflict($"Целевая БД с id '{id}' не найдена.");

    public static Error UnsupportedDbms(string name) =>
        DomainErrors<DomainTargetDb>.Conflict($"Диалект SQL для СУБД '{name}' не поддерживается.");
}
