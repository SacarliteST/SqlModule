using System.Net;
using System.Net.Http.Json;
using EducationSPD.Contracts.Recommendations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using SQLModule.EducationSpd.Client;

namespace SQLModule.IntegrationTests.SpdIntegration;

public sealed class EducationSpdRecommendationClientTests
{
    [Fact(DisplayName = "EducationSPD client: передаёт service key и повторяет временный 5xx в общем timeout")]
    public async Task RankAsync_TransientFailure_RetriesWithSameContract()
    {
        var taskId = Guid.NewGuid();
        var handler = new SequenceHandler(
            _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new RankRecommendationsResponse(
                    1,
                    [new RankedCandidateResponse(taskId, 1, 0.75m, [])]))
            });
        var client = new EducationSpdRecommendationClient(
            new HttpClient(handler),
            Options.Create(new EducationSpdRecommendationClientOptions
            {
                Enabled = true,
                BaseUrl = "https://spd.test",
                ServiceKey = "test-service-key",
                RequestTimeoutSeconds = 2
            }),
            NullLogger<EducationSpdRecommendationClient>.Instance);
        var request = new RankRecommendationsRequest(
            Guid.NewGuid(),
            "sql",
            [new RecommendationCandidateRequest(
                taskId,
                2,
                [new RecommendationSkillRequest("select", RecommendationSkillSource.ValidationRule)])]);

        var result = await client.RankAsync(request);

        result.IsSuccess.ShouldBeTrue();
        handler.Calls.ShouldBe(2);
        handler.ServiceKeys.ShouldAllBe(value => value == "test-service-key");
        handler.CancellationWasEnabled.ShouldBeTrue();
    }

    private sealed class SequenceHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responses)
        : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public List<string?> ServiceKeys { get; } = [];
        public bool CancellationWasEnabled { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ServiceKeys.Add(request.Headers.GetValues("X-Service-Key").SingleOrDefault());
            CancellationWasEnabled |= cancellationToken.CanBeCanceled;
            var response = responses[Math.Min(Calls, responses.Length - 1)](request);
            Calls++;
            return Task.FromResult(response);
        }
    }
}
