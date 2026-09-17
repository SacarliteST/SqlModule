using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.Training.Validation;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Validation.PreviewTaskValidation;

internal sealed record PreviewTaskValidationCommand(
    Guid TaskId,
    TaskValidationDefinition Definition,
    IReadOnlyList<ValidationPreviewIdentity> Identities)
    : IRequest<Result<TaskValidationPreviewResponse>>;

internal sealed class PreviewTaskValidationHandler(ITaskValidationEvaluationService evaluationService)
    : IRequestHandler<PreviewTaskValidationCommand, Result<TaskValidationPreviewResponse>>
{
    public async Task<Result<TaskValidationPreviewResponse>> Handle(
        PreviewTaskValidationCommand command,
        CancellationToken ct)
    {
        var evaluation = await evaluationService.EvaluateAsync(
            command.TaskId,
            command.Definition,
            command.Identities,
            ct);
        return evaluation.IsSuccess
            ? evaluation.Value!.Response
            : Result<TaskValidationPreviewResponse>.Fail(evaluation.Error!);
    }
}
