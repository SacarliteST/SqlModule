using SQLModule.Common.Results;

namespace SQLModule.Application.Schema.SchemaBuilder.GetLookupValues;

public static class LookupValuesErrors
{
    public static Error ColumnNotFound(Guid columnId) => Error.Validation(
        "LookupColumnNotFound",
        $"Колонка lookup со значением id '{columnId}' не найдена.");

    public static Error ColumnDoesNotBelongToTable(Guid columnId, Guid tableId) => Error.Validation(
        "LookupColumnDoesNotBelongToTable",
        $"Колонка lookup '{columnId}' не принадлежит таблице '{tableId}'.");
}
