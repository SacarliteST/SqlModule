using System.Text.Json;
using SQLModule.Sandbox;

namespace SQLModule.Web.Common.Sandbox;

/// <summary>Золотой результат эталонного запроса: колонки и строки данных.</summary>
internal sealed record GoldenResult(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string?>> Rows)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    internal static GoldenResult FromResultSet(QueryResultSet resultSet) =>
        new(resultSet.Columns, resultSet.Rows);

    internal static string Serialize(QueryResultSet resultSet) =>
        JsonSerializer.Serialize(FromResultSet(resultSet), JsonOptions);

    internal static GoldenResult? Deserialize(string json) =>
        JsonSerializer.Deserialize<GoldenResult>(json, JsonOptions);
}
