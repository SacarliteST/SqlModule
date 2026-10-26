using System.Text.Json;
using SQLModule.Application.Schema.SchemaBuilder;
using SQLModule.Application.Training.Attempts;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Contracts.Training.Student;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.Training;
using SQLModule.Sandbox;

namespace SQLModule.Application.Training.Student;

/// <summary>Собирает безопасные подсказки из неизменяемого снимка validation-версии.</summary>
internal static class StudentHints
{
    internal static StudentTaskHintsResponse Build(TaskValidationVersion version)
    {
        var groups = version.GetVisibleHintGroups();
        try
        {
            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            var configuration = JsonSerializer.Deserialize<ValidationConfigurationSnapshot>(
                version.ValidationConfigurationSnapshotJson, jsonOptions);
            var schema = JsonSerializer.Deserialize<SchemaSpec>(version.SchemaSnapshotJson, jsonOptions);
            if (configuration is null || schema is null)
            {
                return EmptyHints(groups);
            }

            var visible = groups.ToHashSet();
            var requiredConstructs = visible.Contains(HintGroup.RequiredConstructs)
                ? ParseConstructs(configuration.Checks, ValidationCheckKind.RequiredConstruct)
                : [];
            var forbiddenConstructs = visible.Contains(HintGroup.ForbiddenConstructs)
                ? ParseConstructs(configuration.Checks, ValidationCheckKind.ForbiddenConstruct)
                : [];
            var tables = schema.Tables
                .Where(value => Guid.TryParse(value.Key, out _))
                .ToDictionary(value => value.Key, value => value.Name);
            var requiredTables = visible.Contains(HintGroup.RequiredTables)
                ? ParseTables(configuration.Checks, ValidationCheckKind.RequiredTable, tables)
                : [];
            var forbiddenTables = visible.Contains(HintGroup.ForbiddenTables)
                ? ParseTables(configuration.Checks, ValidationCheckKind.ForbiddenTable, tables)
                : [];
            return new StudentTaskHintsResponse(
                groups.Select(value => value.ToContract()).ToArray(),
                requiredConstructs,
                forbiddenConstructs,
                requiredTables,
                forbiddenTables);
        }
        catch (JsonException)
        {
            return EmptyHints(groups);
        }
    }

    private static IReadOnlyList<Contracts.Training.SqlConstruct> ParseConstructs(
        IEnumerable<ValidationCheckSnapshot> checks,
        ValidationCheckKind kind) =>
        checks.Where(value => value.Kind == kind)
            .Select(value => Enum.TryParse<SqlConstruct>(value.Value, out var construct)
                ? (SqlConstruct?)construct
                : null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value.ToContract())
            .Distinct()
            .ToArray();

    private static IReadOnlyList<StudentHintTableResponse> ParseTables(
        IEnumerable<ValidationCheckSnapshot> checks,
        ValidationCheckKind kind,
        IReadOnlyDictionary<string, string> tables) =>
        checks.Where(value => value.Kind == kind && value.Value is not null && tables.ContainsKey(value.Value))
            .Select(value => new StudentHintTableResponse(Guid.Parse(value.Value!), tables[value.Value!]))
            .DistinctBy(value => value.Id)
            .ToArray();

    private static StudentTaskHintsResponse EmptyHints(IReadOnlyList<HintGroup> groups) =>
        new(groups.Select(value => value.ToContract()).ToArray(), [], [], [], []);
}
