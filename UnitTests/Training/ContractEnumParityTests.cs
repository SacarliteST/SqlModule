using Shouldly;
using ContractTraining = SQLModule.Contracts.Training;
using DomainTraining = SQLModule.Domain.Training;
using DomainValidation = SQLModule.Domain.Training.Validation;

namespace SQLModule.UnitTests.Training;

/// <summary>
/// Перечисления контракта дублируют доменные (REF-009) и преобразуются по числовому значению:
/// имена и значения обязаны совпадать, иначе изменился бы wire-формат или смысл статусов.
/// </summary>
public sealed class ContractEnumParityTests
{
    public static TheoryData<Type, Type> Pairs => new()
    {
        { typeof(ContractTraining.ExecutionStatus), typeof(DomainTraining.ExecutionStatus) },
        { typeof(ContractTraining.ValidationCheckKind), typeof(DomainTraining.ValidationCheckKind) },
        { typeof(ContractTraining.HintGroup), typeof(DomainTraining.HintGroup) },
        { typeof(ContractTraining.CheckReason), typeof(DomainTraining.CheckReason) },
        { typeof(ContractTraining.AttemptResultSnapshotState), typeof(DomainTraining.AttemptResultSnapshotState) },
        { typeof(ContractTraining.SqlConstruct), typeof(DomainTraining.SqlConstruct) },
        { typeof(ContractTraining.ValidationCheckStatus), typeof(DomainTraining.ValidationCheckStatus) },
        { typeof(ContractTraining.ValidationConfigurationState), typeof(DomainTraining.ValidationConfigurationState) },
        { typeof(ContractTraining.PublicationStatus), typeof(DomainTraining.PublicationStatus) },
        { typeof(ContractTraining.ValidationViolationSeverity), typeof(DomainValidation.ValidationViolationSeverity) },
    };

    [Theory(DisplayName = "Перечисление контракта совпадает с доменным по именам и значениям")]
    [MemberData(nameof(Pairs))]
    public void ContractEnum_MatchesDomainEnum(Type contract, Type domain)
    {
        contract.Name.ShouldBe(domain.Name, "имя типа входит в схему OpenAPI");

        Members(contract).ShouldBe(Members(domain));
    }

    private static Dictionary<string, long> Members(Type enumType) =>
        Enum.GetNames(enumType).ToDictionary(
            name => name,
            name => Convert.ToInt64(Enum.Parse(enumType, name)));
}
