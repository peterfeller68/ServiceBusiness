using ServiceBusiness.Application;
using ServiceBusiness.Domain;

namespace ServiceBusiness.Web;

public sealed class ApplicationModeService(IServiceBusinessStore store)
{
    public async Task<ApplicationModeSnapshot> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var settings = await store.GetSystemSettingsAsync(cancellationToken);
        return ApplicationModeSnapshot.From(settings.SystemMode);
    }
}

public sealed record ApplicationModeSnapshot(
    SystemMode Mode,
    bool IsPoolMode,
    string ProductName,
    string ProductCategory,
    string HeroImageUrl,
    string HeroAltText)
{
    public static ApplicationModeSnapshot Pool { get; } = From(SystemMode.Pool);

    public static ApplicationModeSnapshot From(SystemMode mode)
    {
        var isPoolMode = mode == SystemMode.Pool;
        return new ApplicationModeSnapshot(
            mode,
            isPoolMode,
            isPoolMode ? "PoolShark" : "TreeShark",
            isPoolMode ? "Pool service operations" : "Landscape service operations",
            isPoolMode ? "/images/pool-waterfall-hero.png" : "/images/landscape-fruit-trees-hero.png",
            isPoolMode
                ? "A bright pool with a waterfall in a lush landscape."
                : "A manicured lawn with mature fruit trees.");
    }
}
