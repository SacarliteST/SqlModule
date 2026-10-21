namespace SQLModule.Sandbox.DbmsProbing;

/// <summary>Проверяет, что Docker-конфигурация СУБД действительно запускается и принимает подключения.</summary>
public interface IDbmsProbe
{
    Task<DbmsProbeResult> ProbeAsync(DbmsProbeSpec spec, CancellationToken ct);
}

/// <summary>Результат пробы СУБД; сообщение заполнено только при неудаче.</summary>
public sealed record DbmsProbeResult(bool IsSuccess, string? ErrorMessage = null)
{
    public static DbmsProbeResult Success() => new(true);

    public static DbmsProbeResult Failure(string message) => new(false, message);
}
