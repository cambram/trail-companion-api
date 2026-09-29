using System.Text;

namespace TrailCompanion.Application.Common.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public const int MinimumKeyBytes = 32;

    public const string InvalidKeyMessage =
        $"'{SectionName}:Key' must be at least 32 bytes. " +
        "When deployed, set the JWT_KEY environment variable to the output of 'openssl rand -base64 48'. " +
        $"Locally, use user-secrets: dotnet user-secrets set \"{SectionName}:Key\" \"$(openssl rand -base64 48)\" --project src/TrailCompanion.Api";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    public byte[] GetSigningKeyBytes() => Encoding.UTF8.GetBytes(Key);

    public bool HasUsableKey() => GetSigningKeyBytes().Length >= MinimumKeyBytes;
}
