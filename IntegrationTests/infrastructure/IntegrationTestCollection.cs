namespace SQLModule.IntegrationTests.infrastructure;

/// <summary>
/// xUnit-коллекция, гарантирующая один экземпляр <see cref="TestApplication"/> на все тесты.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<TestApplication>
{
    /// <summary>Имя коллекции — используется в атрибуте <c>[Collection]</c>.</summary>
    public const string Name = "Integration tests collection";

    /// <summary>Категория для фильтрации тестов через <c>dotnet test --filter</c>.</summary>
    public const string Category = "IntegrationTests";
}
