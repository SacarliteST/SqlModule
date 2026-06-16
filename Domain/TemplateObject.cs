namespace Domain;

/// <summary>
/// Шаблонный объект
/// </summary>
public sealed class TemplateObject
{
    public Guid Id { get; private set; }

    public string? TemplateName { get; private set; }

    private TemplateObject(string? templateName)
    {
        TemplateName = templateName;
    }

    /// <summary>
    /// Создать шаблонный объект
    /// </summary>
    public static TemplateObject Create(string? templateName)
    {
        return new(templateName);
    }

    /// <summary>
    /// Обновить данные шаблонного объекта
    /// </summary>
    public void Update(string? templateName)
    {
        TemplateName = templateName;
    }
}
