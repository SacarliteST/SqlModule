using System.Text.Json;
using Shouldly;
using SQLModule.Web.Features.ModuleIntegration;

namespace SQLModule.UnitTests.Common;

public sealed class ModuleIntegrationTransportContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact(DisplayName = "Kafka-событие содержит только согласованную оболочку и безопасный payload")]
    public void PracticeEvent_SerializesExactSafeContractInCamelCase()
    {
        var sessionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        const string sessionKey = "transport-only-session-key";
        var message = new PracticeEventMessage(
            sessionId,
            sessionKey,
            eventId,
            "sql_submit",
            DateTimeOffset.Parse("2026-09-06T10:15:30Z"),
            new PracticeEventPayload(
                "SELECT 1",
                "SUCCESS",
                25,
                120,
                false,
                "ValueMismatch"));

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(message, JsonOptions));
        var root = document.RootElement;
        root.EnumerateObject().Select(property => property.Name).ShouldBe(
            ["sessionId", "sessionKey", "eventId", "kind", "occurredAt", "payload"],
            ignoreOrder: true);
        root.GetProperty("sessionId").GetGuid().ShouldBe(sessionId);
        root.GetProperty("sessionKey").GetString().ShouldBe(sessionKey);
        root.GetProperty("eventId").GetGuid().ShouldBe(eventId);

        var payload = root.GetProperty("payload");
        payload.EnumerateObject().Select(property => property.Name).ShouldBe(
            ["submittedSql", "status", "rowCount", "durationMs", "isCorrect", "reason"],
            ignoreOrder: true);
        var payloadJson = payload.GetRawText();
        payloadJson.ShouldNotContain(sessionKey);
        payloadJson.ShouldNotContain("attemptId");
        payloadJson.ShouldNotContain("moduleSessionId");
        payloadJson.ShouldNotContain("mutationReceipt");
        payloadJson.ShouldNotContain("connectionString");
        payloadJson.ShouldNotContain("accessToken");
        payloadJson.ShouldNotContain("refreshToken");
        payloadJson.ShouldNotContain("stackTrace");
    }

    [Fact(DisplayName = "CompletionData не содержит секрет сессии и транспортные идентификаторы")]
    public void CompletionRequest_KeepsSessionKeyOutsideCompletionData()
    {
        const string sessionKey = "completion-transport-session-key";
        var correctAttemptId = Guid.NewGuid();
        var message = new PracticeCompletionRequest(
            sessionKey,
            100,
            new PracticeCompletionData(3, correctAttemptId),
            DateTimeOffset.Parse("2026-09-06T10:40:00Z"));

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(message, JsonOptions));
        var root = document.RootElement;
        root.EnumerateObject().Select(property => property.Name).ShouldBe(
            ["sessionKey", "grade", "completionData", "completedAt"],
            ignoreOrder: true);
        root.GetProperty("sessionKey").GetString().ShouldBe(sessionKey);
        var completionData = root.GetProperty("completionData");
        completionData.EnumerateObject().Select(property => property.Name).ShouldBe(
            ["totalAttempts", "correctAttemptId"],
            ignoreOrder: true);
        completionData.GetRawText().ShouldNotContain(sessionKey);
        completionData.GetRawText().ShouldNotContain("moduleSessionId");
        completionData.GetRawText().ShouldNotContain("eventId");
    }
}
