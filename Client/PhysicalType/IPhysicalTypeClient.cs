using SQLModule.Contracts.DbmsCatalog.PhysicalType;

namespace SQLModule.Client.PhysicalType;

/// <summary>Типизированный клиент для работы с физическими типами данных.</summary>
public interface IPhysicalTypeClient
    : ICrudClient<CreatePhysicalTypeRequest, UpdatePhysicalTypeRequest, PhysicalTypeResponse>;
