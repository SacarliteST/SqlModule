namespace SQLModule.Sandbox;

/// <summary>Параметры SQL-запроса, выполняемого в песочнице.</summary>
public sealed record SandboxQuery(string Sql, int TimeoutSeconds, int MaxRows);
