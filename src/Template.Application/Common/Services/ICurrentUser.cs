namespace Template.Application.Common.Services;

public interface ICurrentUser
{
    Guid? AppUserId { get; }
}
