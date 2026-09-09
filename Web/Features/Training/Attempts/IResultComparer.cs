using SQLModule.Sandbox;
using SQLModule.Web.Common.Sandbox;

namespace SQLModule.Web.Features.Training.Attempts;

internal interface IResultComparer
{
    CheckOutcome Compare(GoldenResult expected, QueryResultSet actual, bool strictRowOrder = true);
}
