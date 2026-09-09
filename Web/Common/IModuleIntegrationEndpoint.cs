namespace SQLModule.Web.Common;

/// <summary>
/// Маркер endpoint интеграции с платформой. Такие маршруты регистрируются только
/// при включённом профиле <c>ModuleIntegration:Enabled</c>.
/// </summary>
public interface IModuleIntegrationEndpoint : IEndpoint;
