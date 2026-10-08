using SQLModule.Sandbox;
using SQLModule.Application.TaskMaterialization;

namespace SQLModule.Web.Features.Training.Attempts;

internal interface IResultComparer
{
    CheckOutcome Compare(GoldenResult expected, QueryResultSet actual, bool strictRowOrder = true);
}
