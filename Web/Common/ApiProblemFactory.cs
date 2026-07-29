using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SQLModule.Web.Common;

internal static class ApiProblemFactory
{
    internal static ProblemDetails Create(
        int status,
        string title,
        string detail,
        string code,
        IDictionary<string, string[]>? errors = null)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };

        problem.Extensions["code"] = code;
        problem.Extensions["errors"] = errors ?? new Dictionary<string, string[]>();
        return problem;
    }

    internal static IResult ToResult(
        int status,
        string title,
        string detail,
        string code,
        IDictionary<string, string[]>? errors = null)
        => TypedResults.Problem(Create(status, title, detail, code, errors));
}
