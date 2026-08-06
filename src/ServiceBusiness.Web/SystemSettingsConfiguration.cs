using Microsoft.Extensions.Configuration;
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

        var trialDays = int.TryParse(configuration["SystemSettings:HomeOwnerTrialDays"], out var parsedTrialDays)
            ? Math.Max(0, parsedTrialDays)
            : 14;

        return new SystemSettings(mode, IsDevTest(configuration), trialDays);
    }

    public static bool IsDevTest(IConfiguration configuration) =>
        bool.TryParse(configuration["SystemSettings:DevTest"], out var devTest) && devTest;

    public static bool IsDevTestEnabled(IConfiguration configuration) =>
        GetConfiguredDefaults(configuration).DevTest;
}
