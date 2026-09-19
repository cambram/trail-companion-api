using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace TrailCompanion.Api.Common.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? AppUserId
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;

            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var appUserId)
                ? appUserId
                : null;
        }
    }
}
