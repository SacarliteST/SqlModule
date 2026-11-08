namespace SQLModule.Application.Training.Episodes;

public sealed class LearningEpisodeOptions
{
    public const string SectionKey = "LearningEpisodes";

    public int CleanupIntervalSeconds { get; init; } = 30;
    public int BatchSize { get; init; } = 100;
}
