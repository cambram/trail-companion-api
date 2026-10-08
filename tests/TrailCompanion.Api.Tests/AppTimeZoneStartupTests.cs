using TrailCompanion.Application.Common.Services;
using TrailCompanion.Infrastructure.Common.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TrailCompanion.Api.Tests;

public class AppTimeZoneStartupTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Nowhere/Land")]
    public void AddInfrastructure_WithAnUnresolvableTimeZone_FailsBeforeTheHostStarts(string? zone)
    {
        var configuration = Configure(zone);

        var exception = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddInfrastructure(configuration));

        Assert.Contains("App:TimeZone", exception.Message);
    }

    [Fact]
    public void AddInfrastructure_WithJohannesburg_RegistersTheZone()
    {
        using var provider = new ServiceCollection()
            .AddInfrastructure(Configure("Africa/Johannesburg"))
            .BuildServiceProvider();

        Assert.Equal("Africa/Johannesburg", provider.GetRequiredService<AppTimeZone>().Id);
    }

    private static IConfiguration Configure(string? zone) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=unused",
                ["App:TimeZone"] = zone
            })
            .Build();
}
