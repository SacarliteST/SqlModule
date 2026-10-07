namespace SQLModule.UnitTests.Architecture;

/// <summary>Разрешённые прямые ссылки проекта на другие проекты решения.</summary>
/// <param name="Project">Проект, зависимости которого ограничиваются.</param>
/// <param name="Allowed">Проекты, на которые можно ссылаться. Всё остальное — нарушение.</param>
/// <param name="Reason">Смысл правила для сообщения об ошибке.</param>
internal sealed record ProjectDependencyRule(string Project, string[] Allowed, string Reason);

/// <summary>Запрещённые NuGet-пакеты проекта. Шаблон <c>*</c> запрещает любые пакеты, <c>Prefix*</c> — по префиксу.</summary>
/// <param name="Project">Проект, пакеты которого ограничиваются.</param>
/// <param name="Forbidden">Шаблоны запрещённых пакетов.</param>
/// <param name="Reason">Смысл правила для сообщения об ошибке.</param>
internal sealed record PackageRule(string Project, string[] Forbidden, string Reason);

/// <summary>Запрещённые сборки в списке <c>GetReferencedAssemblies()</c> сборки слоя.</summary>
/// <param name="Assembly">Имя проверяемой сборки.</param>
/// <param name="ForbiddenPrefixes">Префиксы имён сборок, на которые слой ссылаться не должен.</param>
internal sealed record AssemblyRule(string Assembly, string[] ForbiddenPrefixes);

/// <summary>
/// Целевая архитектура backend SQLModule (docs/ARCHITECTURE_REFACTORING_PLAN.md, раздел 6) и список
/// известных отступлений от неё. Тесты в этом каталоге делают две вещи:
/// <list type="bullet">
///   <item>не пускают новые нарушения целевой архитектуры;</item>
///   <item>заставляют удалять запись из списка известных нарушений, как только оно исправлено.</item>
/// </list>
/// Список известных нарушений — это технический долг рефакторинга; растить его нельзя.
/// </summary>
internal static class ArchitectureRules
{
    private const string PlatformAbstractions = "PlatformIntegration.Abstractions";
    private const string PlatformContracts = "PlatformIntegration.Contracts";

    /// <summary>Целевой граф ссылок между проектами. Host — composition root и не ограничивается.</summary>
    public static IReadOnlyList<ProjectDependencyRule> ProjectDependencies { get; } =
    [
        new("Domain", [], "Domain — ядро и ни от чего не зависит."),
        new("Common", [], "Common — самый нижний слой общих примитивов."),
        new("Contracts", [], "Публичные Contracts не должны тянуть внутренние слои (Client → Contracts → ничего)."),
        new("Client", ["Contracts"], "Типизированный клиент знает только публичный контракт."),
        new("Data", ["Domain", "Application"], "Persistence зависит от Domain и реализует порты Application."),
        new("Sandbox", ["Common"], "Песочница изолирована от Web, Data и прикладного слоя."),
        new("SqlAnalysis", ["Domain"], "Анализ SQL — самостоятельная библиотека."),
        new("Education.Client", [PlatformAbstractions, PlatformContracts], "Интеграционный клиент не знает о Web и Data."),
        new("Identity.Client", [], "Интеграционный клиент не знает о Web и Data."),
        new("Kafka.Publisher", [PlatformAbstractions, PlatformContracts], "Интеграционный клиент не знает о Web и Data."),
        new(PlatformAbstractions, [PlatformContracts], "Порты платформенной интеграции."),
        new(PlatformContracts, [], "Wire-контракты платформенной интеграции."),
        new(
            "Application",
            ["Common", "Contracts", "Domain", "SqlAnalysis", "Sandbox", PlatformAbstractions, PlatformContracts],
            "Application не зависит от Web, Host и Data: Data реализует его порты."),
        new(
            "Web",
            ["Application", "Common", "Contracts", "Domain", PlatformAbstractions, PlatformContracts],
            "Web — HTTP-слой: composition root в Host, прикладная логика в Application."),
    ];

