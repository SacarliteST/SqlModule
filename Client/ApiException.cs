namespace SQLModule.Client;

/// <summary>Базовое исключение при получении ошибочного HTTP-ответа от API.</summary>
public class ApiException : Exception
{
    /// <summary>HTTP-статус ответа.</summary>
    public int StatusCode { get; }

    /// <summary>Разобранное тело ошибки или <see langword="null"/>, если тело не распарсилось.</summary>
    public ApiProblem? Problem { get; }

    /// <param name="statusCode">HTTP-статус.</param>
    /// <param name="problem">Описание ошибки.</param>
    public ApiException(int statusCode, ApiProblem? problem)
        : base(problem?.Detail ?? $"HTTP {statusCode}")
    {
        StatusCode = statusCode;
        Problem = problem;
    }
}
