namespace SQLModule.Host.Common;

/// <summary>
/// Маркер dev-эндпоинта: маппится только при IsDevelopment() или DevTools:Enabled=true.
/// Используется для структурных мутаций схемы в обход CreateSchema — только для разработки.
/// </summary>
public interface IDevEndpoint : IEndpoint { }
