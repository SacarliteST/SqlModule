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
        anonymousClient.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
        var response = await anonymousClient.GetAsync(ApiRoutes.Schema.MetaTables.ForPagination(0, 10));
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Health: доступен без авторизации и сообщает healthy при выключенном пуле")]
    public async Task NoAuth_Health_ReturnsOkWhenPoolIsDisabled()
    {
        var anonymousClient = App.CreateClient();
        anonymousClient.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");

        var response = await anonymousClient.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
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

    [Fact(DisplayName = "Auth: Student запрашивает teacher details → 403 Forbidden")]
    public async Task Student_GetTeacherTaskDetails_Returns403()
    {
        AsStudent();
        var response = await HttpClient.GetAsync(ApiRoutes.Training.TeacherTasks.ForDetails(Guid.NewGuid()));
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "Lookup values: Teacher/Admin разрешены, Student получает 403, anonymous — 401")]
    public async Task LookupValues_UsesContentAuthorPolicy()
    {
        var path = $"{ApiRoutes.Schema.TargetDbs.ForLookupValues(Guid.NewGuid(), Guid.NewGuid())}" +
                   $"?valueColumnId={Guid.NewGuid()}";

        AsTeacher();
        (await HttpClient.GetAsync(path)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        AsAdmin();
        (await HttpClient.GetAsync(path)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        AsStudent();
        (await HttpClient.GetAsync(path)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var anonymousClient = App.CreateClient();
        anonymousClient.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
        (await anonymousClient.GetAsync(path)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Lookup values: некорректные query-параметры возвращают 422")]
    public async Task LookupValues_InvalidQuery_Returns422()
    {
        AsTeacher();
        var path = ApiRoutes.Schema.TargetDbs.ForLookupValues(Guid.NewGuid(), Guid.NewGuid());

        (await HttpClient.GetAsync(path)).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await HttpClient.GetAsync($"{path}?valueColumnId={Guid.NewGuid()}&offset=-1"))
            .StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await HttpClient.GetAsync($"{path}?valueColumnId={Guid.NewGuid()}&limit=101"))
            .StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await HttpClient.GetAsync($"{path}?valueColumnId={Guid.NewGuid()}&search={new string('x', 201)}"))
            .StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }
}
