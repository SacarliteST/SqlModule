using SQLModule.Common.Results;
using DomainDbms = SQLModule.Domain.DbmsCatalog.DbmsDictionary;

namespace SQLModule.Application.DbmsCatalog.DbmsDictionary;

public static class DbmsDictionaryErrors
{
    public static Error NotFound(Guid id) =>
        DomainErrors<DomainDbms>.NotFound(id);

    public static Error AlreadyExists(string name) =>
        DomainErrors<DomainDbms>.Conflict($"СУБД с именем '{name}' уже существует.");

    public static Error InUse =>
        DomainErrors<DomainDbms>.Conflict("СУБД используется целевыми БД или физическими типами и не может быть удалена.");

    public static Error ProbeFailed(string message) =>
        DomainErrors<DomainDbms>.Conflict($"Проверка Docker-конфигурации не прошла: {message}");
}
