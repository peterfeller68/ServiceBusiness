using ServiceBusiness.Domain;
using ServiceBusiness.Infrastructure.AzureStorage;
using ServiceBusiness.Web;

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
    public async Task Application_mode_service_reads_persisted_system_settings()
    {
        var store = new InMemoryServiceBusinessStore();
        await store.UpsertSystemSettingsAsync(new SystemSettings(SystemMode.Landscape));
        var service = new ApplicationModeService(store);

        var mode = await service.GetCurrentAsync();

        Assert.Equal(SystemMode.Landscape, mode.Mode);
        Assert.Equal("TreeShark", mode.ProductName);
    }
}
