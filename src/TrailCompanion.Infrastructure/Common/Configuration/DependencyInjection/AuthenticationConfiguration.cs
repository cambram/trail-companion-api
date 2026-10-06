using TrailCompanion.Application.Common.Configuration;
using TrailCompanion.Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TrailCompanion.Infrastructure.Common.Configuration.DependencyInjection;

internal static class AuthenticationConfiguration
{
    internal static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => options.HasUsableKey(), JwtOptions.InvalidKeyMessage)
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddSingleton<IRefreshTokenIssuer, RefreshTokenIssuer>();

        services.AddSingleton<IBreachedPasswordChecker>(provider => new PwnedPasswordsChecker(
            new HttpClient
            {
                BaseAddress = new Uri("https://api.pwnedpasswords.com/"),
                Timeout = TimeSpan.FromSeconds(3)
            },
            provider.GetRequiredService<ILogger<PwnedPasswordsChecker>>()));

        return services;
    }
}
