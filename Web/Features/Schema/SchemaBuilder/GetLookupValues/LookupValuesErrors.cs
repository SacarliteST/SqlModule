using SQLModule.Common.Results;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.GetLookupValues;

internal static class LookupValuesErrors
{
    internal static Error ColumnNotFound(Guid columnId) => Error.Validation(
        "LookupColumnNotFound",
        $"Колонка lookup со значением id '{columnId}' не найдена.");

    internal static Error ColumnDoesNotBelongToTable(Guid columnId, Guid tableId) => Error.Validation(
        "LookupColumnDoesNotBelongToTable",
        $"Колонка lookup '{columnId}' не принадлежит таблице '{tableId}'.");
}
