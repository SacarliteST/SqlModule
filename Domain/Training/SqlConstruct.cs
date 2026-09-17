namespace SQLModule.Domain.Training;

/// <summary>Нормализованная SQL-конструкция, распознаваемая AST-анализатором.</summary>
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
