using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.ValidateTargetDbDdl;

internal sealed record ValidateTargetDbDdlCommand(
    ValidateTargetDbDdlRequest Request) : IRequest<Result<ValidateTargetDbDdlResponse>>;

internal sealed class ValidateTargetDbDdlHandler(IDdlSchemaService service)
    : IRequestHandler<ValidateTargetDbDdlCommand, Result<ValidateTargetDbDdlResponse>>
{
    public async Task<Result<ValidateTargetDbDdlResponse>> Handle(
        ValidateTargetDbDdlCommand command, CancellationToken ct)
    {
        var request = command.Request;
        var prepared = await service.ValidateAndInspectAsync(request.DbmsId!.Value, request.DdlScript!, ct);
        return prepared.IsSuccess
            ? new ValidateTargetDbDdlResponse(
                true, [], prepared.Value!.Schema.Tables.Count, prepared.Value.Schema.Relationships.Count)
            : Result<ValidateTargetDbDdlResponse>.Fail(prepared.Error!);
    }
}
