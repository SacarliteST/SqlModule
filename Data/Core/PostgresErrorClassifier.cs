using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Persistence;

namespace SQLModule.Data.Core;

/// <summary>Классификатор ошибок PostgreSQL.</summary>
internal sealed class PostgresErrorClassifier : IDbErrorClassifier
{
    public bool IsForeignKeyViolation(DbUpdateException exception, out string? constraintName) =>
        exception.IsForeignKeyViolation(out constraintName);
}