    /// <summary>Известные нарушения графа проектов в формате <c>Проект -&gt; Зависимость</c>.</summary>
    public static IReadOnlyDictionary<string, string> KnownProjectViolations { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Contracts -> Domain"] = "REF-009: Contracts использует доменные enum.",
            ["Web -> Data"] = "REF-002/004/005: AddData и AppDbContext используются из Web.",
            ["Web -> Sandbox"] = "REF-003/004: sandbox-код вызывается и регистрируется из Web.",
            ["Web -> Education.Client"] = "REF-002: регистрация интеграций должна переехать в Host.",
            ["Web -> Identity.Client"] = "REF-002: регистрация интеграций должна переехать в Host.",
            ["Web -> Kafka.Publisher"] = "REF-002: регистрация интеграций должна переехать в Host.",
            ["Web -> SqlAnalysis"] = "REF-004/005: анализ SQL уйдёт в Application вместе со сценариями.",
        };

    /// <summary>Запрещённые NuGet-пакеты по проектам.</summary>
    public static IReadOnlyList<PackageRule> ForbiddenPackages { get; } =
    [
        new("Domain", ["*"], "Domain не имеет внешних зависимостей."),
        new("Common", ["*"], "Common не имеет внешних зависимостей."),
        new("Contracts", ["*"], "Contracts не имеет внешних зависимостей."),
        new(PlatformAbstractions, ["*"], "Порты не имеют внешних зависимостей."),
        new(PlatformContracts, ["*"], "Wire-контракты не имеют внешних зависимостей."),
        new("Client", ["Microsoft.AspNetCore*", "Microsoft.EntityFrameworkCore*"], "Клиент — не серверный код."),
        new("Data", ["Microsoft.AspNetCore*"], "Persistence не зависит от ASP.NET Core."),
        new("Sandbox", ["Microsoft.AspNetCore*", "Microsoft.EntityFrameworkCore*"], "Песочница не зависит от Web и persistence."),
        new("SqlAnalysis", ["Microsoft.AspNetCore*", "Microsoft.EntityFrameworkCore*", "Npgsql*"], "Анализ SQL — чистая библиотека."),
        new("Education.Client", ["Microsoft.AspNetCore*", "Microsoft.EntityFrameworkCore*", "Npgsql*"], "Интеграционный клиент."),
        new("Identity.Client", ["Microsoft.AspNetCore*", "Microsoft.EntityFrameworkCore*", "Npgsql*"], "Интеграционный клиент."),
        new("Kafka.Publisher", ["Microsoft.AspNetCore*", "Microsoft.EntityFrameworkCore*", "Npgsql*"], "Интеграционный клиент."),
        new("Application", ["Microsoft.AspNetCore*", "Npgsql*", "Testcontainers*"], "Application не знает о HTTP и провайдере БД."),
        new("Web", ["Testcontainers*", "Npgsql*", "Microsoft.EntityFrameworkCore*"], "Web не должен содержать инфраструктуру."),
    ];

    /// <summary>Известные нарушения пакетных правил в формате <c>Проект -&gt; Пакет</c>.</summary>
    public static IReadOnlyDictionary<string, string> KnownPackageViolations { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Web -> Microsoft.EntityFrameworkCore"] = "REF-004/005: handlers используют EF Core, пока не вынесены в Application.",
        };

    /// <summary>Ссылки слоёв на сборки, которых у них быть не должно (проверяется по метаданным).</summary>
    public static IReadOnlyList<AssemblyRule> AssemblyDependencies { get; } = CreateAssemblyRules();

