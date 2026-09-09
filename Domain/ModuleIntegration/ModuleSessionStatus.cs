namespace SQLModule.Domain.ModuleIntegration;

/// <summary>Локальное состояние платформенной сессии.</summary>
public enum ModuleSessionStatus
{
    Active,
    CompletionPending,
    Completed,
    CompletionFailed,
    Expired
}
