using Microsoft.Extensions.Options;
using SQLModule.Client.Configurations;
using SQLModule.Contracts;
using SQLModule.Contracts.Template;

namespace SQLModule.Client.Template;

internal sealed class TemplateClient :
    TemplateClientBase<TemplateResponse, TemplateResponse, CreateOrUpdateTemplateRequest, Guid>,
    ITemplateClient
{
    public TemplateClient(
        HttpClient httpClient,
        IOptions<TemplateClientOptions> templateClientOptions)
        : base(httpClient, baseUri: templateClientOptions.Value.ServerUrl)
    {
    }

    protected override Uri BaseUrl => new(BaseUri + ApiRoutes.Template.TemplateObjects);

    protected override Uri GetUriForId(Guid id)
        => new(BaseUri + ApiRoutes.Template.ForTemplateObject(id));

    protected override Uri GetUriForPagination(int offset, int limit)
        => new(BaseUri + ApiRoutes.Template.ForTemplateObjectPagination(offset, limit));
}
