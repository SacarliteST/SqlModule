using SQLModule.Common.Results;
using SQLModule.Sandbox;

namespace SQLModule.Web.Common.Isolated;

/// <summary>
/// Реализация <see cref="ISandboxExecutor"/> без Docker: возвращает успех с пустым набором строк.
/// Предназначена для изолированного режима (флаг UseFakeSandbox) и интеграционных тестов.
/// </summary>
public sealed class FakeSandboxExecutor : ISandboxExecutor
{
    /// <summary>Переопределяет результат RunAsync для тестовых сценариев.</summary>
    public Result<QueryResultSet>? OverrideRun { get; set; }

    /// <summary>Переопределяет результат ValidateSetupAsync для тестовых сценариев.</summary>
    public Result? OverrideSetup { get; set; }

    public Task<Result<QueryResultSet>> RunAsync(
        SandboxDbmsSpec dbms, SandboxSetup setup, SandboxQuery query, CancellationToken ct) =>
        Task.FromResult(OverrideRun ?? Result<QueryResultSet>.Success(
            new QueryResultSet(true, null, [], [], 0, 0)));

    public Task<Result> ValidateSetupAsync(
        SandboxDbmsSpec dbms, SandboxSetup setup, CancellationToken ct) =>
        Task.FromResult(OverrideSetup ?? Result.Success());
}
