using TrailCompanion.Api.Common.Configuration.Extensions;
using Npgsql;

namespace TrailCompanion.Api.Tests;

public class EnvironmentAliasExtensionsTests
{
    [Fact]
    public void ToNpgsqlConnectionString_TranslatesNeonUrl()
    {
        const string neonUrl =
            "postgresql://trail_owner:np%40ss%3Bword@ep-cool-bird-123.eu-central-1.aws.neon.tech/trailcompanion" +
            "?sslmode=require&channel_binding=require";

        var builder = new NpgsqlConnectionStringBuilder(
            EnvironmentAliasExtensions.ToNpgsqlConnectionString(neonUrl));

        Assert.Equal("ep-cool-bird-123.eu-central-1.aws.neon.tech", builder.Host);
        Assert.Equal(5432, builder.Port);
        Assert.Equal("trailcompanion", builder.Database);
        Assert.Equal("trail_owner", builder.Username);
        Assert.Equal("np@ss;word", builder.Password);
        Assert.Equal(SslMode.Require, builder.SslMode);
        Assert.True(builder.NoResetOnClose);
    }

    [Fact]
    public void ToNpgsqlConnectionString_KeepsExplicitPort()
    {
        var builder = new NpgsqlConnectionStringBuilder(
            EnvironmentAliasExtensions.ToNpgsqlConnectionString(
                "postgres://user:secret@db.example.com:6543/trailcompanion"));

        Assert.Equal(6543, builder.Port);
    }

    [Theory]
    [InlineData("?sslmode=require", SslMode.Require)]
    [InlineData("?sslmode=verify-full", SslMode.VerifyFull)]
    [InlineData("?sslmode=disable", SslMode.Disable)]
    [InlineData("", SslMode.Require)]
    public void ToNpgsqlConnectionString_HonoursSslModeFromUrl(string query, SslMode expected)
    {
        var builder = new NpgsqlConnectionStringBuilder(
            EnvironmentAliasExtensions.ToNpgsqlConnectionString(
                $"postgresql://user:secret@db.example.com/trailcompanion{query}"));

        Assert.Equal(expected, builder.SslMode);
    }

    [Fact]
    public void ToNpgsqlConnectionString_PassesThroughKeyValueStrings()
    {
        const string keyValue = "Host=localhost;Port=5432;Database=trailcompanion;Username=postgres;Password=postgres";

        Assert.Equal(keyValue, EnvironmentAliasExtensions.ToNpgsqlConnectionString(keyValue));
    }
}
