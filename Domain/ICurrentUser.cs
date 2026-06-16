namespace SQLModule.Domain;

public interface ICurrentUser
{
    Guid? UserId { get; }
}
