using Contracts.Template;

namespace TemplateApi.Client.Template;

/// <summary>
/// Интерфейс клиента палет
/// </summary>
public interface ITemplateClient : ITemplateClientBase<TemplateResponse, TemplateResponse, CreateOrUpdateTemplateRequest, Guid>;
