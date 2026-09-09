namespace SQLModule.Domain.Common;

public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? ModuleSessionId { get; }
    string? DisplayName { get; }
    string? Email { get; }
}
