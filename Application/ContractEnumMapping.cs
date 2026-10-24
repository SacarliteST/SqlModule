using ContractTraining = SQLModule.Contracts.Training;
using DomainTraining = SQLModule.Domain.Training;
using DomainModuleIntegration = SQLModule.Domain.ModuleIntegration;
using DomainValidation = SQLModule.Domain.Training.Validation;

namespace SQLModule.Application;

/// <summary>
/// Преобразование между доменными перечислениями и одноимёнными перечислениями публичного контракта.
/// Имена и числовые значения совпадают по определению; соответствие проверяет тест паритета
/// (UnitTests/Training/ContractEnumParityTests), поэтому приведение по числовому значению безопасно.
/// </summary>
public static class ContractEnumMapping
{
    public static ContractTraining.ExecutionStatus ToContract(this DomainTraining.ExecutionStatus value) => Map<ContractTraining.ExecutionStatus>(value);
    public static DomainTraining.ExecutionStatus ToDomain(this ContractTraining.ExecutionStatus value) => Map<DomainTraining.ExecutionStatus>(value);

    public static ContractTraining.ValidationCheckKind ToContract(this DomainTraining.ValidationCheckKind value) => Map<ContractTraining.ValidationCheckKind>(value);
    public static DomainTraining.ValidationCheckKind ToDomain(this ContractTraining.ValidationCheckKind value) => Map<DomainTraining.ValidationCheckKind>(value);

    public static ContractTraining.HintGroup ToContract(this DomainTraining.HintGroup value) => Map<ContractTraining.HintGroup>(value);
    public static DomainTraining.HintGroup ToDomain(this ContractTraining.HintGroup value) => Map<DomainTraining.HintGroup>(value);

    public static ContractTraining.CheckReason ToContract(this DomainTraining.CheckReason value) => Map<ContractTraining.CheckReason>(value);
    public static DomainTraining.CheckReason ToDomain(this ContractTraining.CheckReason value) => Map<DomainTraining.CheckReason>(value);

    public static ContractTraining.AttemptResultSnapshotState ToContract(this DomainTraining.AttemptResultSnapshotState value) =>
        Map<ContractTraining.AttemptResultSnapshotState>(value);

    public static DomainTraining.AttemptResultSnapshotState ToDomain(this ContractTraining.AttemptResultSnapshotState value) =>
        Map<DomainTraining.AttemptResultSnapshotState>(value);

    public static ContractTraining.SqlConstruct ToContract(this DomainTraining.SqlConstruct value) => Map<ContractTraining.SqlConstruct>(value);
    public static DomainTraining.SqlConstruct ToDomain(this ContractTraining.SqlConstruct value) => Map<DomainTraining.SqlConstruct>(value);

    public static ContractTraining.ValidationCheckStatus ToContract(this DomainTraining.ValidationCheckStatus value) =>
        Map<ContractTraining.ValidationCheckStatus>(value);

    public static DomainTraining.ValidationCheckStatus ToDomain(this ContractTraining.ValidationCheckStatus value) =>
        Map<DomainTraining.ValidationCheckStatus>(value);

    public static ContractTraining.SessionStatus ToContract(this DomainModuleIntegration.ModuleSessionStatus value) =>
        Map<ContractTraining.SessionStatus>(value);
    public static ContractTraining.FinalizationReason ToContract(this DomainTraining.FinalizationReason value) =>
        Map<ContractTraining.FinalizationReason>(value);

    public static DomainTraining.FinalizationReason ToDomain(this ContractTraining.FinalizationReason value) =>
        Map<DomainTraining.FinalizationReason>(value);

    public static ContractTraining.ValidationConfigurationState ToContract(this DomainTraining.ValidationConfigurationState value) =>
        Map<ContractTraining.ValidationConfigurationState>(value);

    public static DomainTraining.ValidationConfigurationState ToDomain(this ContractTraining.ValidationConfigurationState value) =>
        Map<DomainTraining.ValidationConfigurationState>(value);

    public static ContractTraining.PublicationStatus ToContract(this DomainTraining.PublicationStatus value) =>
        Map<ContractTraining.PublicationStatus>(value);

    public static DomainTraining.PublicationStatus ToDomain(this ContractTraining.PublicationStatus value) =>
        Map<DomainTraining.PublicationStatus>(value);

    public static ContractTraining.ValidationViolationSeverity ToContract(this DomainValidation.ValidationViolationSeverity value) =>
        Map<ContractTraining.ValidationViolationSeverity>(value);

    public static DomainValidation.ValidationViolationSeverity ToDomain(this ContractTraining.ValidationViolationSeverity value) =>
        Map<DomainValidation.ValidationViolationSeverity>(value);

    private static TTo Map<TTo>(Enum value)
        where TTo : struct, Enum =>
        (TTo)Enum.ToObject(typeof(TTo), Convert.ToInt64(value));
}
