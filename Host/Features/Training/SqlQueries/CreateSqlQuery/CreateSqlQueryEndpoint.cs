using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Training.SqlQueries;

public sealed class CreateSqlQueryEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Training.SqlQueries.Collection, Handle)
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
