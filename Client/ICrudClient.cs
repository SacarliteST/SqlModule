using SQLModule.Contracts;

namespace SQLModule.Client;

/// <summary>
/// Типизированный CRUD-клиент для работы с коллекцией ресурсов.
/// </summary>
/// <typeparam name="TCreateRequest">DTO запроса на создание.</typeparam>
/// <typeparam name="TUpdateRequest">DTO запроса на обновление.</typeparam>
/// <typeparam name="TResponse">DTO ответа.</typeparam>
public interface ICrudClient<TCreateRequest, TUpdateRequest, TResponse>
    where TCreateRequest : class
    where TUpdateRequest : class
    where TResponse : class
{
    /// <summary>Создать ресурс. Бросает <see cref="ConflictException"/> при нарушении FK.</summary>
    Task<TResponse> CreateAsync(TCreateRequest request, CancellationToken ct = default);

    /// <summary>Получить ресурс по Id. Возвращает <see langword="null"/>, если не найден (404).</summary>
    Task<TResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Получить страницу ресурсов.</summary>
    Task<PageResponse<TResponse>> GetAllAsync(int offset, int limit, CancellationToken ct = default);

    /// <summary>Обновить ресурс. Бросает <see cref="NotFoundException"/>, если ресурс не найден.</summary>
    Task UpdateAsync(Guid id, TUpdateRequest request, CancellationToken ct = default);

    /// <summary>Удалить ресурс. Идемпотентен: не бросает исключение при отсутствии ресурса.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
