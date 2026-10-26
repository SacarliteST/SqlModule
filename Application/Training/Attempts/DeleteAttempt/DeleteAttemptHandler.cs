using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;

namespace SQLModule.Application.Training.Attempts;

/// <summary>Удаляет административно допустимую попытку.</summary>
public sealed class DeleteAttemptHandler
    : IRequestHandler<DeleteAttemptCommand, Result>
{
    private readonly IApplicationDbContext db;

    /// <summary>Создаёт обработчик удаления попытки.</summary>
    public DeleteAttemptHandler(IApplicationDbContext db)
    {
        this.db = db;
    }

    public async Task<Result> Handle(DeleteAttemptCommand command, CancellationToken ct)
    {
        var entity = await db.Attempts.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(AttemptErrors.NotFound(command.Id));
        }

        if (entity.ModuleSessionId is null)
        {
            return Result.Fail(Error.Conflict(
                "Attempt.TrainingDeletionForbidden",
                "Тренировочные попытки открытого банка хранятся бессрочно и не могут быть удалены."));
        }

        db.Attempts.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
