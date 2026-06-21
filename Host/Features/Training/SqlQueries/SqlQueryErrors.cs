using SQLModule.Common.Results;
using SQLModule.Domain.Training;

namespace SQLModule.Host.Features.Training.SqlQueries;

internal static class SqlQueryErrors
{
    internal static Error NotFound(Guid id) => DomainErrors<SqlQuery>.NotFound(id);
    internal static Error InUse => DomainErrors<SqlQuery>.Conflict("Запрос используется заданиями или попытками.");
}
