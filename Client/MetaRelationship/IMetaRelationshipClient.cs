using SQLModule.Contracts.Schema.MetaRelationship;

namespace SQLModule.Client.MetaRelationship;

/// <summary>Типизированный клиент для работы с FK-связями между мета-атрибутами.</summary>
public interface IMetaRelationshipClient
    : ICrudClient<CreateMetaRelationshipRequest, UpdateMetaRelationshipRequest, MetaRelationshipResponse>;
