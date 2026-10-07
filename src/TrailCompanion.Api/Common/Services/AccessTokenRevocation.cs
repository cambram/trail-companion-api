using System.Security.Claims;
using TrailCompanion.Domain.Modules.Users.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;

namespace TrailCompanion.Api.Common.Services;

internal static class AccessTokenRevocation
{
    internal static async Task RejectStaleAsync(TokenValidatedContext context)
    {
        if (!Guid.TryParse(context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var appUserId))
        {
            context.Fail("Token has no subject.");
            return;
        }

        var appUser = await context.HttpContext.RequestServices
            .GetRequiredService<IAppUserRepository>()
            .GetByIdAsync(appUserId, context.HttpContext.RequestAborted);

        if (appUser is null)
        {
            context.Fail("Account no longer exists.");
            return;
        }

        if (context.SecurityToken is not JsonWebToken token)
        {
            context.Fail("Unsupported token type.");
            return;
        }

        if (appUser.PasswordChangedAtUtc is { } changedAtUtc
            && token.IssuedAt < TruncateToSeconds(changedAtUtc))
        {
            context.Fail("Token predates the last password change.");
        }
    }

    private static DateTime TruncateToSeconds(DateTime value) =>
        value.AddTicks(-(value.Ticks % TimeSpan.TicksPerSecond));
}
