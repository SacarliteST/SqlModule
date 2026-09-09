using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Shouldly;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Host;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.Training;

[Collection(DockerTestCollection.Name)]
public sealed class CourseSeedDockerTests(DockerTestApplication app)
{
    private static readonly Guid FinalTaskId = new("94000000-0000-0000-0000-000000000020");
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact(DisplayName = "Импортированный курс выполняется в реальном PostgreSQL sandbox")]
    [Trait("Category", DockerTestCollection.Category)]
    public async Task CourseSeed_FinalCteTask_IsCorrectInRealSandbox()
    {
        var courseFile = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../IntegrationTests/TestData/basic-sql-course.json"));
        using var configured = app.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration(configuration =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CourseSeed:Enabled"] = "true",
                    ["CourseSeed:FilePath"] = courseFile,
                    ["UseFakeSandbox"] = "false"
                }));
        });
        using var client = configured.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Training.Attempts.Collection)
        {
            Content = JsonContent.Create(new SubmitAttemptRequest(
                FinalTaskId,
                "WITH order_totals AS (SELECT order_id, SUM(quantity * unit_price) AS order_total " +
                "FROM order_items GROUP BY order_id) SELECT order_id, order_total FROM order_totals " +
                "WHERE order_total > 10000 ORDER BY order_total DESC;"))
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
        request.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString("D"));
        request.Headers.Add("X-Test-Roles", "Student");

        using var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<SubmitAttemptResponse>(JsonOptions);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        result.ShouldNotBeNull();
        result.IsCorrect.ShouldBeTrue(JsonSerializer.Serialize(result, JsonOptions));
        result.ActualColumns.ShouldBe(["order_id", "order_total"]);
        result.ActualRows.ShouldBe([["1", "93000.00"], ["2", "25000.00"]]);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
