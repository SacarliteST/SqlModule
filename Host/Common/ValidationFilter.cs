using FluentValidation;

namespace TemplateApi.Host.Common;

internal sealed class ValidationFilter<TRequest>(IServiceProvider sp) : IEndpointFilter
    where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = sp.GetService<IValidator<TRequest>>();
        if (validator is null)
        {
            return await next(context);
        }

        var argument = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (argument is null)
        {
            return await next(context);
        }

        var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        if (!result.IsValid)
        {
            return Results.ValidationProblem(result.ToDictionary());
        }

        return await next(context);
    }
}
