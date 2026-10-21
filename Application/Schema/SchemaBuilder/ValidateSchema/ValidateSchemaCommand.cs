using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Schema.SchemaBuilder.ValidateSchema;

public record ValidateSchemaCommand(CreateSchemaRequest Request) : IRequest<Result>;

public sealed class ValidateSchemaHandler(ISchemaPreparer preparer)
    : IRequestHandler<ValidateSchemaCommand, Result>
{
    public async Task<Result> Handle(ValidateSchemaCommand command, CancellationToken ct)
    {
        var result = await preparer.PrepareAndValidateAsync(command.Request, ct);
        return result.IsSuccess ? Result.Success() : Result.Fail(result.Error!);
    }
}
