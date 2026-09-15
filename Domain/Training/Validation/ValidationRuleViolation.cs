namespace SQLModule.Domain.Training.Validation;

/// <summary>Смысловая ошибка validation-конфигурации.</summary>
public sealed record ValidationRuleViolation(
    string Path,
    string Code,
    ValidationViolationSeverity Severity,
    string Message);

/// <summary>Серьёзность нарушения validation-конфигурации.</summary>
public enum ValidationViolationSeverity
{
    Error,
    Warning
}

/// <summary>Полный результат проверки конфигурации.</summary>
public sealed record TaskValidationRulesResult(IReadOnlyList<ValidationRuleViolation> Violations)
{
    public bool IsValid => Violations.Count == 0;
}
