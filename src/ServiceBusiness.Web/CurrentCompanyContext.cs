using ServiceBusiness.Application;
using ServiceBusiness.Domain;

namespace ServiceBusiness.Web;

public sealed class CurrentCompanyContext(
    OnboardingService onboardingService,
    ICurrentUserContext currentUser)
{
    public async Task<string> GetRequiredCompanyIdAsync(
        IReadOnlyCollection<CompanyRole> roles,
        CancellationToken cancellationToken = default)
    {
        var overview = await onboardingService.GetAccessOverviewAsync(currentUser.UserId, cancellationToken);
        var access = overview.Companies
            .Where(companyAccess =>
                companyAccess.Status == MembershipStatus.Active &&
                roles.Contains(companyAccess.Role))
            .OrderBy(companyAccess => companyAccess.Company.Name)
            .FirstOrDefault();

        return access?.Company.Id
            ?? throw new UnauthorizedAccessException("An active company admin business is required.");
    }
}
