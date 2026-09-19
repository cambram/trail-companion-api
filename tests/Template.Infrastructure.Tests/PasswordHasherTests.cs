using Template.Infrastructure.Authentication;

namespace Template.Infrastructure.Tests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Verify_WithTheOriginalPassword_ReturnsTrue()
    {
        var stored = _hasher.Hash("correct horse battery");

        Assert.True(_hasher.Verify("correct horse battery", stored));
    }

    [Fact]
    public void Verify_WithAWrongPassword_ReturnsFalse()
    {
        var stored = _hasher.Hash("correct horse battery");

        Assert.False(_hasher.Verify("wrong horse battery", stored));
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentSaltedHashes()
    {
        Assert.NotEqual(_hasher.Hash("same password"), _hasher.Hash("same password"));
    }

    [Fact]
    public void Hash_StoresIterationsSaltAndHash()
    {
        var parts = _hasher.Hash("correct horse battery").Split('.');

        Assert.Equal(3, parts.Length);
        Assert.Equal("600000", parts[0]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not.a-valid.hash!")]
    [InlineData("0.AAAA.AAAA")]
    public void Verify_WithAMissingOrMalformedHash_ReturnsFalse(string? stored)
    {
        Assert.False(_hasher.Verify("anything", stored));
    }
}
