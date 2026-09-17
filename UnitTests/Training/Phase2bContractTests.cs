using Shouldly;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.Training;

namespace SQLModule.UnitTests.Training;

public sealed class Phase2bContractTests
{
    [Fact(DisplayName = "Validation enum имеют зафиксированные публичные имена")]
    public void ValidationEnums_HaveStablePublicNames()
    {
        Enum.GetNames<ValidationCheckKind>().ShouldBe([
            "MainDatasetResult",
            "RequiredConstruct",
            "ForbiddenConstruct",
            "RequiredTable",
            "ForbiddenTable"
        ]);
        Enum.GetNames<SqlConstruct>().ShouldBe([
            "Join",
            "InnerJoin",
            "LeftJoin",
            "RightJoin",
            "FullJoin",
            "GroupBy",
            "Having",
            "Distinct",
            "Subquery",
            "Cte",
            "WindowFunction"
        ]);
        Enum.GetNames<HintGroup>().ShouldBe([
            "Result",
            "RequiredConstructs",
            "ForbiddenConstructs",
            "RequiredTables",
            "ForbiddenTables"
        ]);
        Enum.GetNames<ValidationCheckStatus>().ShouldBe(["Passed", "Failed", "NotEvaluated"]);
        Enum.GetNames<ProgressStatus>().ShouldBe([
            "Active",
            "Finalizing",
            "CompletionPending",
            "CompletionFailed",
            "Completed",
            "Expired"
        ]);
    }

    [Fact(DisplayName = "HiddenDatasetResult отсутствует в validation-контракте")]
    public void ValidationCheckKind_DoesNotExposeHiddenDatasetResult()
    {
        Enum.GetNames<ValidationCheckKind>().ShouldNotContain("HiddenDatasetResult");
    }

    [Fact(DisplayName = "Request DTO допускают model binding неполного тела")]
    public void ValidationRequests_AreNullableAtApiBoundary()
    {
        var configuration = new TaskValidationConfigurationRequest(null, null, null, null, null);
        var check = new ValidationCheckRequest(null, null, null, null, null);
        var previewCheck = new ValidationCheckPreviewRequest(null, null, null, null, null, null);
        var preview = new TaskValidationPreviewRequest(null, null, null, [previewCheck]);
        var publish = new PublishTaskValidationRequest(null);

        configuration.Version.ShouldBeNull();
        check.Kind.ShouldBeNull();
        preview.Checks.ShouldNotBeNull();
        preview.Checks!.ShouldContain(previewCheck);
        publish.Version.ShouldBeNull();
    }

    [Fact(DisplayName = "Маршруты Phase 2b совпадают с публичным контрактом")]
    public void Phase2bRoutes_MatchPublicContract()
    {
        var taskId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var dbmsId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        ApiRoutes.DbmsCatalog.ValidationCapabilities.ByDbmsId
            .ShouldBe("api/v1/dbms/{dbmsId}/validation-capabilities");
        ApiRoutes.DbmsCatalog.ValidationCapabilities.ForDbms(dbmsId)
            .ShouldBe($"api/v1/dbms/{dbmsId:D}/validation-capabilities");
        ApiRoutes.Training.SqlTasks.Validation.ShouldBe("api/v1/sql-tasks/{taskId}/validation");
        ApiRoutes.Training.SqlTasks.ValidationPreview.ShouldBe("api/v1/sql-tasks/{taskId}/validation/preview");
        ApiRoutes.Training.SqlTasks.ValidationPublish.ShouldBe("api/v1/sql-tasks/{taskId}/validation/publish");
        ApiRoutes.Training.Student.TaskProgress.ShouldBe("api/v1/student/tasks/{taskId}/progress");
        ApiRoutes.Training.Student.TaskProgressRestart.ShouldBe("api/v1/student/tasks/{taskId}/progress/restart");
        ApiRoutes.Training.Student.TaskProgressFinalize.ShouldBe("api/v1/student/tasks/{taskId}/progress/finalize");
        ApiRoutes.Training.Student.ForTaskProgressFinalize(taskId)
            .ShouldBe($"api/v1/student/tasks/{taskId:D}/progress/finalize");
        ApiRoutes.ModuleIntegration.FinalizeCurrentSession
            .ShouldBe("api/v1/module-integration/sessions/current/finalize");
    }
}
