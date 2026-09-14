using SQLModule.Common.Results;
using SQLModule.Sandbox;

namespace SQLModule.Web.Common.Isolated;

/// <summary>
/// Реализация <see cref="ISandboxExecutor"/> без Docker: возвращает успех с пустым набором строк.
/// Предназначена для изолированного режима (флаг UseFakeSandbox) и интеграционных тестов.
/// </summary>
public sealed class FakeSandboxExecutor : ISandboxExecutor
{
    private int runCallCount;
    private int validateSetupCallCount;
    public int RunCallCount => runCallCount;
    public int ValidateSetupCallCount => validateSetupCallCount;
    public SandboxQuery? LastQuery { get; private set; }
    public SandboxSetup? LastSetup { get; private set; }

    public void ResetRunCallCount()
    {
        Interlocked.Exchange(ref runCallCount, 0);
        LastQuery = null;
    }

    public void ResetValidateSetup()
    {
        Interlocked.Exchange(ref validateSetupCallCount, 0);
        LastSetup = null;
        OverrideSetup = null;
    }

    public Result<InspectedSchema>? OverrideInspection { get; set; }

    /// <summary>Переопределяет результат RunAsync для тестовых сценариев.</summary>
    public Result<QueryResultSet>? OverrideRun { get; set; }

    /// <summary>Переопределяет результат ValidateSetupAsync для тестовых сценариев.</summary>
    public Result? OverrideSetup { get; set; }

    public Task<Result<QueryResultSet>> RunAsync(
        SandboxDbmsSpec dbms, SandboxSetup setup, SandboxQuery query, CancellationToken ct)
    {
        Interlocked.Increment(ref runCallCount);
        LastQuery = query;
        return Task.FromResult(OverrideRun ?? Result<QueryResultSet>.Success(
            new QueryResultSet(true, null, [], [], 0, 0)));
    }

    public Task<Result> ValidateSetupAsync(
        SandboxDbmsSpec dbms, SandboxSetup setup, CancellationToken ct)
    {
        Interlocked.Increment(ref validateSetupCallCount);
        LastSetup = setup;
        return Task.FromResult(OverrideSetup ?? Result.Success());
    }

    public Task<Result<InspectedSchema>> InspectDdlAsync(
        SandboxDbmsSpec dbms, string ddlScript, CancellationToken ct) =>
        Task.FromResult(OverrideInspection ?? Result<InspectedSchema>.Success(new InspectedSchema([], [])));
}
