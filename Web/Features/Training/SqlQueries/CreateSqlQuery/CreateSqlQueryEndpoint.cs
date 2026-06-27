using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlQueries;

public sealed class CreateSqlQueryEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Training.SqlQueries.Collection, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("CreateSqlQuery")
            .WithTags("Training")
            .WithSummary("Создать эталонный SQL-запрос")
            .WithDescription(
                "Создаёт новый эталонный SQL-запрос. " +
                "Возвращает 201 Created с телом ответа. " +
                "400 — не прошла валидация (пустой QueryText).")
            .Produces<SqlQueryResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<CreateSqlQueryRequest>>();
    }

    private static async Task<IResult> Handle(
        CreateSqlQueryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<CreateSqlQueryCommand, Result<SqlQueryResponse>>(
            SqlQueryMappings.ToCommand(request), ct);
        return result.ToCreated(r => ApiRoutes.Training.SqlQueries.ForId(r.Id));
    }
}
