namespace SQLModule.IntegrationTests.infrastructure;

/// <summary>Контекст текущего тест-пользователя — читается <see cref="TestAuthMessageFilter"/>.</summary>
public sealed class TestUserContext
{
    public string UserId { get; set; } = Guid.NewGuid().ToString();
    public string Roles { get; set; } = "Teacher";
    public string DisplayName { get; set; } = "Test User";
}
