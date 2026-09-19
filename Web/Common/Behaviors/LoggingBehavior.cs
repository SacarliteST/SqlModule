using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SQLModule.Common.Results;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Common.Behaviors;

/// <summary>
/// Pipeline-поведение, которое логирует имя запроса, время выполнения и признак успеха.
/// При выбрасывании исключения пишет Debug и повторно пробрасывает — ошибку логирует глобальный обработчик.
/// </summary>
/// <typeparam name="TRequest">Тип запроса.</typeparam>
/// <typeparam name="TResponse">Тип ответа.</typeparam>
internal sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc/>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next(ct);
            sw.Stop();
            var isSuccess = response is not Result r || r.IsSuccess;
            logger.LogInformation("{Request} handled in {Elapsed}ms IsSuccess={Success}",
                name, sw.ElapsedMilliseconds, isSuccess);
            return response;
        }
        catch (Exception)
        {
            // Исключение записывает один раз глобальный обработчик (с методом и путём) — здесь только Debug,
            // иначе одна ошибка попадает в лог несколько раз, а ожидаемые конфликты — ещё и уровнем Error.
            sw.Stop();
            logger.LogDebug("{Request} завершился исключением через {Elapsed}ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
