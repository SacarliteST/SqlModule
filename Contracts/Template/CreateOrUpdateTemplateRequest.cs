namespace Contracts.Template;

/// <summary>
/// Модель запроса на создание шаблонного объекта
/// </summary>
public sealed class CreateOrUpdateTemplateRequest
{
    /// <summary>
    /// Имя шаблона
    /// </summary>
    public string? TemplateName { get; set; }
}
