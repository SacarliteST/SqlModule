using FluentValidation;
using SQLModule.Contracts.Schema.SchemaBuilder;

namespace SQLModule.Web.Features.Schema.SchemaBuilder;

internal sealed class SchemaUpsertValidator : AbstractValidator<SchemaUpsertRequest>
{
    private static readonly HashSet<string> ForeignKeyRules =
        new(StringComparer.OrdinalIgnoreCase) { "CASCADE", "RESTRICT", "SET NULL", "NO ACTION" };

    public SchemaUpsertValidator()
    {
        RuleFor(x => x.Version).NotEmpty();
        RuleFor(x => x.Tables).NotNull();
        RuleFor(x => x.Relationships).NotNull();
        RuleForEach(x => x.Tables!).ChildRules(table =>
        {
            table.RuleFor(x => x).Must(x => x.Id.HasValue ^ !String.IsNullOrWhiteSpace(x.TempId))
                .WithMessage("Укажите ровно одно из id или tempId.");
            table.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            table.RuleFor(x => x.SortOrder).NotNull().GreaterThanOrEqualTo((short)0);
            table.RuleFor(x => x.Columns).NotNull().NotEmpty();
            table.RuleForEach(x => x.Columns!).ChildRules(column =>
            {
                column.RuleFor(x => x).Must(x => x.Id.HasValue ^ !String.IsNullOrWhiteSpace(x.TempId))
                    .WithMessage("Укажите ровно одно из id или tempId.");
                column.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
                column.RuleFor(x => x.PhysicalTypeId).NotNull().NotEmpty();
                column.RuleFor(x => x.IsPrimaryKey).NotNull();
                column.RuleFor(x => x.IsRequired).NotNull();
                column.RuleFor(x => x.SortOrder).NotNull().GreaterThanOrEqualTo((short)0);
                column.RuleFor(x => x.Parameters).NotNull();
                column.RuleForEach(x => x.Parameters!).ChildRules(parameter =>
                {
                    parameter.RuleFor(x => x.ParameterDefinitionId).NotNull().NotEmpty();
                    parameter.RuleFor(x => x.Value).NotNull();
                });
            });
        });
        RuleForEach(x => x.Relationships!).ChildRules(relationship =>
        {
            relationship.RuleFor(x => x).Must(x => x.Id.HasValue ^ !String.IsNullOrWhiteSpace(x.TempId))
                .WithMessage("Укажите ровно одно из id или tempId.");
            relationship.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            relationship.RuleFor(x => x.SourceColumnRef).NotEmpty();
            relationship.RuleFor(x => x.TargetColumnRef).NotEmpty();
            relationship.RuleFor(x => x.DeleteRule)
                .Must(IsValidRule).WithMessage("Недопустимое правило удаления FK.");
            relationship.RuleFor(x => x.UpdateRule)
                .Must(IsValidRule).WithMessage("Недопустимое правило обновления FK.");
        });
        RuleFor(x => x).Custom(ValidateAggregate);
    }

    private static bool IsValidRule(string? rule) =>
        String.IsNullOrWhiteSpace(rule) || ForeignKeyRules.Contains(rule);

    private static void ValidateAggregate(SchemaUpsertRequest request, ValidationContext<SchemaUpsertRequest> context)
    {
        if (request.Tables is null || request.Relationships is null)
        {
            return;
        }

        foreach (var duplicate in request.Tables.Where(x => !String.IsNullOrWhiteSpace(x.Name))
                     .GroupBy(x => x.Name!, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1))
        {
            context.AddFailure($"Tables[{duplicate.Key}]", "Имена таблиц должны быть уникальны.");
        }

        var columnRefs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var table in request.Tables)
        {
            if (table.Columns is null)
            {
                continue;
            }

            foreach (var duplicate in table.Columns.Where(x => !String.IsNullOrWhiteSpace(x.Name))
                         .GroupBy(x => x.Name!, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1))
            {
                context.AddFailure($"Tables[{table.TempId ?? table.Id?.ToString()}].Columns[{duplicate.Key}]",
                    "Имена колонок внутри таблицы должны быть уникальны.");
            }

            foreach (var column in table.Columns)
            {
                columnRefs.Add(column.Id?.ToString() ?? column.TempId ?? String.Empty);
            }
        }

        foreach (var relationship in request.Relationships)
        {
            var key = relationship.TempId ?? relationship.Id?.ToString() ?? "unknown";
            if (!columnRefs.Contains(relationship.SourceColumnRef ?? String.Empty) ||
                !columnRefs.Contains(relationship.TargetColumnRef ?? String.Empty))
            {
                context.AddFailure($"Relationships[{key}]", "Связь ссылается на неизвестную колонку.");
            }
        }
    }
}
