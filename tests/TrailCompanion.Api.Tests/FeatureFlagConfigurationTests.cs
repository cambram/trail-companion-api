using TrailCompanion.Application.Common.Configuration.Extensions;
using TrailCompanion.Application.Common.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace TrailCompanion.Api.Tests;

public class FeatureFlagConfigurationTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData(" true ", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("0", false)]
    [InlineData("1", false)]
    [InlineData("no", false)]
    [InlineData("flase", false)]
    public async Task RegistrationEnabled_FollowsTheConfiguredValue(string value, bool expected)
    {
        await using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["FeatureManagement:RegistrationEnabled"] = value
        });

        var enabled = await provider.GetRequiredService<IVariantFeatureManager>()
            .IsEnabledAsync(FeatureFlags.RegistrationEnabled, CancellationToken.None);

        Assert.Equal(expected, enabled);
    }

    [Fact]
    public async Task RegistrationEnabled_MissingDefinition_IsDisabled()
    {
        await using var provider = BuildProvider(new Dictionary<string, string?>());

        var enabled = await provider.GetRequiredService<IVariantFeatureManager>()
            .IsEnabledAsync(FeatureFlags.RegistrationEnabled, CancellationToken.None);

        Assert.False(enabled);
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging()
            .AddApplication()
            .BuildServiceProvider();
    }
}
