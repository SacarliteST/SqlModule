using SQLModule.Domain.Training;
using SQLModule.Domain.Training.Validation;

namespace SQLModule.Application.Training.Validation;

public static class TaskValidationDefaults
{
    public static TaskValidationConfiguration Create(Guid taskId)
    {
        var configuration = TaskValidationConfiguration.Create(
            taskId,
            passingScore: 100,
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
