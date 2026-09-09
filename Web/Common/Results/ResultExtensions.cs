using Microsoft.AspNetCore.Http;

namespace SQLModule.Common.Results;

/// <summary>
/// Методы-расширения для конвертации <see cref="Result"/> и <see cref="Result{T}"/> в <see cref="IResult"/>.
/// Используются в эндпоинтах вместо прямого обращения к <see cref="TypedResults"/>.
/// </summary>
public static class ResultExtensions
{
    /// <summary>Преобразует результат без значения в 200 OK или в ProblemDetails при ошибке.</summary>
    public static IResult ToOk(this Result result) =>
        result.IsSuccess ? TypedResults.Ok() : ToProblem(result.Error!);

    /// <inheritdoc cref="ToOk(Result)"/>
    /// <typeparam name="T">Тип значения.</typeparam>
    public static IResult ToOk<T>(this Result<T> result) =>
        result.IsSuccess ? TypedResults.Ok(result.Value!) : ToProblem(result.Error!);

    /// <summary>
    /// Преобразует результат в 201 Created с заголовком Location или в ProblemDetails при ошибке.
    /// </summary>
    public static IResult ToCreated<T>(this Result<T> result, Func<T, string> locationFn) =>
        result.IsSuccess
            ? TypedResults.Created(locationFn(result.Value!), result.Value!)
            : ToProblem(result.Error!);

    /// <summary>Преобразует результат в 204 No Content или в ProblemDetails при ошибке.</summary>
    public static IResult ToNoContent(this Result result) =>
        result.IsSuccess ? TypedResults.NoContent() : ToProblem(result.Error!);

    private static IResult ToProblem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.PreconditionFailed => StatusCodes.Status412PreconditionFailed,
            ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError
        };
        var title = error.Type switch
        {
            ErrorType.Validation => "Ошибка бизнес-валидации",
            ErrorType.NotFound => "Ресурс не найден",
            ErrorType.Forbidden => "Доступ запрещён",
            ErrorType.Conflict => "Конфликт состояния",
            ErrorType.PreconditionFailed => "Условие обновления не выполнено",
            ErrorType.Unavailable => "Сервис временно недоступен",
            _ => "Внутренняя ошибка"
        };

        return SQLModule.Web.Common.ApiProblemFactory.ToResult(
            statusCode,
            title,
            error.Message,
            error.Code,
            error.Path is null ? null : new Dictionary<string, string[]> { [error.Path] = [error.Message] },
            error.AffectedRows,
            error.Severity,
            error.Limit);
    }
}
