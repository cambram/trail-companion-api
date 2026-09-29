using System.Security.Cryptography;
using System.Text;
using TrailCompanion.Application.Common.Configuration;
using Microsoft.Extensions.Options;

namespace TrailCompanion.Infrastructure.Authentication;

internal sealed class RefreshTokenIssuer : IRefreshTokenIssuer
{
    private const int TokenBytes = 32;

    private readonly JwtOptions _options;

    public RefreshTokenIssuer(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public IssuedRefreshToken Issue()
    {
        var value = Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));

        return new IssuedRefreshToken(value, Hash(value), DateTime.UtcNow.AddDays(_options.RefreshTokenLifetimeDays));
    }

    public string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
