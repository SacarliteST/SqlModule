namespace SQLModule.Sandbox.Pooling;

/// <summary>
/// Конфигурация создания воркера. Содержит секреты запуска, но никогда не выводит их в строковое
/// представление, логи или метрики.
/// </summary>
internal sealed class SandboxWorkerProfile
{
    internal SandboxWorkerProfile(SandboxDbmsSpec dbms, SandboxPoolProfileOptions limits)
    {
        Dbms = dbms;
        MinSize = limits.MinSize;
        MaxSize = limits.MaxSize;
        Key = SandboxProfileKey.Create(dbms);
    }

    internal SandboxProfileKey Key { get; }
    internal SandboxDbmsSpec Dbms { get; }
    internal int MinSize { get; }
    internal int MaxSize { get; }

    public override string ToString() => Key.ToString();
}
