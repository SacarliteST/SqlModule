namespace TemplateApi.IntegrationsTest.infrastructure;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<TestApplication>
{
    public const string Name = "Integration tests collection";
    public const string Category = "IntegrationTests";
}
