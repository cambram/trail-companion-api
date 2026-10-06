using System.Net;
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
}
