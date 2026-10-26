using Microsoft.Extensions.Options;
using Shouldly;
using SQLModule.Application.Training.Bank;

namespace SQLModule.UnitTests.Training;

/// <summary>Проверяет ограничение частоты тренировочных попыток.</summary>
public sealed class TrainingAttemptGateTests
{
    [Fact]
    public async Task TryAcquireAsync_RejectsParallelAttemptForSameUser()
    {
        using var gate = new TrainingAttemptGate(Options.Create(new TaskBankOptions
        {
            SubmissionIntervalSeconds = 60,
            RetryAfterSeconds = 3
        }));
        var userId = Guid.NewGuid();
        using var first = await gate.TryAcquireAsync(userId, CancellationToken.None);

        var second = await gate.TryAcquireAsync(userId, CancellationToken.None);

        first.ShouldNotBeNull();
        second.ShouldBeNull();
    }

    [Fact]
    public async Task TryAcquireAsync_AllowsDifferentUsers()
    {
        using var gate = new TrainingAttemptGate(Options.Create(new TaskBankOptions
        {
            SubmissionIntervalSeconds = 60,
            RetryAfterSeconds = 3
        }));
        using var first = await gate.TryAcquireAsync(Guid.NewGuid(), CancellationToken.None);
        using var second = await gate.TryAcquireAsync(Guid.NewGuid(), CancellationToken.None);

        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
    }
}
