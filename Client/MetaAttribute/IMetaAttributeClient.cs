using SQLModule.Contracts.Schema.MetaAttribute;

namespace SQLModule.Client.MetaAttribute;

/// <summary>Типизированный клиент для работы с мета-атрибутами (колонками) таблиц.</summary>
public interface IMetaAttributeClient
    : ICrudClient<CreateMetaAttributeRequest, UpdateMetaAttributeRequest, MetaAttributeResponse>;
