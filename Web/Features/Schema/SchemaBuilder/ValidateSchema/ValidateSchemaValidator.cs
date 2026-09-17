using FluentValidation;
using SQLModule.Contracts.Schema.SchemaBuilder;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.ValidateSchema;

internal sealed class ValidateSchemaValidator : AbstractValidator<CreateSchemaRequest>
{
    public ValidateSchemaValidator()
    {
        RuleFor(x => x.DbmsId).NotEmpty();
        RuleFor(x => x.SchemaName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Tables).NotEmpty();

        RuleForEach(x => x.Tables).ChildRules(table =>
        {
            table.RuleFor(t => t.TempId).NotEmpty();
            table.RuleFor(t => t.Name).NotEmpty();
            table.RuleForEach(t => t.Columns).ChildRules(col =>
            {
                col.RuleFor(c => c.TempId).NotEmpty();
                col.RuleFor(c => c.Name).NotEmpty();
                col.RuleFor(c => c.PhysicalTypeId).NotEmpty();
                col.RuleForEach(c => c.Parameters).ChildRules(param =>
                {
                    param.RuleFor(p => p.ParameterDefinitionId).NotEmpty();
                });
            });
        });

        RuleFor(x => x).Custom((req, ctx) =>
        {
            var tableDupes = req.Tables
                .GroupBy(t => t.TempId, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            foreach (var d in tableDupes)
            {
                ctx.AddFailure("Tables", $"Дублирующийся TempId таблицы: '{d}'.");
            }

            var allColIds = req.Tables.SelectMany(t => t.Columns).Select(c => c.TempId).ToList();
            var colDupes = allColIds
                .GroupBy(id => id, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            foreach (var d in colDupes)
            {
                ctx.AddFailure("Tables", $"Дублирующийся TempId колонки: '{d}'.");
            }

            var validKeys = new HashSet<string>(allColIds, StringComparer.Ordinal);
            foreach (var duplicateSource in req.Relationships
                         .GroupBy(relationship => relationship.SourceColumnTempId, StringComparer.Ordinal)
                         .Where(group => group.Count() > 1))
            {
                ctx.AddFailure(
                    "Relationships",
                    $"Исходная колонка '{duplicateSource.Key}' может иметь только одну внешнюю связь.");
            }

            foreach (var rel in req.Relationships)
            {
                if (!validKeys.Contains(rel.SourceColumnTempId))
                {
                    ctx.AddFailure("Relationships", $"SourceColumnTempId '{rel.SourceColumnTempId}' не найден среди колонок.");
                }

                if (!validKeys.Contains(rel.TargetColumnTempId))
                {
                    ctx.AddFailure("Relationships", $"TargetColumnTempId '{rel.TargetColumnTempId}' не найден среди колонок.");
                }

                if (!String.IsNullOrEmpty(rel.SourceColumnTempId) && rel.SourceColumnTempId == rel.TargetColumnTempId)
                {
                    ctx.AddFailure("Relationships", $"SourceColumnTempId и TargetColumnTempId совпадают: '{rel.SourceColumnTempId}'.");
                }
            }
        });
    }
}
