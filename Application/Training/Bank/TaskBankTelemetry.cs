using System.Diagnostics.Metrics;

namespace SQLModule.Application.Training.Bank;

/// <summary>Метрики сценариев открытого банка.</summary>
public static class TaskBankTelemetry
{
    private static readonly Meter Meter = new("SQLModule.TaskBank");
    private static readonly Counter<long> Attempts = Meter.CreateCounter<long>("sqlmodule_task_bank_attempts_total");
    private static readonly Counter<long> RateLimited = Meter.CreateCounter<long>("sqlmodule_task_bank_rate_limited_total");

    /// <summary>Регистрирует принятую тренировочную попытку.</summary>
    public static void RecordAttempt() => Attempts.Add(1);

    /// <summary>Регистрирует отклонение по локальному лимиту.</summary>
    public static void RecordRateLimited() => RateLimited.Add(1);
}
