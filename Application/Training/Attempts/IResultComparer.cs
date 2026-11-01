using SQLModule.Application.TaskMaterialization;
using SQLModule.Sandbox;

namespace SQLModule.Application.Training.Attempts;

public interface IResultComparer
{
    CheckOutcome Compare(GoldenResult expected, QueryResultSet actual, bool strictRowOrder = true);
}
