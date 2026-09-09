namespace SQLModule.Web.Features.ModuleIntegration;

/// <summary>Настройки платформенного профиля SQL-модуля.</summary>
internal sealed class ModuleIntegrationOptions
{
    internal const string SectionKey = "ModuleIntegration";

    /// <summary>Включить endpoints и фоновые сервисы интеграционного контура.</summary>
    public bool Enabled { get; init; }

    /// <summary>Общий секрет сервер-сервер запросов между Education и SqlModule.</summary>
    public string ServiceKey { get; init; } = String.Empty;

    /// <summary>Базовый адрес Education API для отправки итоговой оценки.</summary>
    public string EducationBaseUrl { get; init; } = String.Empty;

    /// <summary>Настройки публикации цифрового следа.</summary>
    public ModuleIntegrationKafkaOptions Kafka { get; init; } = new();

    /// <summary>Общие настройки фоновой обработки outbox.</summary>
    public ModuleIntegrationPublisherOptions Publisher { get; init; } = new();

    /// <summary>Настройки доставки итоговой оценки в Education.</summary>
    public ModuleIntegrationEducationCompletionOptions EducationCompletion { get; init; } = new();

    /// <summary>Настройки хранения локальных платформенных сессий.</summary>
    public ModuleSessionLifecycleOptions Lifecycle { get; init; } = new();
}

internal sealed class ModuleSessionLifecycleOptions
{
    public int CleanupIntervalMinutes { get; init; } = 60;
    public int RetentionDays { get; init; } = 7;
    public int BatchSize { get; init; } = 100;
}

internal sealed class ModuleIntegrationKafkaOptions
{
    public string BootstrapServers { get; init; } = String.Empty;
    public string EventsTopic { get; init; } = "scoodle.practice.events";
    public bool PublisherEnabled { get; init; } = true;
    public int MaxAttempts { get; init; } = 10;
    public int InitialRetryDelaySeconds { get; init; } = 2;
    public int MaxRetryDelaySeconds { get; init; } = 60;
}

internal sealed class ModuleIntegrationPublisherOptions
{
    public int BatchSize { get; init; } = 50;
    public int PollIntervalSeconds { get; init; } = 5;
    public int SentRetentionDays { get; init; } = 14;
}

internal sealed class ModuleIntegrationEducationCompletionOptions
{
    public int MaxAttempts { get; init; } = 10;
    public int InitialRetryDelaySeconds { get; init; } = 2;
    public int MaxRetryDelaySeconds { get; init; } = 300;
    public int RequestTimeoutSeconds { get; init; } = 10;
}
