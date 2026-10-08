using Microsoft.EntityFrameworkCore;

namespace SQLModule.Application.Persistence;

/// <summary>Разбирает ошибки провайдера БД, чтобы прикладной код не зависел от конкретного драйвера.</summary>
public interface IDbErrorClassifier
{
    /// <summary>Определяет, что запись отклонена внешним ключом, и возвращает имя нарушенного ограничения.</summary>
    bool IsForeignKeyViolation(DbUpdateException exception, out string? constraintName);
}
