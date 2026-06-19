namespace SQLModule.Host.Common.Results;

/// <summary>
/// Методы-расширения для конвертации <see cref="Result"/> и <see cref="Result{T}"/> в <see cref="IResult"/>.
/// Используются в эндпоинтах вместо прямого обращения к <see cref="TypedResults"/>.
/// </summary>
public static class ResultExtensions
{
    /// <summary>Преобразует результат без значения в 200 OK или в ProblemDetails при ошибке.</summary>
    /// <param name="result">Результат операции.</param>
    public static IResult ToOk(this Result result) =>
        result.IsSuccess ? TypedResults.Ok() : ToProblem(result.Error!);

    /// <inheritdoc cref="ToOk(Result)"/>
    /// <typeparam name="T">Тип значения.</typeparam>
    public static IResult ToOk<T>(this Result<T> result) =>
        result.IsSuccess ? TypedResults.Ok(result.Value!) : ToProblem(result.Error!);

    /// <summary>
    /// Преобразует результат в 201 Created с заголовком Location или в ProblemDetails при ошибке.
    /// </summary>
    /// <typeparam name="T">Тип созданного значения.</typeparam>
    /// <param name="result">Результат операции.</param>
    /// <param name="locationFn">Функция, формирующая URI созданного ресурса из значения.</param>
    public static IResult ToCreated<T>(this Result<T> result, Func<T, string> locationFn) =>
        result.IsSuccess
            ? TypedResults.Created(locationFn(result.Value!), result.Value!)
            : ToProblem(result.Error!);

    /// <summary>
    /// Преобразует результат в 204 No Content или в ProblemDetails при ошибке.
    /// </summary>
    /// <param name="result">Результат операции.</param>
    public static IResult ToNoContent(this Result result) =>
        result.IsSuccess ? TypedResults.NoContent() : ToProblem(result.Error!);

    private static IResult ToProblem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
        return TypedResults.Problem(detail: error.Message, statusCode: statusCode, title: error.Code);
    }
}
