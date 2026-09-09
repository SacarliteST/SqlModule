namespace SQLModule.Web.Features.ModuleIntegration;

internal sealed record PracticeCompletionRequest(
    string SessionKey,
    int Grade,
    PracticeCompletionData CompletionData,
    DateTimeOffset CompletedAt);

internal sealed record PracticeCompletionData(
    int TotalAttempts,
    Guid CorrectAttemptId);
