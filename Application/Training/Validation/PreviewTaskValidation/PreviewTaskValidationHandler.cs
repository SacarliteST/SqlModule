using SQLModule.Application.Cqrs;
using SQLModule.Application.Training.Validation;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.Training.Validation;

namespace SQLModule.Application.Training.Validation.PreviewTaskValidation;

public sealed record PreviewTaskValidationCommand(
    Guid TaskId,
    TaskValidationDefinition Definition,
    IReadOnlyList<ValidationPreviewIdentity> Identities)
    : IRequest<Result<TaskValidationPreviewResponse>>;

public sealed class PreviewTaskValidationHandler(
    ITaskValidationEvaluationService evaluationService,
    ValidationSkillResolver skillResolver)
    : IRequestHandler<PreviewTaskValidationCommand, Result<TaskValidationPreviewResponse>>
{
    public async Task<Result<TaskValidationPreviewResponse>> Handle(
        PreviewTaskValidationCommand command,
        CancellationToken ct)
    {
        var skills = await skillResolver.EnsureAsync(command.Definition, ct);
        if (!skills.IsSuccess)
        {
            return Result<TaskValidationPreviewResponse>.Fail(skills.Error!);
        }

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
