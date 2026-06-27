using Microsoft.Extensions.DependencyInjection;

namespace SQLModule.Web.Common.Cqrs;

/// <summary>
/// Реализация <see cref="ISender"/>. Резолвит хендлер и behaviors из DI,
/// строит цепочку вызовов без рефлексии (явные generic-параметры на вызывающей стороне).
/// </summary>
internal sealed class Sender(IServiceProvider sp) : ISender
{
    /// <inheritdoc/>
    public Task<TResponse> Send<TRequest, TResponse>(TRequest command, CancellationToken ct = default)
        where TRequest : IRequest<TResponse>
    {
        var handler = sp.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = sp.GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Reverse()
            .ToList();

        RequestHandlerDelegate<TResponse> pipeline = token => handler.Handle(command, token);

        foreach (var behavior in behaviors)
        {
            var captured = pipeline;
            var beh = behavior;
            pipeline = token => beh.Handle(command, captured, token);
        }

        return pipeline(ct);
    }
}
