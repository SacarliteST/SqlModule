using SQLModule.Sandbox;
using SQLModule.Application.TaskMaterialization;

namespace SQLModule.Application.Training.Attempts;

public interface IResultComparer
{
    CheckOutcome Compare(GoldenResult expected, QueryResultSet actual, bool strictRowOrder = true);
}
