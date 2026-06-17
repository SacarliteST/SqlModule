namespace SQLModule.Domain.Exceptions;

/// <summary>Выбрасывается, когда запрошенная сущность не найдена в хранилище.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string entityName, object id)
        : base($"{entityName} с Id: {id} не найден") { }
}
