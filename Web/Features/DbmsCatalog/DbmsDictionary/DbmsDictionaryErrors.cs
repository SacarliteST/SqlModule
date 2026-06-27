using SQLModule.Common.Results;
using DomainDbms = SQLModule.Domain.DbmsCatalog.DbmsDictionary;

namespace SQLModule.Web.Features.DbmsCatalog.DbmsDictionary;

internal static class DbmsDictionaryErrors
{
    internal static Error NotFound(Guid id) =>
        DomainErrors<DomainDbms>.NotFound(id);

    internal static Error AlreadyExists(string name) =>
        DomainErrors<DomainDbms>.Conflict($"СУБД с именем '{name}' уже существует.");

    internal static Error InUse =>
        DomainErrors<DomainDbms>.Conflict("СУБД используется целевыми БД или физическими типами и не может быть удалена.");

    internal static Error ProbeFailed(string message) =>
        DomainErrors<DomainDbms>.Conflict($"Проверка Docker-конфигурации не прошла: {message}");
}
