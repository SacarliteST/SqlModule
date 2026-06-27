using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.ValidateSchema;

internal sealed class ValidateSchemaEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.SchemaBuilder.Validate, Handle)
            .WithName("ValidateSchema")
            .WithTags("Schema")
            .WithSummary("Проверить схему в контейнере")
            .WithDescription("Генерирует DDL из черновика схемы и прогоняет его в реальном Docker-контейнере СУБД. Мета не сохраняется.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter<ValidationFilter<CreateSchemaRequest>>();
    }

    private static async Task<IResult> Handle(
        CreateSchemaRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<ValidateSchemaCommand, Result>(
            new ValidateSchemaCommand(request), ct);
        return result.ToNoContent();
    }
}
