using SQLModule.Common.Results;

namespace SQLModule.Sandbox;

/// <summary>
/// Поднимает одноразовый контейнер из <see cref="SandboxDbmsSpec"/>,
/// применяет setup-стейтменты и выполняет запрос.
/// </summary>
public interface ISandboxExecutor
{
    /// <summary>
    /// Запускает песочницу: поднимает контейнер, применяет <paramref name="setup"/>,
    /// выполняет <paramref name="query"/> и возвращает набор строк.
    /// Инфраструктурные сбои → <see cref="Result{T}.Fail"/>.
    /// SQL-ошибка в запросе → <see cref="QueryResultSet.Succeeded"/>=false внутри <see cref="Result{T}.Success"/>.
    /// </summary>
    Task<Result<QueryResultSet>> RunAsync(
        SandboxDbmsSpec dbms,
        SandboxSetup setup,
        SandboxQuery query,
        CancellationToken ct);

    /// <summary>
    /// Проверяет корректность setup-стейтментов: поднимает контейнер, применяет <paramref name="setup"/>.
    /// Возвращает <see cref="Result.Fail"/> с <c>Sandbox.SetupFailed</c> при ошибке DDL/seed.
    /// </summary>
    Task<Result> ValidateSetupAsync(
        SandboxDbmsSpec dbms,
        SandboxSetup setup,
        CancellationToken ct);

    /// <summary>Применяет DDL в одноразовой песочнице и читает созданную схему из системного каталога.</summary>
    Task<Result<InspectedSchema>> InspectDdlAsync(
        SandboxDbmsSpec dbms,
        string ddlScript,
        CancellationToken ct);
}
