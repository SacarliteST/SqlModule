using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;
using SQLModule.IntegrationTests.infrastructure;
using SQLModule.Web.Common.Isolated;

namespace SQLModule.IntegrationTests.Training;

[Collection(IntegrationTestCollection.Name)]
public sealed class CourseDataSeederTests(TestApplication app)
{
    private static readonly Guid[] TopicIds = Enumerable.Range(1, 5)
        .Select(value => new Guid($"93000000-0000-0000-0000-{value:D12}"))
        .ToArray();
    private static readonly Guid[] TaskIds = Enumerable.Range(1, 20)
        .Select(value => new Guid($"94000000-0000-0000-0000-{value:D12}"))
        .ToArray();

    [Fact(DisplayName = "CourseSeed без пути блокирует запуск через IOptions validation")]
    public void CourseSeed_EnabledWithoutFilePath_RejectsStartup()
    {
        using var configured = app.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration(configuration =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CourseSeed:Enabled"] = "true",
                    ["CourseSeed:FilePath"] = " "
                }));
        });

        Should.Throw<OptionsValidationException>(() => _ = configured.Services);
    }

    [Fact(DisplayName = "CourseSeed импортируется через IOptions и повторно не создаёт дубликаты")]
    public async Task CourseSeed_ImportsFromConfiguredFileAndIsIdempotent()
    {
        var courseFile = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../IntegrationTests/TestData/basic-sql-course.json"));
        var userTopicId = Guid.NewGuid();
        var userTopicName = $"Пользовательская тема {userTopicId:N}";
        using (var initialScope = app.Services.CreateScope())
        {
            var initialDb = initialScope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await initialDb.SqlTasks.AnyAsync(value =>
                value.Id == new Guid("94000000-0000-0000-0000-000000000001"))).ShouldBeFalse();
            initialDb.Topics.Add(SQLModule.Domain.Training.Topic.Create(
                userTopicName,
                null,
                userTopicId));
            await initialDb.SaveChangesAsync();
        }

        using var configured = app.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration(configuration =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CourseSeed:Enabled"] = "true",
                    ["CourseSeed:FilePath"] = courseFile
                }));
        });
        _ = configured.Services;

        var settings = configured.Services.GetRequiredService<IOptions<CourseSeedOptions>>().Value;
        settings.Enabled.ShouldBeTrue();
        settings.FilePath.ShouldBe(courseFile);

        using var scope = configured.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new CourseDataSeeder(
            db,
            Options.Create(settings),
            scope.ServiceProvider.GetRequiredService<IHostEnvironment>());
        var countsAfterStartup = await ReadCountsAsync(db);

        await seeder.SeedAsync();
        db.ChangeTracker.Clear();

        (await ReadCountsAsync(db)).ShouldBe(countsAfterStartup);
        countsAfterStartup.ShouldBe([1, 5, 1, 4, 18, 20, 92, 4, 3, 5, 20, 20]);
        (await db.Topics.AsNoTracking().SingleAsync(value => value.Id == userTopicId))
            .TopicName.ShouldBe(userTopicName);

        var firstTask = await db.SqlTasks.AsNoTracking().SingleAsync(value =>
            value.Id == new Guid("94000000-0000-0000-0000-000000000001"));
        firstTask.PublicationStatus.ShouldBe(PublicationStatus.Published);
        firstTask.SqlQueryId.ShouldBe(new Guid("95000000-0000-0000-0000-000000000001"));
        var firstQuery = await db.SqlQueries.AsNoTracking().SingleAsync(value =>
            value.Id == firstTask.SqlQueryId);
        firstQuery.QueryText.ShouldBe("SELECT id, full_name FROM customers ORDER BY id;");
        firstQuery.ExpectedResult.ShouldNotBeNull();
        firstQuery.ExpectedResult.ShouldContain("Анна");
    }

    private static async Task<int[]> ReadCountsAsync(AppDbContext db) =>
    [
        await db.DbmsDictionaries.CountAsync(value =>
            value.Id == new Guid("91000000-0000-0000-0000-000000000001")),
        await db.PhysicalTypes.CountAsync(value =>
            value.DbmsId == new Guid("91000000-0000-0000-0000-000000000001")),
        await db.TargetDbs.CountAsync(value =>
            value.Id == new Guid("92000000-0000-0000-0000-000000000001")),
        await db.MetaTables.CountAsync(value =>
            value.TargetDbId == new Guid("92000000-0000-0000-0000-000000000001")),
        await db.MetaAttributes.CountAsync(value =>
            value.MetaTable.TargetDbId == new Guid("92000000-0000-0000-0000-000000000001")),
        await db.DataRecords.CountAsync(value =>
            value.MetaTable.TargetDbId == new Guid("92000000-0000-0000-0000-000000000001")),
        await db.CellValues.CountAsync(value =>
            value.DataRecord.MetaTable.TargetDbId == new Guid("92000000-0000-0000-0000-000000000001")),
        await db.AttributeParameterValues.CountAsync(value =>
            value.MetaAttribute.MetaTable.TargetDbId == new Guid("92000000-0000-0000-0000-000000000001")),
        await db.MetaRelationships.CountAsync(value =>
            value.SourceAttribute.MetaTable.TargetDbId == new Guid("92000000-0000-0000-0000-000000000001")),
        await db.Topics.CountAsync(value => TopicIds.Contains(value.Id)),
        await db.SqlQueries.CountAsync(value =>
            value.TargetDbId == new Guid("92000000-0000-0000-0000-000000000001")),
        await db.SqlTasks.CountAsync(value => TaskIds.Contains(value.Id))
    ];
}
