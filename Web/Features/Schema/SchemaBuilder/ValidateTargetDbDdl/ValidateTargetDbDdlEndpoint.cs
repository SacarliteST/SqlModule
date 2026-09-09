using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.ValidateTargetDbDdl;

internal sealed class ValidateTargetDbDdlEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.TargetDbs.ValidateDdl, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("ValidateTargetDbDdl")
            .WithTags("Schema")
            .WithSummary("Проверить пользовательский DDL без сохранения")
            .Produces<ValidateTargetDbDdlResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .AddEndpointFilter<ValidationFilter<ValidateTargetDbDdlRequest>>();
    }

    private static async Task<IResult> Handle(
        ValidateTargetDbDdlRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<ValidateTargetDbDdlCommand, Result<ValidateTargetDbDdlResponse>>(
            new ValidateTargetDbDdlCommand(request), ct);
        return result is { IsSuccess: false, Error.Type: ErrorType.Validation }
            ? ApiProblemFactory.ToResult(
                StatusCodes.Status422UnprocessableEntity, "Ошибка проверки DDL", result.Error.Message,
                result.Error.Code, new Dictionary<string, string[]> { ["ddlScript"] = [result.Error.Message] })
            : result.ToOk();
    }
}
