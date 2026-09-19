using System.Security.Cryptography;

namespace TrailCompanion.Infrastructure.Authentication;

internal sealed class PasswordHasher : IPasswordHasher
{
    private const int Iterations = 600_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    private static readonly Lazy<string> DummyHash = new(() => CreateHash(Guid.NewGuid().ToString()));

    public string Hash(string password) => CreateHash(password);

    public bool Verify(string password, string? storedHash)
    {
        if (storedHash is null || !TryParse(storedHash, out var iterations, out var salt, out var expected))
        {
            TryParse(DummyHash.Value, out iterations, out salt, out expected);
            Derive(password, salt, iterations, expected.Length);
            return false;
        }

        var actual = Derive(password, salt, iterations, expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static string CreateHash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Derive(password, salt, Iterations, HashSize);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static byte[] Derive(string password, byte[] salt, int iterations, int length) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, length);

    private static bool TryParse(string storedHash, out int iterations, out byte[] salt, out byte[] hash)
    {
        iterations = 0;
        salt = [];
        hash = [];

        var parts = storedHash.Split('.');

        if (parts.Length != 3 || !int.TryParse(parts[0], out iterations) || iterations <= 0)
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[1]);
            hash = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        return hash.Length > 0;
    }
}
