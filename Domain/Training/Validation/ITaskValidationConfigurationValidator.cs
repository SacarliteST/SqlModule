namespace SQLModule.Domain.Training.Validation;

/// <summary>Проверяет все DBMS-зависимые и общие инварианты validation-конфигурации.</summary>
public interface ITaskValidationConfigurationValidator
{
    TaskValidationRulesResult Validate(
        TaskValidationDefinition definition,
        ValidationRuleCapabilities capabilities,
        ValidationSchemaContext schema);
}
