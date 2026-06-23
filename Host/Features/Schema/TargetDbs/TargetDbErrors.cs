using SQLModule.Common.Results;
using SQLModule.Domain.Schema;

namespace SQLModule.Host.Features.Schema.TargetDbs;

internal static class TargetDbErrors
{
    internal static Error NotFound(Guid id) => DomainErrors<TargetDb>.NotFound(id);
    internal static Error DbmsNotFound(Guid id) => DomainErrors<TargetDb>.Conflict($"СУБД с id '{id}' не найдена.");
}