    /// <summary>Известные нарушения ссылок на сборки в формате <c>Сборка -&gt; Сборка</c>.</summary>
    public static IReadOnlyDictionary<string, string> KnownAssemblyViolations { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Классы Web, унаследованные от <c>BackgroundService</c>. Фоновые процессы должны жить вне HTTP-слоя.</summary>
    public static IReadOnlyDictionary<string, string> KnownWebBackgroundServices { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["PendingPublishWorker"] = "REF-005: уйдёт вместе с ModuleIntegration в Application/hosting.",
            ["ModuleSessionCleanupWorker"] = "REF-005: уйдёт вместе с ModuleIntegration в Application/hosting.",
        };

    /// <summary>Простые имена типов, которые endpoint-классы не должны получать напрямую.</summary>
    public static IReadOnlyList<string> TypesForbiddenInEndpoints { get; } =
    [
        "AppDbContext",
        "ISandboxExecutor",
        "IEducationCompletionClient",
        "IIdentityAuthClient",
        "IPracticeEventPublisher",
    ];

    /// <summary>Известные endpoint-классы с прямым доступом к запрещённым типам.</summary>
    public static IReadOnlyDictionary<string, string> KnownEndpointViolations { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["GetAttemptFilterOptionsEndpoint"] = "REF-005: читает AppDbContext напрямую — вынести запрос в Application.",
            ["GetCurrentModuleSessionEndpoint"] = "REF-005: читает AppDbContext напрямую — вынести запрос в Application.",
            ["GetTasksCatalogEndpoint"] = "REF-005: читает AppDbContext напрямую — вынести запрос в Application.",
            ["UpsertModuleSessionEndpoint"] = "REF-005: пишет через AppDbContext напрямую — вынести команду в Application.",
            ["StudentEndpoints"] = "REF-005: читает AppDbContext напрямую — вынести запросы в Application.",
            ["StandaloneLoginEndpoint"] = "REF-005: вызывает IIdentityAuthClient напрямую — вынести команду в Application.",
            ["StandaloneLogoutEndpoint"] = "REF-005: вызывает IIdentityAuthClient напрямую — вынести команду в Application.",
            ["StandaloneRefreshEndpoint"] = "REF-005: вызывает IIdentityAuthClient напрямую — вынести команду в Application.",
        };

    private static List<AssemblyRule> CreateAssemblyRules()
    {
        string[] webFramework = ["Microsoft.AspNetCore"];
        string[] infrastructure =
            ["Microsoft.EntityFrameworkCore", "Npgsql", "MySqlConnector", "Docker", "Testcontainers", "Confluent"];

        return
        [
            new("SQLModule.Domain", [.. webFramework, .. infrastructure, .. Layers("Web", "Data", "Sandbox", "Host")]),
            new("SQLModule.Contracts", [.. webFramework, .. infrastructure, .. Layers("Web", "Data", "Sandbox", "Host")]),
            new("SQLModule.Data", [.. webFramework, .. Layers("Web", "Sandbox", "Host")]),
            new("SQLModule.Sandbox", [.. webFramework, "Microsoft.EntityFrameworkCore", .. Layers("Web", "Data", "Host")]),
            new("SQLModule.SqlAnalysis", [.. webFramework, .. infrastructure, .. Layers("Web", "Data", "Sandbox", "Host")]),
            new("SQLModule.Education.Client", [.. webFramework, "Microsoft.EntityFrameworkCore", .. Layers("Web", "Data", "Host")]),
            new("SQLModule.Identity.Client", [.. webFramework, "Microsoft.EntityFrameworkCore", .. Layers("Web", "Data", "Host")]),
            new("SQLModule.Kafka.Publisher", [.. webFramework, "Microsoft.EntityFrameworkCore", .. Layers("Web", "Data", "Host")]),
            new("SQLModule.Application", [.. webFramework, "Npgsql", "Testcontainers", .. Layers("Web", "Data", "Host")]),
        ];
    }

    private static string[] Layers(params string[] names) =>
        [.. names.Select(name => $"SQLModule.{name}")];
}
