using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace SQLModule.Data.Core;

/// <summary>Разбор ошибок провайдера БД, чтобы код выше Data не зависел от Npgsql.</summary>
public static class DbUpdateExceptionExtensions
{
    private const string ForeignKeyViolationSqlState = "23503";

    /// <summary>Определяет, что запись отклонена внешним ключом, и возвращает имя нарушенного ограничения.</summary>
    public static bool IsForeignKeyViolation(this DbUpdateException exception, out string? constraintName)
    {
        if (exception.InnerException is PostgresException { SqlState: ForeignKeyViolationSqlState } postgres)
        {
            constraintName = postgres.ConstraintName;
            return true;
        }

        constraintName = null;
        return false;
    }
}
