namespace SQLModule.Domain.Training.Validation;

/// <summary>Единая реализация правил сохранения, preview и публикации validation-конфигурации.</summary>
public sealed class TaskValidationConfigurationValidator : ITaskValidationConfigurationValidator
{
    public TaskValidationRulesResult Validate(
        TaskValidationDefinition definition,
        ValidationRuleCapabilities capabilities,
        ValidationSchemaContext schema)
    {
        var violations = new List<ValidationRuleViolation>();

        ValidateScoreAndLimit(definition, capabilities, violations);
        ValidateHints(definition.VisibleHintGroups, capabilities, violations);
        ValidateChecks(definition, capabilities, schema, violations);

        return new TaskValidationRulesResult(violations);
    }

    private static void ValidateScoreAndLimit(
        TaskValidationDefinition definition,
        ValidationRuleCapabilities capabilities,
        ICollection<ValidationRuleViolation> violations)
    {
        if (definition.PassingScore is < 1 or > 100)
        {
            Add(
                violations,
                "passingScore",
                "Validation.PassingScoreOutOfRange",
                "Проходной балл должен находиться в диапазоне от 1 до 100.");
        }

        if (definition.MaxAttempts is <= 0 ||
            definition.MaxAttempts > capabilities.MaxAttemptsLimit)
        {
            Add(
                violations,
                "maxAttempts",
                "Validation.MaxAttemptsOutOfRange",
                $"Лимит попыток должен быть null или находиться в диапазоне от 1 до {capabilities.MaxAttemptsLimit}.");
        }
    }

    private static void ValidateHints(
        IReadOnlyList<HintGroup> hints,
        ValidationRuleCapabilities capabilities,
        ICollection<ValidationRuleViolation> violations)
    {
        var seen = new HashSet<HintGroup>();
        for (var index = 0; index < hints.Count; index++)
        {
            var hint = hints[index];
            var path = $"visibleHintGroups[{index}]";
            if (!Enum.IsDefined(hint))
            {
                Add(violations, path, "Validation.UnknownHintGroup", "Указана неизвестная группа подсказок.");
                continue;
            }

            if (!seen.Add(hint))
            {
                Add(violations, path, "Validation.DuplicateHintGroup", "Группа подсказок указана повторно.");
            }

            if (!capabilities.SupportedHintGroups.Contains(hint))
            {
                Add(
                    violations,
                    path,
                    "Validation.UnsupportedHintGroup",
                    "Группа подсказок не поддерживается выбранной СУБД.");
            }
        }
    }

    private static void ValidateChecks(
        TaskValidationDefinition definition,
        ValidationRuleCapabilities capabilities,
        ValidationSchemaContext schema,
        ICollection<ValidationRuleViolation> violations)
    {
        var checks = definition.Checks;
        if (checks.Count == 0)
        {
            Add(violations, "checks", "Validation.ChecksRequired", "Добавьте хотя бы один критерий проверки.");
            return;
        }

        var mainIndexes = checks
            .Select((check, index) => (check, index))
            .Where(item => item.check.Kind == ValidationCheckKind.MainDatasetResult)
            .Select(item => item.index)
            .ToArray();

        if (mainIndexes.Length == 0)
        {
            Add(
                violations,
                "checks",
                "Validation.MainDatasetResultRequired",
                "Конфигурация должна содержать критерий MainDatasetResult.");
        }
        else if (mainIndexes.Length > 1)
        {
            foreach (var index in mainIndexes.Skip(1))
            {
                Add(
                    violations,
                    $"checks[{index}].kind",
                    "Validation.MainDatasetResultDuplicate",
                    "Критерий MainDatasetResult может присутствовать только один раз.");
            }
        }

        var seenIds = new HashSet<Guid>();
        var seenCriteria = new Dictionary<(ValidationCheckKind Kind, string Value), int>();
        var seenOrders = new Dictionary<int, int>();
        var requiredConstructs = new Dictionary<SqlConstruct, int>();
        var forbiddenConstructs = new Dictionary<SqlConstruct, int>();
        var requiredTables = new Dictionary<Guid, int>();
        var forbiddenTables = new Dictionary<Guid, int>();

        for (var index = 0; index < checks.Count; index++)
        {
            var check = checks[index];
            var path = $"checks[{index}]";
            var normalizedValue = Normalize(check.Value);

            if (check.Id.HasValue && !seenIds.Add(check.Id.Value))
            {
                Add(
                    violations,
                    $"{path}.id",
                    "Validation.DuplicateCheckId",
                    "Идентификатор критерия указан повторно.");
            }

            var isKnownKind = Enum.IsDefined(check.Kind);
            if (!isKnownKind)
            {
                Add(violations, $"{path}.kind", "Validation.UnknownCheckKind", "Указан неизвестный вид критерия.");
            }
            else if (!capabilities.SupportedCheckKinds.Contains(check.Kind))
            {
                Add(
                    violations,
                    $"{path}.kind",
                    "Validation.UnsupportedCheckKind",
                    "Критерий не поддерживается выбранной СУБД.");
            }

            if (check.Weight is < 1 or > 100)
            {
                Add(
                    violations,
                    $"{path}.weight",
                    "Validation.WeightOutOfRange",
                    "Вес критерия должен находиться в диапазоне от 1 до 100.");
            }

            if (check.Order < 0)
            {
                Add(
                    violations,
                    $"{path}.order",
                    "Validation.OrderOutOfRange",
                    "Порядок критерия не может быть отрицательным.");
            }
            else if (seenOrders.TryGetValue(check.Order, out _))
            {
                Add(
                    violations,
                    $"{path}.order",
                    "Validation.DuplicateOrder",
                    "Порядок критерия должен быть уникальным.");
            }
            else
            {
                seenOrders.Add(check.Order, index);
            }

            var uniquenessKey = (check.Kind, (normalizedValue ?? String.Empty).ToUpperInvariant());
            if (seenCriteria.TryGetValue(uniquenessKey, out _))
            {
                Add(
                    violations,
                    path,
                    "Validation.DuplicateCheck",
                    "Такой критерий уже присутствует в конфигурации.");
            }
            else
            {
                seenCriteria.Add(uniquenessKey, index);
            }

            if (isKnownKind)
            {
                switch (check.Kind)
                {
                    case ValidationCheckKind.MainDatasetResult:
                        ValidateMainResult(normalizedValue, path, violations);
                        break;
                    case ValidationCheckKind.RequiredConstruct:
                        ValidateConstruct(
                            normalizedValue, path, capabilities, requiredConstructs,
                            forbiddenConstructs, violations);
                        break;
                    case ValidationCheckKind.ForbiddenConstruct:
                        ValidateConstruct(
                            normalizedValue, path, capabilities, forbiddenConstructs,
                            requiredConstructs, violations);
                        break;
                    case ValidationCheckKind.RequiredTable:
                        ValidateTable(
                            normalizedValue, path, schema, requiredTables,
                            forbiddenTables, violations);
                        break;
                    case ValidationCheckKind.ForbiddenTable:
                        ValidateTable(
                            normalizedValue, path, schema, forbiddenTables,
                            requiredTables, violations);
                        break;
                }
            }
        }

        if (checks.Sum(check => (long)check.Weight) != 100)
        {
            Add(
                violations,
                "checks",
                "Validation.WeightsMustSumTo100",
                "Сумма весов всех критериев должна быть равна 100.");
        }

        if (mainIndexes.Length == 1)
        {
            var mainWeight = checks[mainIndexes[0]].Weight;
            if (mainWeight is >= 1 and <= 100 &&
                definition.PassingScore is >= 1 and <= 100 &&
                definition.PassingScore <= 100 - mainWeight)
            {
                Add(
                    violations,
                    "passingScore",
                    "Validation.MainDatasetResultRequiredForPassing",
                    "Проходной балл должен быть больше максимального балла без MainDatasetResult.");
            }
        }
    }

