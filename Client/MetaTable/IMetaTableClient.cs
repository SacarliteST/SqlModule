using SQLModule.Contracts.Schema.MetaTable;

namespace SQLModule.Client.MetaTable;

/// <summary>Клиент для работы с мета-таблицами схемы данных.</summary>
public interface IMetaTableClient : ICrudClient<CreateMetaTableRequest, UpdateMetaTableRequest, MetaTableResponse>;
