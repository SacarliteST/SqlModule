using SQLModule.Contracts.Training.Attempt;

namespace SQLModule.Client.Attempt;

/// <summary>Клиент для работы с попытками выполнения заданий.</summary>
public interface IAttemptClient : ICrudClient<CreateAttemptRequest, UpdateAttemptRequest, AttemptResponse>;
