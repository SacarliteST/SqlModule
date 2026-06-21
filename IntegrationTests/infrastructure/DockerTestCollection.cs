namespace SQLModule.IntegrationTests.infrastructure;

/// <summary>
/// xUnit-коллекция для Docker-тестов с реальным ISandboxExecutor.
/// Используется отдельно от основного прогона, который заменяет executor фейком.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DockerTestCollection : ICollectionFixture<DockerTestApplication>
{
    /// <summary>Имя коллекции.</summary>
    public const string Name = "Docker sandbox tests";

    /// <summary>Категория для фильтрации: <c>dotnet test --filter Category=DockerTests</c>.</summary>
    public const string Category = "DockerTests";
}
