using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlTasks;

internal record DeleteSqlTaskCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteSqlTaskHandler(
    AppDbContext db,
    ISqlTaskDeleteRules deleteRules,
    ILogger<DeleteSqlTaskHandler> logger)
    : IRequestHandler<DeleteSqlTaskCommand, Result>
{
    private const string ForeignKeyViolation = "23503";

    public async Task<Result> Handle(DeleteSqlTaskCommand command, CancellationToken ct)
    {
        var task = await db.SqlTasks.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (task is null)
        {
            return Result.Fail(SqlTaskErrors.NotFound(command.Id));
        }

        var blockers = await deleteRules.EvaluateAsync(command.Id, ct);
        if (blockers.Count > 0)
        {
            // Первая причина определяет code, остальные — в errors (та же форма, что у публикации).
            return Result.Fail(blockers.Count == 1
                ? blockers[0]
                : blockers[0] with
                {
                    Errors = blockers.ToDictionary(blocker => blocker.Code, blocker => new[] { blocker.Message })
                });
        }

        // Удаляем всё, что принадлежит только этому заданию, одной транзакцией: при любой ошибке
        // частично удалённого задания не остаётся.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // Задание ссылается на активную версию, а версии — на задание: разрываем цикл до удаления.
            task.ResetActiveValidationVersion();
            await db.SaveChangesAsync(ct);

            await db.TaskValidationVersions.Where(version => version.TaskId == command.Id).ExecuteDeleteAsync(ct);

            // Конфигурация проверки и её правила удаляются каскадом; эталонный запрос связан с заданием 1:1,
            // то есть принадлежит только ему, и после удаления задания остался бы «сиротой», мешающим удалению учебной базы.
            var query = await db.SqlQueries.FirstOrDefaultAsync(value => value.Id == task.SqlQueryId, ct);
            db.SqlTasks.Remove(task);
            if (query is not null)
            {
                db.SqlQueries.Remove(query);
            }

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: ForeignKeyViolation })
        {
            // Страховка: на задание появилась ссылка, о которой правила не знают, — ожидаемый конфликт, а не 500.
            logger.LogWarning(
                "Удаление задания {TaskId} отклонено внешним ключом: {Constraint}",
                command.Id,
                ((PostgresException)exception.InnerException).ConstraintName);
            db.ChangeTracker.Clear();
            return Result.Fail(SqlTaskErrors.InUse);
        }
    }
}
