namespace SQLModule.Domain.Training;

/// <summary>Вид критерия составной проверки SQL-решения.</summary>
public enum ValidationCheckKind
{
    MainDatasetResult,
    RequiredConstruct,
    ForbiddenConstruct,
    RequiredTable,
    ForbiddenTable
}
