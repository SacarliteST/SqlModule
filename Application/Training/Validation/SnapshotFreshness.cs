using System.Text.Json.Nodes;

namespace SQLModule.Application.Training.Validation;

/// <summary>
/// Сравнение снимка опубликованной версии оценки с живым состоянием схемы, данных и эталона.
/// Порядок элементов при повторной выборке из БД не гарантирован — сравниваются канонические формы.
/// </summary>
public static class SnapshotFreshness
{
    public static async Task<bool> IsFreshAsync(
        ITaskValidationSnapshotFactory snapshotFactory,
        Guid taskId,
        string schemaSnapshotJson,
        string datasetSnapshotJson,
        string referenceQuerySnapshotJson,
        CancellationToken ct)
    {
        var live = await snapshotFactory.CreateLiveAsync(taskId, ct);
        // Если живое состояние не построить (нет базы) — устаревание не диагностируем: это отдельная причина.
        return !live.IsSuccess ||
               Same(schemaSnapshotJson, live.Value!.SchemaJson) &&
               Same(datasetSnapshotJson, live.Value.DatasetJson) &&
               Same(referenceQuerySnapshotJson, live.Value.ReferenceQueryJson);
    }

    public static bool Same(string stored, string live) =>
        String.Equals(Canonical(JsonNode.Parse(stored)), Canonical(JsonNode.Parse(live)), StringComparison.Ordinal);

    private static string Canonical(JsonNode? node) => node switch
    {
        JsonObject value => "{" + String.Join(
            ",",
            value.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"\"{pair.Key}\":{Canonical(pair.Value)}")) + "}",
        JsonArray value => "[" + String.Join(
            ",",
            value.Select(Canonical).OrderBy(item => item, StringComparer.Ordinal)) + "]",
        null => "null",
        _ => node.ToJsonString()
    };
}
