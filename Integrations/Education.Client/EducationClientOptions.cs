namespace SQLModule.Education.Client;

internal sealed class EducationClientOptions
{
    public string ServiceKey { get; init; } = String.Empty;
    public string EducationBaseUrl { get; init; } = String.Empty;
    public EducationCompletionOptions EducationCompletion { get; init; } = new();
}

internal sealed class EducationCompletionOptions
{
    public int RequestTimeoutSeconds { get; init; } = 10;
}
