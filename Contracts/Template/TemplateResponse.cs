namespace Contracts.Template;

/// <summary>
/// Модель ответа с информацией о шаблонном объекте
/// </summary>
public class TemplateResponse
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Имя шаблона
    /// </summary>
    public string? TemplateName { get; set; }
}
