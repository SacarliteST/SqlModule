using SQLModule.Domain.Common;

namespace SQLModule.Domain.Training;

/// <summary>Редактируемый критерий validation-конфигурации.</summary>
public sealed class ValidationCheck : BaseEntity
{
    public Guid ConfigurationId { get; private set; }
    public ValidationCheckKind Kind { get; private set; }
    public string? Value { get; private set; }
    public string UniquenessValue { get; private set; }
    public int Weight { get; private set; }
    public int Order { get; private set; }

    public TaskValidationConfiguration Configuration { get; private set; } = null!;

    private ValidationCheck(
        Guid id,
        Guid configurationId,
        ValidationCheckKind kind,
        string? value,
        int weight,
        int order) : base(id)
    {
        ConfigurationId = configurationId;
        Kind = kind;
        Value = NormalizeValue(value);
        UniquenessValue = (Value ?? String.Empty).ToUpperInvariant();
        Weight = weight;
        Order = order;
    }

    public static ValidationCheck Create(
        Guid configurationId,
        ValidationCheckKind kind,
        string? value,
        int weight,
        int order,
        Guid? id = null) =>
        new(id ?? Guid.NewGuid(), configurationId, kind, value, weight, order);

    public void Update(ValidationCheckKind kind, string? value, int weight, int order)
    {
        Kind = kind;
        Value = NormalizeValue(value);
        UniquenessValue = (Value ?? String.Empty).ToUpperInvariant();
        Weight = weight;
        Order = order;
    }

    private static string? NormalizeValue(string? value) =>
        String.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
