using System.Text;
using SQLModule.Domain.Common;
using SQLModule.Domain.Schema;

namespace SQLModule.Domain.DbmsCatalog;

/// <summary>Физический тип данных для конкретной СУБД.</summary>
public sealed class PhysicalType : AuditableEntity
{
    public Guid DbmsId { get; private set; }
    public string TypeName { get; private set; }

    public DbmsDictionary Dbms { get; private set; } = default!;

    private readonly List<ParameterDefinition> parameterDefinitions = [];
    public IReadOnlyCollection<ParameterDefinition> ParameterDefinitions => parameterDefinitions.AsReadOnly();

    private PhysicalType(Guid id, Guid dbmsId, string typeName) : base(id)
    {
        DbmsId = dbmsId;
        TypeName = typeName;
    }

    public static PhysicalType Create(Guid dbmsId, string typeName, Guid? id = null)
        => new(id ?? Guid.NewGuid(), dbmsId, typeName);

    public void Update(string typeName) => TypeName = typeName;

    /// <summary>Строит SQL-описание типа с учётом переданных значений параметров.</summary>
    public string ResolveSql(IReadOnlyList<AttributeParameterValue> values)
    {
        var valuesDict = values.ToDictionary(v => v.ParameterDefinitionId, v => v.ParameterValue);
        var sb = new StringBuilder(TypeName);

        foreach (var def in ParameterDefinitions.OrderBy(p => p.SortOrder))
        {
            valuesDict.TryGetValue(def.Id, out var userValue);
            var finalValue = userValue ?? def.DefaultValue;

            if (def.IsRequired && string.IsNullOrWhiteSpace(finalValue))
            {
                throw new InvalidOperationException($"Параметр '{def.DisplayName}' обязателен для типа '{TypeName}'.");
            }

            if (!string.IsNullOrWhiteSpace(finalValue))
            {
                var sql = def.SqlFragment.Replace("{value}", $"{def.ValuePrefix}{finalValue}{def.ValueSuffix}");
                sb.Append(sql);
            }
        }

        return sb.ToString();
    }
}
