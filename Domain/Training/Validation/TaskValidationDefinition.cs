namespace SQLModule.Domain.Training.Validation;

/// <summary>Нормализованный draft конфигурации для проверки бизнес-инвариантов.</summary>
public sealed record TaskValidationDefinition(
    int PassingScore,
    int? MaxAttempts,
    IReadOnlyList<HintGroup> VisibleHintGroups,
    IReadOnlyList<ValidationCheckDefinition> Checks);

/// <summary>Нормализованный критерий draft-конфигурации.</summary>
public sealed record ValidationCheckDefinition(
    Guid? Id,
    ValidationCheckKind Kind,
    string? Value,
    int Weight,
    int Order);

/// <summary>Реально поддерживаемые конкретным DBMS analyzer возможности.</summary>
public sealed record ValidationRuleCapabilities(
    IReadOnlySet<ValidationCheckKind> SupportedCheckKinds,
    IReadOnlySet<SqlConstruct> SupportedConstructs,
    IReadOnlySet<HintGroup> SupportedHintGroups,
    int MaxAttemptsLimit);

/// <summary>Минимальный безопасный контекст схемы, нужный validation rules.</summary>
public sealed record ValidationSchemaContext(IReadOnlySet<Guid> TableIds);
