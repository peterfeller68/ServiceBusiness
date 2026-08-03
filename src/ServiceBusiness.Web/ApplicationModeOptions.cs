using Microsoft.Extensions.Configuration;
using ServiceBusiness.Domain;

namespace ServiceBusiness.Web;

public sealed class ApplicationModeService(IConfiguration configuration)
{
    public Task<ApplicationModeSnapshot> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(ApplicationModeSnapshot.From(SystemSettingsConfiguration.GetConfiguredDefaults(configuration)));
}

public sealed record ApplicationModeSnapshot(
    SystemMode Mode,
    bool DevTest,
    bool IsPoolMode,
    string ProductName,
    string ProductCategory,
    string HeroImageUrl,
    string HeroAltText,
    string GlobalCatalogCompanyId)
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
                : "A manicured lawn with mature fruit trees.",
            GlobalCatalogScope.For(mode));
    }
}
