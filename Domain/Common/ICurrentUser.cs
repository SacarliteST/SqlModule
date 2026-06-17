namespace SQLModule.Domain.Common;

public interface ICurrentUser
{
    Guid? UserId { get; }
}
