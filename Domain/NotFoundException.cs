namespace Domain;

/// <summary>
/// Выбрасывается, когда запрошенная сущность не найдена в хранилище
/// </summary>
public sealed class NotFoundException : Exception
{
    /// <summary>
    /// Создать исключение с указанием типа и идентификатора сущности
    /// </summary>
    public NotFoundException(string entityName, object id)
        : base($"{entityName} с Id: {id} не найден")
    {
    }
}
