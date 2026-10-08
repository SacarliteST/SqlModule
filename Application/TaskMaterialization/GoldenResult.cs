using System.Text.Json;
using SQLModule.Sandbox;

namespace SQLModule.Application.TaskMaterialization;

/// <summary>Золотой результат эталонного запроса: колонки и строки данных.</summary>
public sealed record GoldenResult(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string?>> Rows)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static GoldenResult FromResultSet(QueryResultSet resultSet) =>
        new(resultSet.Columns, resultSet.Rows);

    public static string Serialize(QueryResultSet resultSet) =>
        JsonSerializer.Serialize(FromResultSet(resultSet), JsonOptions);

    public static GoldenResult? Deserialize(string json) =>
        JsonSerializer.Deserialize<GoldenResult>(json, JsonOptions);
}
