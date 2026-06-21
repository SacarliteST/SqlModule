namespace SQLModule.Sandbox;

/// <summary>DDL и seed-данные, применяемые перед выполнением запроса в песочнице.</summary>
public sealed record SandboxSetup(IReadOnlyList<string> Statements);
