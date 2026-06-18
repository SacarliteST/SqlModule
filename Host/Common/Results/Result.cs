namespace SQLModule.Host.Common.Results;

/// <summary>
/// Базовый результат операции без возвращаемого значения.
/// Либо успех, либо ошибка с описанием в <see cref="Error"/>.
/// </summary>
public class Result
{
    /// <summary>Операция завершилась успешно.</summary>
    public bool IsSuccess { get; private init; }

    /// <summary>Описание ошибки. <see langword="null"/> при <see cref="IsSuccess"/> = <see langword="true"/>.</summary>
    public Error? Error { get; private init; }

    /// <param name="isSuccess">Признак успеха.</param>
    /// <param name="error">Ошибка или <see langword="null"/>.</param>
    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Создаёт успешный результат.</summary>
    public static Result Success() => new(true, null);

    /// <summary>Создаёт результат с ошибкой.</summary>
    /// <param name="error">Описание ошибки.</param>
    public static Result Fail(Error error) => new(false, error);
}

/// <summary>
/// Результат операции с возвращаемым значением <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Тип возвращаемого значения при успехе.</typeparam>
public sealed class Result<T> : Result
{
    /// <summary>Возвращаемое значение. <see langword="null"/> при ошибке.</summary>
    public T? Value { get; private init; }

    private Result(bool isSuccess, T? value, Error? error) : base(isSuccess, error)
    {
        Value = value;
    }

    /// <summary>Создаёт успешный результат со значением.</summary>
    /// <param name="value">Возвращаемое значение.</param>
    public static Result<T> Success(T value) => new(true, value, null);

    /// <summary>Создаёт результат с ошибкой. Скрывает <see cref="Result.Fail"/> базового класса.</summary>
    /// <param name="error">Описание ошибки.</param>
    public new static Result<T> Fail(Error error) => new(false, default, error);

    /// <summary>
    /// Неявное преобразование значения в успешный результат.
    /// Позволяет писать <c>return entity;</c> вместо <c>return Result&lt;T&gt;.Success(entity);</c>.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);
}
