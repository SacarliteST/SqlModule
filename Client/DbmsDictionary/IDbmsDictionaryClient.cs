using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;

namespace SQLModule.Client.DbmsDictionary;

/// <summary>Типизированный клиент для работы со справочником СУБД.</summary>
public interface IDbmsDictionaryClient
    : ICrudClient<CreateDbmsDictionaryRequest, UpdateDbmsDictionaryRequest, DbmsDictionaryResponse>
{
    /// <summary>Проверяет Docker-конфигурацию без сохранения записи. 204 — успех, 409 — конфигурация нерабочая.</summary>
    Task ValidateAsync(CreateDbmsDictionaryRequest request, CancellationToken ct = default);
}
