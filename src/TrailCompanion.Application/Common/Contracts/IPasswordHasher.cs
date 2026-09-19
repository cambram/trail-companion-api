namespace TrailCompanion.Application.Common.Contracts;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string? storedHash);
}
