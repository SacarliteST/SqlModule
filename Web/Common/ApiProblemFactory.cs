using System.Diagnostics;
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
        IDictionary<string, string[]>? errors = null,
        long? affectedRows = null,
        string severity = "Error",
        long? limit = null)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };

        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? String.Empty;
        problem.Extensions["errors"] = errors ?? new Dictionary<string, string[]>();
        problem.Extensions["violations"] = (errors ?? new Dictionary<string, string[]>())
            .SelectMany(pair => pair.Value.Select(message => new
            {
                path = pair.Key,
                code,
                message,
                severity,
                affectedRows,
                limit
            }))
            .ToArray();
        return problem;
    }

    internal static IResult ToResult(
        int status,
        string title,
        string detail,
        string code,
        IDictionary<string, string[]>? errors = null,
        long? affectedRows = null,
        string severity = "Error",
        long? limit = null)
        => TypedResults.Problem(Create(status, title, detail, code, errors, affectedRows, severity, limit));
}
