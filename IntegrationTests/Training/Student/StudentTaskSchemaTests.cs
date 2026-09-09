using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Student;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.Training.Student;

[Collection(IntegrationTestCollection.Name)]
public sealed class StudentTaskSchemaTests : ApiTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public StudentTaskSchemaTests(TestApplication testApplication) : base(testApplication)
    {
    }

    private async Task<(Guid TaskId, Guid DraftId, Guid ArchivedId, Guid TargetDbId, Guid FirstRelationshipId)> SeedSchemaAsync()
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var dbms = Domain.DbmsCatalog.DbmsDictionary.Create(
            "PostgreSql", "postgres", "postgres:latest", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
            "training", "user", "password");
        var type = Domain.DbmsCatalog.PhysicalType.Create(dbms.Id, "integer");
        var targetDb = TargetDb.Create(dbms.Id, "training", "Учебная база", false);
        var topic = Domain.Training.Topic.Create("Student schema");
        var query = Domain.Training.SqlQuery.Create("SELECT secret_reference FROM orders", false, false, targetDb.Id);
        var task = Domain.Training.SqlTask.Create(
            topic.Id, query.Id, "Published schema", "Task", 1,
            publicationStatus: PublicationStatus.Published);
        var draftQuery = Domain.Training.SqlQuery.Create("SELECT draft_secret", false, false, targetDb.Id);
        var draft = Domain.Training.SqlTask.Create(
            topic.Id, draftQuery.Id, "Draft schema", "Task", 1,
            publicationStatus: PublicationStatus.Draft);
        var archivedQuery = Domain.Training.SqlQuery.Create("SELECT archived_secret", false, false, targetDb.Id);
        var archived = Domain.Training.SqlTask.Create(
            topic.Id, archivedQuery.Id, "Archived schema", "Task", 1,
            publicationStatus: PublicationStatus.Archived);

        var customers = MetaTable.Create(targetDb.Id, "customers", null, sortOrder: 0);
        var orders = MetaTable.Create(targetDb.Id, "orders", "Заказы клиентов", sortOrder: 1);
        var customerId = MetaAttribute.Create(customers.Id, type.Id, "id", true, true, 0);
        var customerRegion = MetaAttribute.Create(customers.Id, type.Id, "region_id", true, true, 1);
        var orderCustomer = MetaAttribute.Create(orders.Id, type.Id, "customer_id", false, true, 0);
        var orderRegion = MetaAttribute.Create(orders.Id, type.Id, "region_id", false, true, 1);
        var firstPair = MetaRelationship.Create(
            "fk_orders_customers", orderCustomer.Id, customerId.Id, null, null);
        var secondPair = MetaRelationship.Create(
            "fk_orders_customers", orderRegion.Id, customerRegion.Id, null, null);

        db.AddRange(dbms, type, targetDb, topic, query, task, draftQuery, draft, archivedQuery, archived,
            customers, orders, customerId, customerRegion, orderCustomer, orderRegion, firstPair, secondPair);
        await db.SaveChangesAsync();
        return (task.Id, draft.Id, archived.Id, targetDb.Id,
            firstPair.Id.CompareTo(secondPair.Id) < 0 ? firstPair.Id : secondPair.Id);
    }

    [Fact(DisplayName = "Student schema → возвращает безопасную стабильную схему и составной FK")]
    public async Task PublishedTask_ReturnsSafeStableCompositeSchema()
    {
        var fixture = await SeedSchemaAsync();
        AsStudent();
        var uri = ApiRoutes.Training.Student.ForTaskSchema(fixture.TaskId);

        using var firstResponse = await HttpClient.GetAsync(uri);
        using var secondResponse = await HttpClient.GetAsync(uri);
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstJson = await firstResponse.Content.ReadAsStringAsync();
        var secondJson = await secondResponse.Content.ReadAsStringAsync();
        var schema = JsonSerializer.Deserialize<StudentTaskSchemaResponse>(firstJson, JsonOptions)!;

        schema.DatabaseName.ShouldBe("training");
        schema.Dbms.ShouldBe("PostgreSql");
        schema.Tables.Count.ShouldBe(2);
        schema.Tables[0].Name.ShouldBe("customers");
        schema.Tables[1].Name.ShouldBe("orders");
        schema.Tables[1].Description.ShouldBe("Заказы клиентов");
        schema.Tables[1].Columns[0].DataType.ShouldBe("integer");
        schema.Tables[1].Columns[0].IsNullable.ShouldBeFalse();
        var foreignKey = schema.ForeignKeys.ShouldHaveSingleItem();
        foreignKey.Id.ShouldBe(fixture.FirstRelationshipId);
        foreignKey.ColumnPairs.Count.ShouldBe(2);
        secondJson.ShouldBe(firstJson);

        firstJson.ShouldNotContain("secret_reference");
        firstJson.ShouldNotContain("expectedResult", Case.Insensitive);
        firstJson.ShouldNotContain("queryText", Case.Insensitive);
        firstJson.ShouldNotContain("connectionString", Case.Insensitive);
        firstJson.ShouldNotContain("dataRecords", Case.Insensitive);
        firstJson.ShouldNotContain("cellValues", Case.Insensitive);
        firstJson.ShouldNotContain("ddl", Case.Insensitive);
    }

    [Fact(DisplayName = "Student schema → Draft и Archived скрываются через 404")]
    public async Task ClosedTasks_ReturnNotFound()
    {
        var fixture = await SeedSchemaAsync();
        AsStudent();

        (await HttpClient.GetAsync(ApiRoutes.Training.Student.ForTaskSchema(fixture.DraftId)))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await HttpClient.GetAsync(ApiRoutes.Training.Student.ForTaskSchema(fixture.ArchivedId)))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Student schema → endpoint требует Student role")]
    public async Task Endpoint_EnforcesStudentPolicy()
    {
        var uri = ApiRoutes.Training.Student.ForTaskSchema(Guid.NewGuid());
        HttpClient.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
        (await HttpClient.GetAsync(uri)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        HttpClient.DefaultRequestHeaders.Remove("X-Test-Anonymous");

        AsTeacher();
        (await HttpClient.GetAsync(uri)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        AsAdmin();
        (await HttpClient.GetAsync(uri)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "Student schema → пустая схема возвращается как 200 с пустыми массивами")]
    public async Task EmptySchema_ReturnsOkWithEmptyArrays()
    {
        var fixture = await SeedSchemaAsync();
        using (var scope = App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tableIds = await db.MetaTables.Where(table => table.TargetDbId == fixture.TargetDbId)
                .Select(table => table.Id).ToListAsync();
            var attributeIds = await db.MetaAttributes.Where(column => tableIds.Contains(column.MetaTableId))
                .Select(column => column.Id).ToListAsync();
            db.MetaRelationships.RemoveRange(db.MetaRelationships.Where(relationship =>
                attributeIds.Contains(relationship.SourceAttributeId) ||
                attributeIds.Contains(relationship.TargetAttributeId)));
            db.MetaAttributes.RemoveRange(db.MetaAttributes.Where(column => attributeIds.Contains(column.Id)));
            db.MetaTables.RemoveRange(db.MetaTables.Where(table => tableIds.Contains(table.Id)));
            await db.SaveChangesAsync();
        }
        AsStudent();

        var schema = await HttpClient.GetFromJsonAsync<StudentTaskSchemaResponse>(
            ApiRoutes.Training.Student.ForTaskSchema(fixture.TaskId), JsonOptions);
        schema!.Tables.ShouldBeEmpty();
        schema.ForeignKeys.ShouldBeEmpty();
    }
}
