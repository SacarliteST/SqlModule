using SQLModule.Contracts.Training.SqlQuery;

namespace SQLModule.Client.SqlQuery;

/// <summary>Клиент для работы с эталонными SQL-запросами тренажёра.</summary>
public interface ISqlQueryClient : ICrudClient<CreateSqlQueryRequest, UpdateSqlQueryRequest, SqlQueryResponse>;
