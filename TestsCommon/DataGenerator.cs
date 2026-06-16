using AutoFixture;
using SQLModule.Contracts.Template;
using SQLModule.Domain;

namespace SQLModule.TestsCommon;

public static class DataGenerator
{
    private static readonly IFixture AutoFixture = new Fixture();

    public static CreateOrUpdateTemplateRequest GenerateValidCreateOrUpdateTemplateRequest(TemplateObject template)
        => new() { TemplateName = template.TemplateName };

    public static TemplateObject GenerateTemplate()
        => TemplateObject.Create(AutoFixture.Create<string>());
}
