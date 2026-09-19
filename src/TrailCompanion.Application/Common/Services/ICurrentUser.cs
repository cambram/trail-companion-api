namespace TrailCompanion.Application.Common.Services;

public interface ICurrentUser
{
    Guid? AppUserId { get; }
}
