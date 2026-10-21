using SQLModule.Common.Results;
using SQLModule.Domain.Schema;

namespace SQLModule.Application.Schema.TargetDbs;

public static class TargetDbErrors
{
    public static Error NotFound(Guid id) => DomainErrors<TargetDb>.NotFound(id);
    public static Error DbmsNotFound(Guid id) => DomainErrors<TargetDb>.Conflict($"СУБД с id '{id}' не найдена.");

    public static Error InUse(Guid id, int queryCount) => Error.Conflict(
        "TargetDb.InUse",
        $"Учебную базу нельзя удалить: на неё ссылаются эталонные запросы ({queryCount}). " +
        "Сначала удалите или перепривяжите задания, использующие эту базу.");
}
