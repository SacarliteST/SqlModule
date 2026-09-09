namespace SQLModule.Domain.Training;

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
