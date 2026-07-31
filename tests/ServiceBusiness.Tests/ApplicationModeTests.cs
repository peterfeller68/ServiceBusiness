using ServiceBusiness.Domain;
using ServiceBusiness.Web;
using Microsoft.Extensions.Configuration;

namespace ServiceBusiness.Tests;

public sealed class ApplicationModeTests
{
    [Fact]
    public void Pool_mode_uses_pool_branding_and_pool_equipment()
    {
        var mode = ApplicationModeSnapshot.From(SystemMode.Pool);

        Assert.Equal(SystemMode.Pool, mode.Mode);
        Assert.Equal("PoolShark", mode.ProductName);
        Assert.True(mode.IsPoolMode);
        Assert.Equal("/images/pool-waterfall-hero.png", mode.HeroImageUrl);
    }

    [Fact]
    public void Landscape_mode_uses_landscape_branding_and_hides_pool_equipment()
    {
        var mode = ApplicationModeSnapshot.From(SystemMode.Landscape);

        Assert.Equal(SystemMode.Landscape, mode.Mode);
        Assert.Equal("TreeShark", mode.ProductName);
        Assert.False(mode.IsPoolMode);
        Assert.Equal("/images/landscape-fruit-trees-hero.png", mode.HeroImageUrl);
    }

    [Fact]
    public async Task Application_mode_service_reads_configured_system_settings()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SystemSettings:SystemMode"] = "Landscape",
                ["SystemSettings:DevTest"] = "true"
            })
            .Build();
        var service = new ApplicationModeService(configuration);

        var mode = await service.GetCurrentAsync();

        Assert.Equal(SystemMode.Landscape, mode.Mode);
        Assert.Equal("TreeShark", mode.ProductName);
        Assert.True(mode.DevTest);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("false", false)]
    [InlineData("", false)]
    public void Dev_test_setting_is_enabled_only_when_configured_true(string configuredValue, bool expected)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SystemSettings:DevTest"] = configuredValue
            })
            .Build();

        Assert.Equal(expected, SystemSettingsConfiguration.IsDevTest(configuration));
    }

    [Fact]
    public void Dev_test_setting_defaults_to_disabled_when_missing()
    {
        var configuration = new ConfigurationBuilder().Build();

        Assert.False(SystemSettingsConfiguration.IsDevTest(configuration));
    }

    [Fact]
    public void Configured_defaults_use_pool_mode_when_missing()
    {
        var configuration = new ConfigurationBuilder().Build();

        var settings = SystemSettingsConfiguration.GetConfiguredDefaults(configuration);

        Assert.Equal(SystemMode.Pool, settings.SystemMode);
        Assert.False(settings.DevTest);
    }
}
