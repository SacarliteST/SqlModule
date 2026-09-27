using SQLModule.Common.Results;
using SQLModule.Domain.Schema;

namespace SQLModule.Web.Features.Schema.TargetDbs;

internal static class TargetDbErrors
{
    internal static Error NotFound(Guid id) => DomainErrors<TargetDb>.NotFound(id);
    internal static Error DbmsNotFound(Guid id) => DomainErrors<TargetDb>.Conflict($"СУБД с id '{id}' не найдена.");

    internal static Error InUse(Guid id, int queryCount) => Error.Conflict(
        "TargetDb.InUse",
        $"Учебную базу нельзя удалить: на неё ссылаются эталонные запросы ({queryCount}). " +
        "Сначала удалите или перепривяжите задания, использующие эту базу.");
}
