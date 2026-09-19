using System.Security.Claims;
using TrailCompanion.Application.Common.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace TrailCompanion.Infrastructure.Authentication;

internal sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _signingCredentials;
    private readonly JsonWebTokenHandler _handler = new();

    public JwtAccessTokenIssuer(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(_options.GetSigningKeyBytes()),
            SecurityAlgorithms.HmacSha256);
    }

    public AccessToken Issue(Guid appUserId)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, appUserId.ToString())]),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expiresAtUtc,
            SigningCredentials = _signingCredentials
        });

        return new AccessToken(token, expiresAtUtc);
    }
}
