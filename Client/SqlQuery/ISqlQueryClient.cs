using SQLModule.Contracts.Training.SqlQuery;

namespace SQLModule.Client.SqlQuery;

/// <summary>Клиент для работы с эталонными SQL-запросами тренажёра.</summary>
public interface ISqlQueryClient : ICrudClient<CreateSqlQueryRequest, UpdateSqlQueryRequest, SqlQueryResponse>
{
    /// <summary>Проверить SQL-запрос в песочнице без сохранения.</summary>
    Task<ValidateSqlQueryResponse> ValidateAsync(
        ValidateSqlQueryRequest request,
        CancellationToken ct = default);
}
