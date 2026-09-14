using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.IntegrationTests.infrastructure;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Auth;

namespace SQLModule.IntegrationTests.Schema;

[Collection(IntegrationTestCollection.Name)]
public sealed class ContentAuthorPolicyAuditTests(TestApplication app)
{
    [Fact(DisplayName = "TAI-008: все endpoint-файлы authoring-контура явно используют ContentAuthor")]
    public void AuthoringEndpoints_ExplicitlyUseContentAuthorPolicy()
    {
        var webRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Web"));
        var authoringFiles = new List<string>();
        foreach (var relativeDirectory in new[]
                 {
                     "Features/Schema",
                     "Features/Training/Topics",
                     "Features/Training/SqlTasks",
                     "Features/Training/SqlQueries"
                 })
        {
            authoringFiles.AddRange(Directory.GetFiles(
                Path.Combine(webRoot, relativeDirectory),
                "*Endpoint.cs",
                SearchOption.AllDirectories));
        }

        authoringFiles.AddRange(Directory.GetFiles(
            Path.Combine(webRoot, "Features/DbmsCatalog"),
            "Get*Endpoint.cs",
            SearchOption.AllDirectories));
        authoringFiles.AddRange(Directory.GetFiles(
            Path.Combine(webRoot, "Features/Training/Attempts"),
            "Get*Endpoint.cs",
            SearchOption.AllDirectories));

        authoringFiles.ShouldNotBeEmpty();
        foreach (var file in authoringFiles)
        {
            var source = File.ReadAllText(file);
            source.ShouldContain(
                ".RequireAuthorization(Policies.ContentAuthor)",
                customMessage: $"Authoring endpoint {Path.GetRelativePath(webRoot, file)} не защищён ContentAuthor.");
            source.ShouldNotContain("session_id", Case.Insensitive);
            source.ShouldNotContain("ModuleSessionId", Case.Insensitive);
        }
    }

    [Fact(DisplayName = "TAI-008: ContentAuthor разрешает только Teacher/Admin и не требует session_id")]
    public async Task ContentAuthorPolicy_RequiresOnlyAuthorRoleAndAuthentication()
    {
        var provider = app.Services.GetRequiredService<IAuthorizationPolicyProvider>();
        var policy = await provider.GetPolicyAsync(Policies.ContentAuthor);

        policy.ShouldNotBeNull();
        policy.Requirements.ShouldHaveSingleItem();
        var roles = policy.Requirements.OfType<RolesAuthorizationRequirement>().ShouldHaveSingleItem();
        roles.AllowedRoles.Order().ShouldBe(new[] { Roles.Admin, Roles.Teacher }.Order());
        policy.Requirements.ShouldNotContain(requirement =>
            requirement.GetType().Name.Contains("Session", StringComparison.OrdinalIgnoreCase));
    }
}
