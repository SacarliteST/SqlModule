using SQLModule.Domain.Training;
using SQLModule.Domain.Training.Validation;

namespace SQLModule.Web.Features.Training.Validation;

internal static class TaskValidationDefaults
{
    internal static TaskValidationConfiguration Create(Guid taskId)
    {
        var configuration = TaskValidationConfiguration.Create(
            taskId,
            passingScore: 100,
            maxAttempts: null,
            [HintGroup.Result]);
        configuration.SynchronizeChecks(
        [
            new ValidationCheckDefinition(
                null,
                ValidationCheckKind.MainDatasetResult,
                null,
                100,
                0)
        ]);
        return configuration;
    }
}
