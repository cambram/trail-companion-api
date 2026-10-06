using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TrailCompanion.Api.Common.Configuration.DependencyInjection;

namespace TrailCompanion.Api.Tests;

public class RateLimitingConfigurationTests
{
    [Fact]
    public void PartitionKey_GroupsIpv6ByPrefix64()
    {
        var first = RateLimitingConfiguration.PartitionKey(IPAddress.Parse("2001:db8:1:2::1"));
        var sameNetwork = RateLimitingConfiguration.PartitionKey(IPAddress.Parse("2001:db8:1:2:ffff::9"));
        var otherNetwork = RateLimitingConfiguration.PartitionKey(IPAddress.Parse("2001:db8:1:3::1"));

        Assert.Equal(first, sameNetwork);
        Assert.NotEqual(first, otherNetwork);
    }

    [Theory]
    [InlineData("203.0.113.7", "203.0.113.7")]
    [InlineData("::ffff:203.0.113.7", "203.0.113.7")]
    public void PartitionKey_KeepsIpv4AddressesWhole(string address, string expected)
    {
        Assert.Equal(expected, RateLimitingConfiguration.PartitionKey(IPAddress.Parse(address)));
    }

    [Fact]
    public void PartitionKey_WithoutAnAddress_UsesOneSharedKey()
    {
        Assert.Equal("unknown", RateLimitingConfiguration.PartitionKey(null));
    }

    [Fact]
    public void GlobalLimiter_CapsConcurrentPasswordHashingOnly()
    {
        var limiter = new ServiceCollection()
            .AddApiRateLimiting()
            .BuildServiceProvider()
            .GetRequiredService<IOptions<RateLimiterOptions>>()
            .Value.GlobalLimiter!;
        var hashing = ContextFor(new RateLimitingConfiguration.PasswordHashingEndpoint());

        var held = Enumerable.Range(0, RateLimitingConfiguration.PasswordHashingConcurrency)
            .Select(_ => limiter.AttemptAcquire(hashing))
            .ToList();

        Assert.All(held, lease => Assert.True(lease.IsAcquired));
        Assert.False(limiter.AttemptAcquire(hashing).IsAcquired);
        Assert.True(limiter.AttemptAcquire(ContextFor()).IsAcquired);
    }

    private static HttpContext ContextFor(params object[] metadata)
    {
        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(metadata), "test"));
        return context;
    }
}
