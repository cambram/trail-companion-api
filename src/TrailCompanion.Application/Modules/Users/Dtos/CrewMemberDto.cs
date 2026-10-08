using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Modules.Users.Dtos;

public record CrewMemberDto(Guid AppUserId, string DisplayName, DateTime JoinedAtUtc)
{
    public static CrewMemberDto FromEntity(RaceCrewMembers member, AppUser user)
    {
        var fullName = string.Join(' ', new[] { user.FirstName, user.LastName }.Where(n => !string.IsNullOrWhiteSpace(n)));

        return new(member.AppUserId, fullName.Length > 0 ? fullName : user.Username, member.CreatedAtUtc);
    }
}
