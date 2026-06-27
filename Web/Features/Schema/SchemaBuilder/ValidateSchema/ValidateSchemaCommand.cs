using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.ValidateSchema;

internal record ValidateSchemaCommand(CreateSchemaRequest Request) : IRequest<Result>;

internal sealed class ValidateSchemaHandler(ISchemaPreparer preparer)
    : IRequestHandler<ValidateSchemaCommand, Result>
{
    public async Task<Result> Handle(ValidateSchemaCommand command, CancellationToken ct)
    {
        var result = await preparer.PrepareAndValidateAsync(command.Request, ct);
        return result.IsSuccess ? Result.Success() : Result.Fail(result.Error!);
    }
}
