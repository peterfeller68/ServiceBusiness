using Microsoft.Extensions.Configuration;
using ServiceBusiness.Application;
using ServiceBusiness.Domain;

namespace ServiceBusiness.Web;

public static class SystemSettingsConfiguration
{
    public static SystemSettings GetConfiguredDefaults(IConfiguration configuration)
    {
        var configuredMode = configuration["SystemSettings:SystemMode"] ?? configuration["SystemMode"];
        var mode = Enum.TryParse<SystemMode>(configuredMode, ignoreCase: true, out var parsedMode)
            ? parsedMode
            : SystemMode.Pool;

        return new SystemSettings(mode, IsDevTest(configuration));
    }

    public static bool IsDevTest(IConfiguration configuration) =>
        bool.TryParse(configuration["SystemSettings:DevTest"], out var devTest) && devTest;

    public static async Task<bool> IsDevTestEnabledAsync(
        IConfiguration configuration,
        IServiceBusinessStore store,
        CancellationToken cancellationToken = default)
    {
        if (IsDevTest(configuration))
        {
            return true;
        }

        var settings = await store.GetSystemSettingsAsync(cancellationToken);
        return settings.DevTest;
    }
}
