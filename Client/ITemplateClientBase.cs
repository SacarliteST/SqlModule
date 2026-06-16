using Contracts;

namespace TemplateApi.Client;

/// <summary>
/// Предоставляет методы запросов для взаимодействия с элементами склада
/// </summary>
public interface ITemplateClientBase<TResponse, TDetailedResponse, TRequest, TKey>
    where TResponse : class
    where TDetailedResponse : class
    where TRequest : class
{
    /// <summary>
    /// Получить элемент по идентификатору
    /// </summary>
    public Task<TDetailedResponse?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить страницу
    /// </summary>
    public Task<PageResponse<TResponse>> GetWithPagingAsync(
        int limit,
        int offset,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Создать элемент
    /// </summary>
    public Task<TResponse> CreateAsync(
        TRequest createRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновить элемент
    /// </summary>
    public Task<TResponse?> UpdateAsync(
        TKey id,
        TRequest updateRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить элемент
    /// </summary>
    public Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);
}
