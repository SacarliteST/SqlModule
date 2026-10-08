using SQLModule.Common.Results;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.SqlQueries;

public static class SqlQueryErrors
{
    public static Error NotFound(Guid id) => DomainErrors<SqlQuery>.NotFound(id);
    public static Error InUse => DomainErrors<SqlQuery>.Conflict("Запрос используется заданиями или попытками.");

    public static Error ReferenceInvalid(string detail) =>
        DomainErrors<SqlQuery>.Validation($"Эталонный запрос вернул ошибку: {detail}");
}
