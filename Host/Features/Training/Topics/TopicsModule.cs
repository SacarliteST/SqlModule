using SQLModule.Contracts;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.Topics;

internal static class TopicsModule
{
    internal static IServiceCollection AddTopics(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<CreateTopicCommand, Result<TopicResponse>>, CreateTopicHandler>();
        services.AddScoped<IRequestHandler<GetTopicByIdQuery, Result<TopicResponse>>, GetTopicByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllTopicsQuery, Result<PageResponse<TopicResponse>>>, GetAllTopicsHandler>();
        services.AddScoped<IRequestHandler<UpdateTopicCommand, Result>, UpdateTopicHandler>();
        services.AddScoped<IRequestHandler<DeleteTopicCommand, Result>, DeleteTopicHandler>();
        services.AddScoped<IRequestHandler<MoveTopicCommand, Result>, MoveTopicHandler>();
        return services;
    }
}
