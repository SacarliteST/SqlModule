using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;

namespace SQLModule.Client.Attempt;

/// <summary>Клиент для работы с попытками выполнения заданий.</summary>
public interface IAttemptClient
{
    /// <summary>Отправить попытку. Бросает <see cref="NotFoundException"/> если задание не найдено,
    /// <see cref="ConflictException"/> если эталонный результат ещё не готов.</summary>
    Task<SubmitAttemptResponse> SubmitAsync(SubmitAttemptRequest request, CancellationToken ct = default);

    /// <summary>Получить попытку по Id. Возвращает <see langword="null"/>, если не найдена.</summary>
    Task<AttemptResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Получить страницу попыток.</summary>
    Task<PageResponse<AttemptResponse>> GetAllAsync(int offset, int limit, CancellationToken ct = default);

    /// <summary>Удалить попытку. Идемпотентен.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
