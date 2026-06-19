using FluentValidation;

namespace SQLModule.Host.Common;

/// <summary>
/// Endpoint-фильтр, запускающий FluentValidation-валидатор для параметра типа <typeparamref name="TRequest"/>.
/// Если валидатор не зарегистрирован или параметр не найден — пропускает запрос дальше.
/// При неуспешной валидации возвращает 400 ValidationProblem со словарём ошибок.
/// </summary>
/// <typeparam name="TRequest">Тип DTO запроса, для которого ищется <see cref="IValidator{T}"/>.</typeparam>
internal sealed class ValidationFilter<TRequest>(IServiceProvider sp) : IEndpointFilter
    where TRequest : class
{
    /// <inheritdoc/>
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
            return TypedResults.ValidationProblem(result.ToDictionary());
        }

        return await next(context);
    }
}
