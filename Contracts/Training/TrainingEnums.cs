namespace SQLModule.Contracts.Training;

// Публичные перечисления контракта. Имена и значения совпадают с одноимёнными доменными перечислениями:
// это часть wire-формата, а соответствие защищает тест паритета (UnitTests/Training/ContractEnumParityTests).
// Комментарии намеренно не XML-документация: иначе описания попадут в схему OpenAPI и изменят опубликованный контракт.
// Статус выполнения запроса студента.
public enum ExecutionStatus
{
    Succeeded = 0,
    Error = 1,
    TimedOut = 2
}

// Вид критерия составной проверки SQL-решения.
public enum ValidationCheckKind
{
    MainDatasetResult,
    RequiredConstruct,
    ForbiddenConstruct,
    RequiredTable,
    ForbiddenTable
}

// Группа диагностики, которую преподаватель может открыть студенту.
public enum HintGroup
{
    Result,
    RequiredConstructs,
    ForbiddenConstructs,
    RequiredTables,
    ForbiddenTables
}

// Причина итога проверки результата.
public enum CheckReason
{
    Ok = 0,
    ColumnMismatch = 1,
    RowCountMismatch = 2,
    ValueMismatch = 3,
    SqlError = 4,
    Timeout = 5,
    NotRun = 6,
    ResultLimitExceeded = 7
}

// Состояние сохранённого безопасного результата попытки.
public enum AttemptResultSnapshotState
{
    NotStored = 0,
    Available = 1,
    NotProduced = 2,
    Expired = 3
}

// Нормализованная SQL-конструкция, распознаваемая AST-анализатором.
public enum SqlConstruct
{
    Join,
    InnerJoin,
    LeftJoin,
    RightJoin,
    FullJoin,
    GroupBy,
    Having,
    Distinct,
    Subquery,
    Cte,
    WindowFunction
}

// Результат выполнения отдельного критерия проверки.
public enum ValidationCheckStatus
{
    Passed,
    Failed,
    NotEvaluated
}

// Состояние платформенной сессии SQL-модуля.
public enum SessionStatus
{
    Active,
    CompletionPending,
    Completed,
    CompletionFailed,
    Expired
}

// Причина завершения платформенной сессии.
public enum FinalizationReason
{
    Manual,
    PerfectScore,
    AttemptsExhausted,
    Expired,
    EducationClosed
}

// Состояние текущей редактируемой validation-конфигурации.
public enum ValidationConfigurationState
{
    Draft,
    Published
}

// Статус публикации SQL-задания.
public enum PublicationStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

// Серьёзность нарушения validation-конфигурации.
public enum ValidationViolationSeverity
{
    Error,
    Warning
}
