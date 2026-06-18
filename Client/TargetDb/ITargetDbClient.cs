using SQLModule.Contracts.Schema.TargetDb;

namespace SQLModule.Client.TargetDb;

/// <summary>Typed HTTP-клиент для работы с TargetDb.</summary>
public interface ITargetDbClient
    : ICrudClient<CreateTargetDbRequest, UpdateTargetDbRequest, TargetDbResponse>;
