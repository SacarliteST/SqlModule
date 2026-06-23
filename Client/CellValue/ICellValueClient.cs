using SQLModule.Contracts.Schema.CellValue;

namespace SQLModule.Client.CellValue;

/// <summary>Типизированный клиент для работы со значениями ячеек (EAV).</summary>
public interface ICellValueClient
    : ICrudClient<CreateCellValueRequest, UpdateCellValueRequest, CellValueResponse>;