    private static void ValidateMainResult(
        string? value,
        string path,
        ICollection<ValidationRuleViolation> violations)
    {
        if (value is not null)
        {
            Add(
                violations,
                $"{path}.value",
                "Validation.MainDatasetResultValueMustBeEmpty",
                "Критерий MainDatasetResult не принимает значение.");
        }
    }

    private static void ValidateConstruct(
        string? value,
        string path,
        ValidationRuleCapabilities capabilities,
        IDictionary<SqlConstruct, int> current,
        IReadOnlyDictionary<SqlConstruct, int> opposite,
        ICollection<ValidationRuleViolation> violations)
    {
        if (!TryParseConstruct(value, out var construct))
        {
            Add(
                violations,
                $"{path}.value",
                "Validation.InvalidConstruct",
                "Укажите поддерживаемую SQL-конструкцию.");
            return;
        }

        if (!capabilities.SupportedConstructs.Contains(construct))
        {
            Add(
                violations,
                $"{path}.value",
                "Validation.UnsupportedConstruct",
                "SQL-конструкция не поддерживается анализатором выбранной СУБД.");
        }

        if (opposite.ContainsKey(construct))
        {
            Add(
                violations,
                $"{path}.value",
                "Validation.ContradictingConstruct",
                "SQL-конструкция не может быть одновременно обязательной и запрещённой.");
        }

        current.TryAdd(construct, 0);
    }

    private static void ValidateTable(
        string? value,
        string path,
        ValidationSchemaContext schema,
        IDictionary<Guid, int> current,
        IReadOnlyDictionary<Guid, int> opposite,
        ICollection<ValidationRuleViolation> violations)
    {
        if (!Guid.TryParseExact(value, "D", out var tableId))
        {
            Add(
                violations,
                $"{path}.value",
                "Validation.InvalidTableId",
                "Укажите идентификатор таблицы в формате UUID.");
            return;
        }

        if (!schema.TableIds.Contains(tableId))
        {
            Add(
                violations,
                $"{path}.value",
                "Validation.TableNotFound",
                "Таблица отсутствует в схеме задания.");
        }

        if (opposite.ContainsKey(tableId))
        {
            Add(
                violations,
                $"{path}.value",
                "Validation.ContradictingTable",
                "Таблица не может быть одновременно обязательной и запрещённой.");
        }

        current.TryAdd(tableId, 0);
    }

    private static bool TryParseConstruct(string? value, out SqlConstruct construct) =>
        Enum.TryParse(value, false, out construct) &&
        Enum.IsDefined(construct) &&
        String.Equals(value, construct.ToString(), StringComparison.Ordinal);

    private static string? Normalize(string? value) =>
        String.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Add(
        ICollection<ValidationRuleViolation> violations,
        string path,
        string code,
        string message) =>
        violations.Add(new ValidationRuleViolation(
            path,
            code,
            ValidationViolationSeverity.Error,
            message));
}
