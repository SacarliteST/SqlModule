using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SQLModule.Web.Common;

namespace SQLModule.Web.Features.ModuleIntegration;

/// <summary>Проверяет серверный ключ до чтения и валидации тела integration-запроса.</summary>
internal sealed class ModuleIntegrationServiceKeyFilter(
    IOptions<ModuleIntegrationOptions> options) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var serviceKey = context.HttpContext.Request.Headers["X-Service-Key"].FirstOrDefault();
        if (!ServiceKeyValidator.IsValid(serviceKey, options.Value.ServiceKey))
        {
            return ApiProblemFactory.ToResult(
                StatusCodes.Status401Unauthorized,
                "Доступ запрещён",
                "Не удалось авторизовать серверный запрос.",
                "InvalidServiceKey");
        }

        return await next(context);
    }
}
