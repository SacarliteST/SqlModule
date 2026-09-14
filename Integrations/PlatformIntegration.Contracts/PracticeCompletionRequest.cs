namespace SQLModule.PlatformIntegration.Contracts;

public sealed record PracticeCompletionRequest(
    string SessionKey,
    int Grade,
    PracticeCompletionData CompletionData,
    DateTimeOffset CompletedAt);

public sealed record PracticeCompletionData(
    int TotalAttempts,
    Guid CorrectAttemptId);
