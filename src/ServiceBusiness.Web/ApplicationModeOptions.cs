using ServiceBusiness.Application;
using ServiceBusiness.Domain;

namespace ServiceBusiness.Web;

public sealed class ApplicationModeService(IServiceBusinessStore store)
{
    public async Task<ApplicationModeSnapshot> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var settings = await store.GetSystemSettingsAsync(cancellationToken);
        return ApplicationModeSnapshot.From(settings);
    }
}

public sealed record ApplicationModeSnapshot(
    SystemMode Mode,
    bool DevTest,
    bool IsPoolMode,
    string ProductName,
    string ProductCategory,
    string HeroImageUrl,
    string HeroAltText)
{
    public static ApplicationModeSnapshot Pool { get; } = From(new SystemSettings(SystemMode.Pool));

    public static ApplicationModeSnapshot From(SystemMode mode) =>
        From(new SystemSettings(mode));

    public static ApplicationModeSnapshot From(SystemSettings settings)
    {
        var mode = settings.SystemMode;
        var isPoolMode = mode == SystemMode.Pool;
        return new ApplicationModeSnapshot(
            mode,
            settings.DevTest,
            isPoolMode,
            isPoolMode ? "PoolShark" : "TreeShark",
            isPoolMode ? "Pool service operations" : "Landscape service operations",
            isPoolMode ? "/images/pool-waterfall-hero.png" : "/images/landscape-fruit-trees-hero.png",
            isPoolMode
                ? "A bright pool with a waterfall in a lush landscape."
                : "A manicured lawn with mature fruit trees.");
    }
}
