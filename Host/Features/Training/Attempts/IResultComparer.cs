using SQLModule.Host.Common.Sandbox;
using SQLModule.Sandbox;

namespace SQLModule.Host.Features.Training.Attempts;

internal interface IResultComparer
{
    CheckOutcome Compare(GoldenResult expected, QueryResultSet actual);
}
