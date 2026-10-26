using Shouldly;
using SQLModule.Domain.Training;

namespace SQLModule.UnitTests.Training;

/// <summary>Проверяет доменные инварианты задания открытого банка.</summary>
public sealed class TaskBankDomainTests
{
    private static readonly DateTimeOffset FirstPublishedAt =
        new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "Новое задание является контрольным и показывает разбор после решения")]
    public void Create_UsesSafeBankDefaults()
    {
        var task = CreateTask();

        task.Visibility.ShouldBe(TaskVisibility.Control);
        task.BankNumber.ShouldBeNull();
        task.BankPublishedAt.ShouldBeNull();
        task.ShowSolution.ShouldBeTrue();
    }

    [Fact(DisplayName = "Повторное открытие банка не меняет номер и дату первой публикации")]
    public void OpenInBank_PreservesFirstPublication()
    {
        var task = CreateTask();
        task.OpenInBank(12, FirstPublishedAt);
        task.CloseBank();

        task.OpenInBank(99, FirstPublishedAt.AddDays(1));

        task.Visibility.ShouldBe(TaskVisibility.Open);
        task.BankNumber.ShouldBe(12);
        task.BankPublishedAt.ShouldBe(FirstPublishedAt);
    }

    [Fact(DisplayName = "Тексты банка нормализуются без изменения остальных полей задания")]
    public void UpdateBankContent_NormalizesOptionalText()
    {
        var task = CreateTask();

        task.UpdateBankContent("  Подумайте о JOIN  ", false, "   ");

        task.BankHint.ShouldBe("Подумайте о JOIN");
        task.ShowSolution.ShouldBeFalse();
        task.SolutionExplanation.ShouldBeNull();
    }

    [Fact(DisplayName = "Журнал подсказки сохраняет выданные группы маской")]
    public void HintAccess_StoresIssuedGroups()
    {
        var access = BankHintAccess.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            3,
            [HintGroup.RequiredTables, HintGroup.ForbiddenConstructs],
            true,
            FirstPublishedAt);

        access.GetAutoHintGroups().ShouldBe([
            HintGroup.ForbiddenConstructs,
            HintGroup.RequiredTables
        ]);
        access.AuthorHintIssued.ShouldBeTrue();
    }

    private static SqlTask CreateTask() =>
        SqlTask.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Задание",
            "Условие",
            1);
}
