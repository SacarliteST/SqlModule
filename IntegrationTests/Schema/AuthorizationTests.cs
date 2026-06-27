using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SQLModule.Contracts;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.Schema;

/// <summary>
/// Негативные тесты авторизации: проверяют 401 при отсутствии аутентификации
/// и 403 при несоответствии роли.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class AuthorizationTests : ApiTestBase
{
    public AuthorizationTests(TestApplication testApplication) : base(testApplication)
    {
    }

    [Fact(DisplayName = "Auth: без заголовков → 401 Unauthorized")]
    public async Task NoAuth_AnyEndpoint_Returns401()
    {
        var anonymousClient = App.CreateClient();
        var response = await anonymousClient.GetAsync(ApiRoutes.Schema.MetaTables.ForPagination(0, 10));
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Auth: Student пытается создать схему (ContentAuthor) → 403 Forbidden")]
    public async Task Student_CreateSchema_Returns403()
    {
        AsStudent();
        var response = await HttpClient.PostAsJsonAsync(ApiRoutes.Schema.SchemaBuilder.Collection, new { });
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "Auth: Teacher пытается отправить попытку (Student only) → 403 Forbidden")]
    public async Task Teacher_SubmitAttempt_Returns403()
    {
        AsTeacher();
        var response = await HttpClient.PostAsJsonAsync(ApiRoutes.Training.Attempts.Collection, new { });
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "Auth: Student пытается создать СУБД-словарь (Admin only) → 403 Forbidden")]
    public async Task Student_CreateDbmsDictionary_Returns403()
    {
        AsStudent();
        var response = await HttpClient.PostAsJsonAsync(ApiRoutes.DbmsCatalog.DbmsDictionaries.Collection, new { });
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
