using System.Net;
using ServiceBusiness.Domain;

namespace ServiceBusiness.Application;

public static class BusinessClientTypeReferenceData
{
    public const string HomeOwnerId = "home-owner";
    public const string HomeOwnerName = "Home Owner";

    public static ClientType HomeOwner(string companyId) =>
        new(HomeOwnerId, companyId, HomeOwnerName, BillingFrequency.FeeForService, 0m, true);

    public static async Task<IReadOnlyList<ClientType>> EnsureForCompanyAsync(
        IServiceBusinessStore store,
        string companyId,
        CancellationToken cancellationToken = default)
    {
        var clientTypes = await store.GetClientTypesAsync(companyId, cancellationToken);
        var homeOwner = clientTypes.FirstOrDefault(type => string.Equals(type.Id, HomeOwnerId, StringComparison.OrdinalIgnoreCase));
        if (homeOwner is null || !homeOwner.IsActive || !string.Equals(homeOwner.Name, HomeOwnerName, StringComparison.Ordinal))
        {
            homeOwner = HomeOwner(companyId);
            await store.UpsertClientTypeAsync(homeOwner, cancellationToken);
        }

        return clientTypes
            .Where(type => !string.Equals(type.Id, HomeOwnerId, StringComparison.OrdinalIgnoreCase))
            .Prepend(homeOwner)
            .ToList();
    }
}

public sealed class TenantAuthorizationService(IServiceBusinessStore store, ICurrentUserContext currentUser)
{
    public async Task<AppUser> RequireCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var user = await store.GetUserAsync(currentUser.UserId, cancellationToken);
        if (user is null)
        {
            throw new UnauthorizedAccessException("The current user profile was not found.");
        }

        if (user.Status == UserStatus.Disabled)
        {
            throw new UnauthorizedAccessException("The current user account is disabled.");
        }

        return user;
    }

    public async Task RequireSystemAdminAsync(CancellationToken cancellationToken = default)
    {
        var user = await RequireCurrentUserAsync(cancellationToken);
        if (!user.IsSystemAdmin)
        {
            throw new UnauthorizedAccessException("System administrator access is required.");
        }
    }

    public async Task<CompanyMembership> RequireCompanyRoleAsync(
        string companyId,
        CompanyRole role,
        CancellationToken cancellationToken = default)
    {
        var memberships = await store.GetMembershipsForUserAsync(currentUser.UserId, cancellationToken);
        var membership = memberships.FirstOrDefault(m =>
            m.CompanyId == companyId &&
            m.Role == role &&
            m.Status == MembershipStatus.Active);

        if (membership is null)
        {
            throw new UnauthorizedAccessException($"{role} access is required for company {companyId}.");
        }

        return membership;
    }

    public async Task<CompanyMembership> RequireAnyCompanyRoleAsync(
        string companyId,
        IReadOnlyCollection<CompanyRole> roles,
        CancellationToken cancellationToken = default)
    {
        var memberships = await store.GetMembershipsForUserAsync(currentUser.UserId, cancellationToken);
        var membership = memberships.FirstOrDefault(m =>
            m.CompanyId == companyId &&
            roles.Contains(m.Role) &&
            m.Status == MembershipStatus.Active);

        if (membership is null)
        {
            throw new UnauthorizedAccessException("An active company membership is required.");
        }

        return membership;
    }

    public async Task RequireSystemAdminOrAnyCompanyRoleAsync(
        string companyId,
        IReadOnlyCollection<CompanyRole> roles,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireCurrentUserAsync(cancellationToken);
        if (user.IsSystemAdmin)
        {
            return;
        }

        await RequireAnyCompanyRoleAsync(companyId, roles, cancellationToken);
    }
}

public sealed class PlatformAdminService(IServiceBusinessStore store, TenantAuthorizationService authorization)
{
    public const string TestHomeOwnerProfileMarker = "[TestUserType:HomeOwner]";

    public async Task<SystemSettings> GetSystemSettingsAsync(CancellationToken cancellationToken = default)
    {
        var actor = await authorization.RequireCurrentUserAsync(cancellationToken);
        if (!actor.IsSystemAdmin)
        {
            throw new UnauthorizedAccessException("System administrator access is required.");
        }
        return await store.GetSystemSettingsAsync(cancellationToken);
    }

    public async Task<SystemSettings> UpdateSystemModeAsync(SystemMode systemMode, CancellationToken cancellationToken = default)
    {
        var actor = await authorization.RequireCurrentUserAsync(cancellationToken);
        if (!actor.IsSystemAdmin)
        {
            throw new UnauthorizedAccessException("System administrator access is required.");
        }

        var currentSettings = await store.GetSystemSettingsAsync(cancellationToken);
        var settings = currentSettings with { SystemMode = systemMode };
        await store.UpsertSystemSettingsAsync(settings, cancellationToken);
        return settings;
    }

    public async Task<SystemSettings> UpdateSystemSettingsAsync(SystemSettings settings, CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminAsync(cancellationToken);

        await store.UpsertSystemSettingsAsync(settings, cancellationToken);
        return settings;
    }

    public async Task<IReadOnlyList<CompanyType>> GetCompanyTypesAsync(CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminAsync(cancellationToken);
        return await store.GetCompanyTypesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Company>> GetCompaniesAsync(CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminAsync(cancellationToken);
        return await store.GetCompaniesAsync(cancellationToken);
    }

    public async Task UpsertCompanyAsync(Company company, CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(company.Id))
        {
            throw new InvalidOperationException("Company ID is required.");
        }

        if (string.IsNullOrWhiteSpace(company.CompanyTypeId))
        {
            throw new InvalidOperationException("Company type is required.");
        }

        if (string.IsNullOrWhiteSpace(company.Name))
        {
            throw new InvalidOperationException("Company name is required.");
        }

        if (string.IsNullOrWhiteSpace(company.BusinessEmail) || !company.BusinessEmail.Contains('@', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Business email must be valid.");
        }

        var companyTypes = await store.GetCompanyTypesAsync(cancellationToken);
        if (!companyTypes.Any(t => t.Id == company.CompanyTypeId))
        {
            throw new InvalidOperationException("Company type was not found.");
        }

        if (!string.IsNullOrWhiteSpace(company.ServicePackageId))
        {
            var companyType = companyTypes.First(t => t.Id == company.CompanyTypeId);
            var globalCatalogCompanyId = GlobalCatalogScope.For(GetSystemModeForCompanyType(companyType));
            var servicePackageExists = (await store.GetServicePackagesAsync(globalCatalogCompanyId, cancellationToken))
                .Any(package => package.IsActive && string.Equals(package.Id, company.ServicePackageId, StringComparison.OrdinalIgnoreCase));
            if (!servicePackageExists)
            {
                throw new InvalidOperationException("Service package was not found.");
            }
        }

        await store.UpsertCompanyAsync(company with
        {
            Id = CreateSlug(company.Id),
            CompanyTypeId = company.CompanyTypeId.Trim(),
            Name = company.Name.Trim(),
            BusinessEmail = company.BusinessEmail.Trim().ToLowerInvariant(),
            BusinessPhone = string.IsNullOrWhiteSpace(company.BusinessPhone) ? "" : company.BusinessPhone.Trim(),
            TimeZone = string.IsNullOrWhiteSpace(company.TimeZone) ? "America/Los_Angeles" : company.TimeZone.Trim(),
            ServicePackageId = string.IsNullOrWhiteSpace(company.ServicePackageId) ? null : company.ServicePackageId.Trim()
        }, cancellationToken);
    }

    private static SystemMode GetSystemModeForCompanyType(CompanyType companyType)
    {
        var searchable = $"{companyType.Id} {companyType.Name} {companyType.Description}";
        return PlatformContainsAny(searchable, "landscape", "landscaping", "lawn", "yard", "tree")
            ? SystemMode.Landscape
            : SystemMode.Pool;
    }

    public async Task SetCompanyStatusAsync(
        string companyId,
        CompanyStatus status,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminAsync(cancellationToken);

        var company = await store.GetCompanyAsync(companyId, cancellationToken)
            ?? throw new InvalidOperationException("Company was not found.");

        await store.UpsertCompanyAsync(company with { Status = status }, cancellationToken);
    }

    public async Task<IReadOnlyList<BusinessClientManagementRow>> GetBusinessClientManagementRowsAsync(CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminAsync(cancellationToken);

        var companies = await store.GetCompaniesAsync(cancellationToken);
        var rows = new List<BusinessClientManagementRow>();

        foreach (var company in companies.OrderBy(c => c.Name))
        {
            var clientTypes = await store.GetClientTypesAsync(company.Id, cancellationToken);
            var clients = await store.GetClientsAsync(company.Id, cancellationToken);

            rows.AddRange(clients
                .OrderBy(client => client.DisplayName)
                .Select(client => new BusinessClientManagementRow(
                    company,
                    client,
                    clientTypes.FirstOrDefault(type => string.Equals(type.Id, client.ClientTypeId, StringComparison.OrdinalIgnoreCase)))));
        }

        return rows;
    }

    public async Task<IReadOnlyList<PoolConfigurationClientRow>> GetPoolConfigurationClientsAsync(CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminAsync(cancellationToken);

        var companies = await store.GetCompaniesAsync(cancellationToken);
        var companyTypes = await store.GetCompanyTypesAsync(cancellationToken);
        var settings = await store.GetSystemSettingsAsync(cancellationToken);
        var visibleCompanyTypeIds = companyTypes
            .Where(type => type.IsActive && PlatformCompanyTypeMatchesSystemMode(type, settings.SystemMode))
            .Select(type => type.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rows = new List<PoolConfigurationClientRow>();

        foreach (var company in companies
            .Where(company => visibleCompanyTypeIds.Contains(company.CompanyTypeId))
            .OrderBy(c => c.Name))
        {
            var clients = await store.GetClientsAsync(company.Id, cancellationToken);
            rows.AddRange(clients
                .Where(client => client.IsActive)
                .OrderBy(client => client.ServiceAddress)
                .ThenBy(client => client.DisplayName)
                .Select(client => new PoolConfigurationClientRow(
                    client.Id,
                    company.Name,
                    client.ServiceAddress,
                    "Business Client")));
        }

        var homeOwnerProfiles = await store.GetIndependentHomeOwnerProfilesAsync(cancellationToken);
        rows.AddRange(homeOwnerProfiles
            .OrderBy(profile => profile.HomeAddress)
            .ThenBy(profile => profile.UserId)
            .Select(profile => new PoolConfigurationClientRow(
                profile.UserId,
                "",
                string.IsNullOrWhiteSpace(profile.HomeAddress) ? profile.UserId : profile.HomeAddress,
                "Independent Home Owner")));

        return rows
            .OrderBy(row => row.CompanyName)
            .ThenBy(row => row.ClientAddress)
            .ToList();
    }

    private static bool PlatformCompanyTypeMatchesSystemMode(CompanyType type, SystemMode systemMode)
    {
        var searchable = $"{type.Id} {type.Name} {type.Description}";
        return systemMode == SystemMode.Pool
            ? searchable.Contains("pool", StringComparison.OrdinalIgnoreCase)
            : PlatformContainsAny(searchable, "landscape", "landscaping", "lawn", "yard", "tree");
    }

    private static bool PlatformContainsAny(string value, params string[] terms) =>
        terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));

    public async Task<IReadOnlyList<ClientType>> GetClientTypesForCompanyAsync(string companyId, CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminAsync(cancellationToken);
        return await BusinessClientTypeReferenceData.EnsureForCompanyAsync(store, companyId, cancellationToken);
    }

    public async Task UpsertBusinessClientAsync(CompanyClient client, CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminAsync(cancellationToken);
        await ValidateClientAsync(client, cancellationToken);
        await store.UpsertClientAsync(NormalizeClient(client), cancellationToken);
    }

    private async Task ValidateClientAsync(CompanyClient client, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(client.Id))
        {
            throw new InvalidOperationException("Business client ID is required.");
        }

        if (string.IsNullOrWhiteSpace(client.CompanyId))
        {
            throw new InvalidOperationException("Service client is required.");
        }

        if (string.IsNullOrWhiteSpace(client.DisplayName))
        {
            throw new InvalidOperationException("Business client name is required.");
        }

        if (string.IsNullOrWhiteSpace(client.PrimaryContactName))
        {
            throw new InvalidOperationException("Primary contact is required.");
        }

        if (string.IsNullOrWhiteSpace(client.Email) || !client.Email.Contains('@', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Business client email must be valid.");
        }

        if (string.IsNullOrWhiteSpace(client.ServiceAddress))
        {
            throw new InvalidOperationException("Service address is required.");
        }

        var company = await store.GetCompanyAsync(client.CompanyId, cancellationToken);
        if (company is null)
        {
            throw new InvalidOperationException("Service client was not found.");
        }

        var clientTypes = await BusinessClientTypeReferenceData.EnsureForCompanyAsync(store, client.CompanyId, cancellationToken);
        if (!clientTypes.Any(t => t.Id == client.ClientTypeId && t.IsActive))
        {
            throw new InvalidOperationException("An active business client type is required.");
        }

        await ValidateBusinessClientServicePackageAsync(client, cancellationToken);
    }

    private static CompanyClient NormalizeClient(CompanyClient client) => client with
    {
        Id = CreateSlug(client.Id),
        CompanyId = client.CompanyId.Trim(),
        DisplayName = client.DisplayName.Trim(),
        PrimaryContactName = client.PrimaryContactName.Trim(),
        Email = client.Email.Trim().ToLowerInvariant(),
        Phone = string.IsNullOrWhiteSpace(client.Phone) ? "" : client.Phone.Trim(),
        ServiceAddress = client.ServiceAddress.Trim(),
        AccessNotes = string.IsNullOrWhiteSpace(client.AccessNotes) ? "" : client.AccessNotes.Trim(),
        ClientTypeId = client.ClientTypeId.Trim(),
        ServicePackageId = string.IsNullOrWhiteSpace(client.ServicePackageId) ? null : client.ServicePackageId.Trim()
    };

    private async Task ValidateBusinessClientServicePackageAsync(CompanyClient client, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(client.ServicePackageId))
        {
            return;
        }

        var company = await store.GetCompanyAsync(client.CompanyId, cancellationToken)
            ?? throw new InvalidOperationException("Service client was not found.");
        var companyType = (await store.GetCompanyTypesAsync(cancellationToken))
            .FirstOrDefault(type => string.Equals(type.Id, company.CompanyTypeId, StringComparison.OrdinalIgnoreCase));
        var globalCatalogCompanyId = GlobalCatalogScope.For(companyType is null ? SystemMode.Pool : GetSystemModeForCompanyType(companyType));
        var allowedPackages = (await store.GetServicePackagesAsync(company.Id, cancellationToken))
            .Concat(await store.GetServicePackagesAsync(globalCatalogCompanyId, cancellationToken));

        if (!allowedPackages.Any(package => package.IsActive && string.Equals(package.Id, client.ServicePackageId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Service package was not found.");
        }
    }

    public async Task<IReadOnlyList<EmailLogEntry>> GetEmailLogsAsync(CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminAsync(cancellationToken);
        return await store.GetEmailLogsAsync(cancellationToken);
    }

    public async Task<PlatformUserManagementOverview> GetUserManagementOverviewAsync(CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminAsync(cancellationToken);

        var users = await store.GetUsersAsync(cancellationToken);
        var companies = await store.GetCompaniesAsync(cancellationToken);
        var roles = (await store.GetRoleDefinitionsAsync(cancellationToken))
            .Select(NormalizeRoleDefinition)
            .ToList();
        var rows = new List<UserManagementRow>();

        foreach (var user in users.OrderBy(u => u.DisplayName))
        {
            var memberships = await store.GetMembershipsForUserAsync(user.Id, cancellationToken);
            var access = memberships
                .Select(m => new CompanyAccess(
                    companies.First(c => c.Id == m.CompanyId),
                    m.Role,
                    m.Status))
                .OrderBy(a => a.Company.Name)
                .ThenBy(a => a.Role)
                .ToList();

            rows.Add(new UserManagementRow(
                user,
                access,
                memberships.Count(m => m.Status == MembershipStatus.Pending),
                memberships.Count(m => m.Status == MembershipStatus.Active)));
        }

        return new PlatformUserManagementOverview(
            users.Count,
            users.Count(u => u.Status == UserStatus.Active),
            users.Count(u => u.Status == UserStatus.Disabled),
            users.Count(u => u.IsSystemAdmin),
            rows.Sum(r => r.PendingMembershipCount),
            roles,
            rows);
    }

    public async Task<IReadOnlyList<RoleDefinition>> GetRoleDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminAsync(cancellationToken);
        return (await store.GetRoleDefinitionsAsync(cancellationToken))
            .Select(NormalizeRoleDefinition)
            .ToList();
    }

    public async Task UpdateRoleDefinitionAsync(
        RoleDefinition roleDefinition,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(roleDefinition.DisplayName))
        {
            throw new InvalidOperationException("Role display name is required.");
        }

        if (string.IsNullOrWhiteSpace(roleDefinition.Description))
        {
            throw new InvalidOperationException("Role description is required.");
        }

        var normalizedPermissions = (roleDefinition.Permissions ?? [])
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p)
            .ToList();

        if (normalizedPermissions.Count == 0)
        {
            throw new InvalidOperationException("At least one permission is required.");
        }

        await store.UpsertRoleDefinitionAsync(roleDefinition with
        {
            DisplayName = roleDefinition.DisplayName.Trim(),
            Description = roleDefinition.Description.Trim(),
            Permissions = normalizedPermissions
        }, cancellationToken);
    }

    public async Task SetSystemAdminAsync(
        string userId,
        bool isSystemAdmin,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminAsync(cancellationToken);

        var user = await store.GetUserAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");

        if (!isSystemAdmin)
        {
            await EnsureAnotherActiveSystemAdminExistsAsync(user.Id, cancellationToken);
        }

        await store.UpsertUserAsync(user with { IsSystemAdmin = isSystemAdmin }, cancellationToken);
    }

    public async Task SetUserStatusAsync(
        string userId,
        UserStatus status,
        CancellationToken cancellationToken = default)
    {
        var actor = await authorization.RequireCurrentUserAsync(cancellationToken);
        if (!actor.IsSystemAdmin)
        {
            throw new UnauthorizedAccessException("System administrator access is required.");
        }

        if (actor.Id == userId && status == UserStatus.Disabled)
        {
            throw new InvalidOperationException("A system admin cannot disable their own account.");
        }

        var user = await store.GetUserAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");

        if (user.IsSystemAdmin && status == UserStatus.Disabled)
        {
            await EnsureAnotherActiveSystemAdminExistsAsync(user.Id, cancellationToken);
        }

        await store.UpsertUserAsync(user with { Status = status }, cancellationToken);
    }

    public async Task<UserDeletionResult> DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var actor = await authorization.RequireCurrentUserAsync(cancellationToken);
        if (!actor.IsSystemAdmin)
        {
            throw new UnauthorizedAccessException("System administrator access is required.");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("User ID is required.");
        }

        var normalizedUserId = userId.Trim();
        if (string.Equals(actor.Id, normalizedUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("You cannot delete the currently signed-in system administrator.");
        }

        var user = await store.GetUserAsync(normalizedUserId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");

        if (user.IsSystemAdmin)
        {
            await EnsureAnotherActiveSystemAdminExistsAsync(user.Id, cancellationToken);
        }

        return await store.DeleteUserAsync(user.Id, cancellationToken);
    }

    public async Task SetCompanyMembershipApprovalAsync(
        string companyId,
        string userId,
        CompanyRole role,
        bool isApproved,
        CancellationToken cancellationToken = default)
    {
        var actor = await authorization.RequireCurrentUserAsync(cancellationToken);
        if (!actor.IsSystemAdmin)
        {
            throw new UnauthorizedAccessException("System administrator access is required.");
        }

        var memberships = await store.GetMembershipsForCompanyAsync(companyId, cancellationToken);
        var membership = memberships.FirstOrDefault(m => m.UserId == userId && m.Role == role)
            ?? throw new InvalidOperationException("Company user membership was not found.");

        var nextStatus = isApproved ? MembershipStatus.Active : MembershipStatus.Inactive;
        if (membership.Status == MembershipStatus.Pending && !isApproved)
        {
            nextStatus = MembershipStatus.Rejected;
        }

        if (membership.Role == CompanyRole.CompanyAdmin && nextStatus != MembershipStatus.Active)
        {
            EnsureAnotherActiveCompanyAdminExists(memberships, userId);
        }

        await store.UpsertMembershipAsync(membership with
        {
            Status = nextStatus,
            DecidedUtc = DateTimeOffset.UtcNow,
            DecidedByUserId = actor.Id
        }, cancellationToken);
    }

    public async Task UpdateCompanyMembershipRoleAsync(
        string companyId,
        string userId,
        CompanyRole currentRole,
        CompanyRole newRole,
        CancellationToken cancellationToken = default)
    {
        var actor = await authorization.RequireCurrentUserAsync(cancellationToken);
        if (!actor.IsSystemAdmin)
        {
            throw new UnauthorizedAccessException("System administrator access is required.");
        }

        if (string.Equals(actor.Id, userId, StringComparison.OrdinalIgnoreCase) && currentRole != newRole)
        {
            throw new InvalidOperationException("A system admin cannot change their own user type.");
        }

        var memberships = await store.GetMembershipsForCompanyAsync(companyId, cancellationToken);
        var membership = memberships.FirstOrDefault(m => m.UserId == userId && m.Role == currentRole)
            ?? throw new InvalidOperationException("Company user membership was not found.");

        if (membership.Status is not MembershipStatus.Active and not MembershipStatus.Inactive)
        {
            throw new InvalidOperationException("Only approved company users can have roles updated.");
        }

        if (currentRole == newRole)
        {
            return;
        }

        if (currentRole == CompanyRole.CompanyAdmin)
        {
            EnsureAnotherActiveCompanyAdminExists(memberships, userId);
        }

        await store.UpsertMembershipAsync(membership with
        {
            Status = MembershipStatus.Removed,
            DecidedUtc = DateTimeOffset.UtcNow,
            DecidedByUserId = actor.Id
        }, cancellationToken);

        var replacement = memberships.FirstOrDefault(m => m.UserId == userId && m.Role == newRole);
        await store.UpsertMembershipAsync((replacement ?? new CompanyMembership(
            companyId,
            userId,
            newRole,
            membership.Status,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            actor.Id,
            membership.CompanyClientId)) with
        {
            Status = membership.Status,
            DecidedUtc = DateTimeOffset.UtcNow,
            DecidedByUserId = actor.Id,
            CompanyClientId = membership.CompanyClientId
        }, cancellationToken);
    }

    public async Task UpdateUserAsync(
        string userId,
        string displayName,
        string email,
        string? notificationEmail,
        string? phone,
        bool isTestUser,
        bool emailNotificationsEnabled,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminAsync(cancellationToken);

        var user = await store.GetUserAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new InvalidOperationException("Display name is required.");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Email must be valid.");
        }

        var trimmedNotificationEmail = string.IsNullOrWhiteSpace(notificationEmail)
            ? email.Trim().ToLowerInvariant()
            : notificationEmail.Trim().ToLowerInvariant();

        if (!trimmedNotificationEmail.Contains('@', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Notification email must be valid.");
        }

        await store.UpsertUserAsync(user with
        {
            Email = email.Trim().ToLowerInvariant(),
            NotificationEmail = trimmedNotificationEmail,
            DisplayName = displayName.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            IsTestUser = isTestUser,
            EmailNotificationsEnabled = emailNotificationsEnabled
        }, cancellationToken);
    }

    public async Task<AppUser> CreateUserAsync(
        string displayName,
        string email,
        string? notificationEmail,
        string? phone,
        bool isTestUser,
        bool emailNotificationsEnabled,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new InvalidOperationException("Display name is required.");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Email must be valid.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existing = await store.GetUserByEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("A user with that email already exists.");
        }

        var trimmedNotificationEmail = string.IsNullOrWhiteSpace(notificationEmail)
            ? normalizedEmail
            : notificationEmail.Trim().ToLowerInvariant();

        if (!trimmedNotificationEmail.Contains('@', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Notification email must be valid.");
        }

        var user = new AppUser(
            CreateSlug(normalizedEmail.Split('@')[0]),
            null,
            normalizedEmail,
            trimmedNotificationEmail,
            displayName.Trim(),
            string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            null,
            false,
            isTestUser,
            emailNotificationsEnabled,
            UserStatus.Active);

        await store.UpsertUserAsync(user, cancellationToken);
        return user;
    }

    public async Task ConfigureTestUserAccessAsync(
        string userId,
        bool isSystemAdmin,
        string? companyId,
        CompanyRole role,
        bool approvalNeeded,
        bool isHomeOwner,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminAsync(cancellationToken);

        var user = await store.GetUserAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");
        var company = string.IsNullOrWhiteSpace(companyId)
            ? null
            : await store.GetCompanyAsync(companyId, cancellationToken)
                ?? throw new InvalidOperationException("Business was not found.");

        if (user.Id == "sys-admin" && !isSystemAdmin)
        {
            throw new InvalidOperationException("The seeded system administrator must remain a system admin.");
        }

        await store.UpsertUserAsync(user with
        {
            IsSystemAdmin = isSystemAdmin,
            IsTestUser = true
        }, cancellationToken);

        var status = approvalNeeded ? MembershipStatus.Pending : MembershipStatus.Active;
        var existingMemberships = await store.GetMembershipsForUserAsync(user.Id, cancellationToken);
        foreach (var existing in existingMemberships.Where(m =>
            company is null ||
            m.CompanyId != company.Id ||
            m.Role != role ||
            m.Status is MembershipStatus.Active or MembershipStatus.Pending or MembershipStatus.Inactive))
        {
            await store.UpsertMembershipAsync(existing with
            {
                Status = MembershipStatus.Removed,
                DecidedUtc = DateTimeOffset.UtcNow,
                DecidedByUserId = "sys-admin"
            }, cancellationToken);
        }

        if (company is not null)
        {
            var matchingMembership = existingMemberships.FirstOrDefault(m => m.CompanyId == company.Id && m.Role == role);
            await store.UpsertMembershipAsync((matchingMembership ?? new CompanyMembership(
                company.Id,
                user.Id,
                role,
                status,
                DateTimeOffset.UtcNow,
                approvalNeeded ? null : DateTimeOffset.UtcNow,
                approvalNeeded ? null : "sys-admin")) with
            {
                Status = status,
                DecidedUtc = approvalNeeded ? null : DateTimeOffset.UtcNow,
                DecidedByUserId = approvalNeeded ? null : "sys-admin"
            }, cancellationToken);
        }

        var existingProfile = await store.GetIndependentHomeOwnerProfileAsync(user.Id, cancellationToken);
        if (isHomeOwner)
        {
            var now = DateTimeOffset.UtcNow;
            await store.UpsertIndependentHomeOwnerProfileAsync((existingProfile ?? new IndependentHomeOwnerProfile(
                user.Id,
                "Test homeowner address",
                "",
                now,
                now)) with
            {
                AccessNotes = TestHomeOwnerProfileMarker,
                UpdatedUtc = now
            }, cancellationToken);
        }
        else if (existingProfile?.AccessNotes == TestHomeOwnerProfileMarker)
        {
            await store.UpsertIndependentHomeOwnerProfileAsync(existingProfile with
            {
                AccessNotes = "",
                UpdatedUtc = DateTimeOffset.UtcNow
            }, cancellationToken);
        }
    }

    private async Task EnsureAnotherActiveSystemAdminExistsAsync(string excludedUserId, CancellationToken cancellationToken)
    {
        var users = await store.GetUsersAsync(cancellationToken);
        if (users.Count(u => u.IsSystemAdmin && u.Status == UserStatus.Active && u.Id != excludedUserId) == 0)
        {
            throw new InvalidOperationException("At least one active system admin is required.");
        }
    }

    private static void EnsureAnotherActiveCompanyAdminExists(
        IReadOnlyList<CompanyMembership> memberships,
        string userId)
    {
        var anotherAdminExists = memberships.Any(m =>
            m.UserId != userId &&
            m.Role == CompanyRole.CompanyAdmin &&
            m.Status == MembershipStatus.Active);

        if (!anotherAdminExists)
        {
            throw new InvalidOperationException("At least one active company admin is required.");
        }
    }

    private static string CreateSlug(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        var slug = string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(slug) ? $"item-{Guid.NewGuid():N}" : slug;
    }

    private static RoleDefinition NormalizeRoleDefinition(RoleDefinition roleDefinition)
    {
        return roleDefinition.Permissions is { Count: > 0 }
            ? roleDefinition
            : roleDefinition with { Permissions = GetDefaultPermissions(roleDefinition.Role) };
    }

    private static IReadOnlyList<string> GetDefaultPermissions(CompanyRole role)
    {
        return role switch
        {
            CompanyRole.CompanyAdmin =>
            [
                "catalog.manage",
                "clients.manage",
                "company.configure",
                "reports.view",
                "schedule.manage",
                "users.approve"
            ],
            CompanyRole.CompanyUser =>
            [
                "materials.record",
                "visits.assigned.view",
                "visits.complete",
                "visits.start"
            ],
            CompanyRole.CompanyClientUser =>
            [
                "client.billing.view",
                "client.history.view",
                "client.messages.create"
            ],
            _ => []
        };
    }
}

public sealed class UserProfileService(IServiceBusinessStore store, TenantAuthorizationService authorization)
{
    public async Task<AppUser> GetCurrentProfileAsync(CancellationToken cancellationToken = default)
    {
        return await authorization.RequireCurrentUserAsync(cancellationToken);
    }

    public async Task<AppUser> UpdateCurrentProfileAsync(
        string displayName,
        string? notificationEmail,
        string? phone,
        bool emailNotificationsEnabled,
        CancellationToken cancellationToken = default)
    {
        var user = await authorization.RequireCurrentUserAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new InvalidOperationException("Display name is required.");
        }

        var trimmedNotificationEmail = string.IsNullOrWhiteSpace(notificationEmail)
            ? user.Email
            : notificationEmail.Trim().ToLowerInvariant();

        if (!trimmedNotificationEmail.Contains('@', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Notification email must be a valid email address.");
        }

        var updated = user with
        {
            DisplayName = displayName.Trim(),
            NotificationEmail = trimmedNotificationEmail,
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            EmailNotificationsEnabled = emailNotificationsEnabled
        };

        await store.UpsertUserAsync(updated, cancellationToken);
        return updated;
    }
}

public sealed class IndependentHomeOwnerService(IServiceBusinessStore store, ICurrentUserContext currentUser)
{
    public async Task<IndependentHomeOwnerDashboard> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var user = await RequireIndependentHomeOwnerAsync(cancellationToken);
        var profile = await GetOrCreateProfileAsync(user.Id, cancellationToken);
        var categories = await store.GetPoolEquipmentCategoriesAsync(EquipmentScope.HomeOwner, user.Id, cancellationToken);
        var items = await store.GetPoolEquipmentItemsAsync(EquipmentScope.HomeOwner, user.Id, cancellationToken);
        var history = (await store.GetIndependentHomeOwnerServiceHistoryAsync(user.Id, cancellationToken))
            .Where(item => !item.IsDeleted)
            .ToList();

        return new IndependentHomeOwnerDashboard(
            user,
            profile,
            BuildPoolEquipmentGroups(EquipmentScope.HomeOwner, user.Id, categories, items),
            history);
    }

    public async Task<IndependentHomeOwnerProfile> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        var user = await RequireIndependentHomeOwnerAsync(cancellationToken);
        return await GetOrCreateProfileAsync(user.Id, cancellationToken);
    }

    public async Task<IndependentHomeOwnerProfile> UpdateGeneralSettingsAsync(
        string displayName,
        string? phone,
        string homeAddress,
        string? accessNotes,
        string? generalNotes = null,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireIndependentHomeOwnerAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new InvalidOperationException("Display name is required.");
        }

        if (string.IsNullOrWhiteSpace(homeAddress))
        {
            throw new InvalidOperationException("Home address is required.");
        }

        await store.UpsertUserAsync(user with
        {
            DisplayName = displayName.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim()
        }, cancellationToken);

        var profile = await GetOrCreateProfileAsync(user.Id, cancellationToken);
        var updated = profile with
        {
            HomeAddress = homeAddress.Trim(),
            AccessNotes = string.IsNullOrWhiteSpace(accessNotes) ? "" : accessNotes.Trim(),
            GeneralNotes = string.IsNullOrWhiteSpace(generalNotes) ? "" : generalNotes.Trim(),
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        await store.UpsertIndependentHomeOwnerProfileAsync(updated, cancellationToken);
        return updated;
    }

    public async Task<IndependentHomeOwnerServiceHistoryItem> AddServiceHistoryItemAsync(
        string serviceId,
        DateOnly serviceDate,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireIndependentHomeOwnerAsync(cancellationToken);
        var service = await GetRequiredHomeOwnerServiceAsync(user.Id, serviceId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var item = new IndependentHomeOwnerServiceHistoryItem(
            $"home-service-{now:yyyyMMddHHmmssfff}",
            user.Id,
            new DateTimeOffset(serviceDate.ToDateTime(TimeOnly.MinValue)),
            string.IsNullOrWhiteSpace(notes) ? "" : notes.Trim(),
            now,
            service.Id,
            service.Name);

        await store.UpsertIndependentHomeOwnerServiceHistoryItemAsync(item, cancellationToken);
        return item;
    }

    public async Task<IndependentHomeOwnerServiceHistoryItem> UpdateServiceHistoryItemAsync(
        string itemId,
        string serviceId,
        DateOnly serviceDate,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireIndependentHomeOwnerAsync(cancellationToken);
        var service = await GetRequiredHomeOwnerServiceAsync(user.Id, serviceId, cancellationToken);
        var existing = (await store.GetIndependentHomeOwnerServiceHistoryAsync(user.Id, cancellationToken))
            .FirstOrDefault(item => item.Id == itemId)
            ?? throw new InvalidOperationException("Service history item was not found.");

        var updated = existing with
        {
            ServiceDateTime = new DateTimeOffset(serviceDate.ToDateTime(TimeOnly.MinValue)),
            Notes = string.IsNullOrWhiteSpace(notes) ? "" : notes.Trim(),
            ServiceId = service.Id,
            ServiceName = service.Name
        };

        await store.UpsertIndependentHomeOwnerServiceHistoryItemAsync(updated, cancellationToken);
        return updated;
    }

    public async Task DeleteServiceHistoryItemAsync(string itemId, CancellationToken cancellationToken = default)
    {
        var user = await RequireIndependentHomeOwnerAsync(cancellationToken);
        var item = (await store.GetIndependentHomeOwnerServiceHistoryAsync(user.Id, cancellationToken))
            .FirstOrDefault(item => item.Id == itemId && !item.IsDeleted)
            ?? throw new InvalidOperationException("Service history item was not found.");

        await store.UpsertIndependentHomeOwnerServiceHistoryItemAsync(item with { IsDeleted = true }, cancellationToken);
    }

    public async Task<IndependentHomeOwnerProfile> AddPoolEquipmentPhotosAsync(
        IReadOnlyList<HomeOwnerPoolEquipmentPhoto> photos,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireIndependentHomeOwnerAsync(cancellationToken);
        var profile = await GetOrCreateProfileAsync(user.Id, cancellationToken);
        foreach (var photo in photos)
        {
            await store.UpsertHomeOwnerPoolEquipmentPhotoAsync(user.Id, photo, cancellationToken);
        }

        var updated = profile with { UpdatedUtc = DateTimeOffset.UtcNow };
        await store.UpsertIndependentHomeOwnerProfileAsync(updated, cancellationToken);
        return await GetOrCreateProfileAsync(user.Id, cancellationToken);
    }

    public async Task<IndependentHomeOwnerProfile> DeletePoolEquipmentPhotoAsync(
        string photoId,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireIndependentHomeOwnerAsync(cancellationToken);
        var profile = await GetOrCreateProfileAsync(user.Id, cancellationToken);
        await store.DeleteHomeOwnerPoolEquipmentPhotoAsync(user.Id, photoId, cancellationToken);

        var updated = profile with { UpdatedUtc = DateTimeOffset.UtcNow };
        await store.UpsertIndependentHomeOwnerProfileAsync(updated, cancellationToken);
        return await GetOrCreateProfileAsync(user.Id, cancellationToken);
    }

    private async Task<AppUser> RequireIndependentHomeOwnerAsync(CancellationToken cancellationToken)
    {
        var user = await store.GetUserAsync(currentUser.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("The current user profile was not found.");
        if (user.Status == UserStatus.Disabled)
        {
            throw new UnauthorizedAccessException("The current user account is disabled.");
        }

        var memberships = await store.GetMembershipsForUserAsync(user.Id, cancellationToken);
        if (memberships.Any(m => m.Status == MembershipStatus.Active || m.Status == MembershipStatus.Pending))
        {
            throw new UnauthorizedAccessException("Independent homeowner access is required.");
        }

        return user;
    }

    private async Task<IndependentHomeOwnerProfile> GetOrCreateProfileAsync(string userId, CancellationToken cancellationToken)
    {
        var profile = await store.GetIndependentHomeOwnerProfileAsync(userId, cancellationToken);
        if (profile is not null)
        {
            var photos = await store.GetHomeOwnerPoolEquipmentPhotosAsync(userId, cancellationToken);
            return profile with { PoolEquipmentPhotos = photos };
        }

        var now = DateTimeOffset.UtcNow;
        profile = new IndependentHomeOwnerProfile(userId, "", "", now, now);
        await store.UpsertIndependentHomeOwnerProfileAsync(profile, cancellationToken);
        return profile with { PoolEquipmentPhotos = [] };
    }

    private async Task<ServiceOffering> GetRequiredHomeOwnerServiceAsync(
        string userId,
        string serviceId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(serviceId))
        {
            throw new InvalidOperationException("Choose a service before saving the service history item.");
        }

        return (await store.GetServicesAsync(userId, cancellationToken))
            .FirstOrDefault(service => string.Equals(service.Id, serviceId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Service was not found.");
    }

    private static PoolEquipmentOverview BuildPoolEquipmentGroups(
        EquipmentScope scope,
        string scopeOwnerId,
        IReadOnlyList<PoolEquipmentCategory> categories,
        IReadOnlyList<PoolEquipmentItem> items)
    {
        var knownCategories = categories
            .OrderByDescending(c => c.IsActive)
            .ThenBy(c => c.Name)
            .ToList();
        var categoryIds = knownCategories.Select(c => c.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var groups = knownCategories
            .Select(category => new PoolEquipmentCategoryGroup(
                category,
                items
                    .Where(item => string.Equals(item.CategoryId, category.Id, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(item => item.Name)
                    .ToList()))
            .ToList();

        var uncategorized = items
            .Where(item => string.IsNullOrWhiteSpace(item.CategoryId) || !categoryIds.Contains(item.CategoryId))
            .OrderBy(item => item.Name)
            .ToList();

        if (uncategorized.Count > 0)
        {
            groups.Add(new PoolEquipmentCategoryGroup(
                new PoolEquipmentCategory("uncategorized-equipment", scope, scopeOwnerId, "", "Uncategorized Equipment", "Equipment without an assigned category.", false, true),
                uncategorized));
        }

        return new PoolEquipmentOverview(groups);
    }
}

public sealed class CompanyAdminService(
    IServiceBusinessStore store,
    TenantAuthorizationService authorization,
    ICurrentUserContext currentUser,
    INotificationQueue notificationQueue)
{
    public async Task<CompanyDashboard> GetDashboardAsync(string companyId, DateOnly date, CancellationToken cancellationToken = default)
    {
        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyAdmin, cancellationToken);

        var company = await store.GetCompanyAsync(companyId, cancellationToken)
            ?? throw new InvalidOperationException("Company was not found.");
        var memberships = await store.GetMembershipsForCompanyAsync(companyId, cancellationToken);
        var clients = await store.GetClientsAsync(companyId, cancellationToken);
        var users = await store.GetUsersAsync(cancellationToken);
        var roles = (await store.GetRoleDefinitionsAsync(cancellationToken))
            .Select(NormalizeCompanyRoleDefinition)
            .ToList();
        var equipmentItems = await store.GetPoolEquipmentItemsAsync(EquipmentScope.Company, companyId, cancellationToken);
        var materials = await store.GetMaterialsAsync(companyId, cancellationToken);
        var services = await store.GetServicesAsync(companyId, cancellationToken);
        var pendingAccessRequests = BuildAccessRequests(company, users, roles, memberships);

        return new CompanyDashboard(
            company,
            CustomerCount: clients.Count(c => c.IsActive),
            EmployeeCount: memberships.Count(m => m.Role == CompanyRole.CompanyUser && m.Status == MembershipStatus.Active),
            EquipmentCount: equipmentItems.Count(i => i.IsActive),
            MaterialCount: materials.Count(m => m.IsActive),
            ServiceCount: services.Count(s => s.IsActive),
            PendingEmployeeRequests: pendingAccessRequests.Count(r => r.Membership.Role == CompanyRole.CompanyUser),
            PendingCustomerRequests: pendingAccessRequests.Count(r => r.Membership.Role == CompanyRole.CompanyClientUser),
            PendingAccessRequests: pendingAccessRequests);
    }

    public async Task<IReadOnlyList<CompanyClient>> GetClientsAsync(string companyId, CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminOrAnyCompanyRoleAsync(companyId, [CompanyRole.CompanyAdmin], cancellationToken);
        return await store.GetClientsAsync(companyId, cancellationToken);
    }

    public async Task<IReadOnlyList<PoolConfigurationClientRow>> GetPoolConfigurationClientsAsync(string companyId, CancellationToken cancellationToken = default)
    {
        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyAdmin, cancellationToken);

        var clients = await store.GetClientsAsync(companyId, cancellationToken);
        return clients
            .Where(client => client.IsActive)
            .OrderBy(client => client.ServiceAddress)
            .ThenBy(client => client.DisplayName)
            .Select(client => new PoolConfigurationClientRow(
                client.Id,
                "",
                client.ServiceAddress,
                "Business Client"))
            .ToList();
    }

    public async Task<IReadOnlyList<ClientType>> GetClientTypesAsync(string companyId, CancellationToken cancellationToken = default)
    {
        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyAdmin, cancellationToken);
        return await BusinessClientTypeReferenceData.EnsureForCompanyAsync(store, companyId, cancellationToken);
    }

    public async Task UpsertClientAsync(CompanyClient client, CancellationToken cancellationToken = default)
    {
        await authorization.RequireCompanyRoleAsync(client.CompanyId, CompanyRole.CompanyAdmin, cancellationToken);

        await ValidateClientAsync(client, cancellationToken);
        await store.UpsertClientAsync(NormalizeClient(client), cancellationToken);
    }

    private async Task ValidateClientAsync(CompanyClient client, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(client.Id))
        {
            throw new InvalidOperationException("Business client ID is required.");
        }

        if (string.IsNullOrWhiteSpace(client.DisplayName))
        {
            throw new InvalidOperationException("Business client name is required.");
        }

        if (string.IsNullOrWhiteSpace(client.PrimaryContactName))
        {
            throw new InvalidOperationException("Primary contact is required.");
        }

        if (string.IsNullOrWhiteSpace(client.Email) || !client.Email.Contains('@', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Business client email must be valid.");
        }

        if (string.IsNullOrWhiteSpace(client.ServiceAddress))
        {
            throw new InvalidOperationException("Service address is required.");
        }

        var clientTypes = await BusinessClientTypeReferenceData.EnsureForCompanyAsync(store, client.CompanyId, cancellationToken);
        if (!clientTypes.Any(t => t.Id == client.ClientTypeId && t.IsActive))
        {
            throw new InvalidOperationException("An active business client type is required.");
        }

        await ValidateBusinessClientServicePackageAsync(client, cancellationToken);
    }

    private static CompanyClient NormalizeClient(CompanyClient client) => client with
    {
        Id = CreateSlug(client.Id),
        CompanyId = client.CompanyId.Trim(),
        DisplayName = client.DisplayName.Trim(),
        PrimaryContactName = client.PrimaryContactName.Trim(),
        Email = client.Email.Trim().ToLowerInvariant(),
        Phone = string.IsNullOrWhiteSpace(client.Phone) ? "" : client.Phone.Trim(),
        ServiceAddress = client.ServiceAddress.Trim(),
        AccessNotes = string.IsNullOrWhiteSpace(client.AccessNotes) ? "" : client.AccessNotes.Trim(),
        ClientTypeId = client.ClientTypeId.Trim(),
        ServicePackageId = string.IsNullOrWhiteSpace(client.ServicePackageId) ? null : client.ServicePackageId.Trim()
    };

    private async Task ValidateBusinessClientServicePackageAsync(CompanyClient client, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(client.ServicePackageId))
        {
            return;
        }

        var company = await store.GetCompanyAsync(client.CompanyId, cancellationToken)
            ?? throw new InvalidOperationException("Service client was not found.");
        var companyType = (await store.GetCompanyTypesAsync(cancellationToken))
            .FirstOrDefault(type => string.Equals(type.Id, company.CompanyTypeId, StringComparison.OrdinalIgnoreCase));
        var globalCatalogCompanyId = GlobalCatalogScope.For(companyType is null ? SystemMode.Pool : GetSystemModeForCompanyType(companyType));
        var allowedPackages = (await store.GetServicePackagesAsync(company.Id, cancellationToken))
            .Concat(await store.GetServicePackagesAsync(globalCatalogCompanyId, cancellationToken));

        if (!allowedPackages.Any(package => package.IsActive && string.Equals(package.Id, client.ServicePackageId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Service package was not found.");
        }
    }

    private static SystemMode GetSystemModeForCompanyType(CompanyType companyType)
    {
        var searchable = $"{companyType.Id} {companyType.Name} {companyType.Description}";
        return searchable.Contains("landscape", StringComparison.OrdinalIgnoreCase) ||
            searchable.Contains("landscaping", StringComparison.OrdinalIgnoreCase) ||
            searchable.Contains("lawn", StringComparison.OrdinalIgnoreCase) ||
            searchable.Contains("yard", StringComparison.OrdinalIgnoreCase) ||
            searchable.Contains("tree", StringComparison.OrdinalIgnoreCase)
                ? SystemMode.Landscape
                : SystemMode.Pool;
    }

    public async Task<IReadOnlyList<ServiceOffering>> GetServicesAsync(string companyId, CancellationToken cancellationToken = default)
    {
        await authorization.RequireAnyCompanyRoleAsync(companyId, [CompanyRole.CompanyAdmin, CompanyRole.CompanyUser], cancellationToken);
        return await store.GetServicesAsync(companyId, cancellationToken);
    }

    public async Task<IReadOnlyList<Material>> GetMaterialsAsync(string companyId, CancellationToken cancellationToken = default)
    {
        await authorization.RequireAnyCompanyRoleAsync(companyId, [CompanyRole.CompanyAdmin, CompanyRole.CompanyUser], cancellationToken);
        return await store.GetMaterialsAsync(companyId, cancellationToken);
    }

    public async Task<CatalogOverview> GetCatalogOverviewAsync(string companyId, CancellationToken cancellationToken = default)
    {
        await RequireCatalogReadAsync(companyId, cancellationToken);

        var serviceCategories = await store.GetServiceCategoriesAsync(companyId, cancellationToken);
        var materialCategories = await store.GetMaterialCategoriesAsync(companyId, cancellationToken);
        var services = await store.GetServicesAsync(companyId, cancellationToken);
        var materials = await store.GetMaterialsAsync(companyId, cancellationToken);

        return new CatalogOverview(
            BuildServiceGroups(companyId, serviceCategories, services),
            BuildMaterialGroups(companyId, materialCategories, materials));
    }

    public async Task<PoolEquipmentOverview> GetPoolEquipmentOverviewAsync(EquipmentScope scope, string scopeOwnerId, CancellationToken cancellationToken = default)
    {
        await RequireEquipmentAccessAsync(scope, scopeOwnerId, manage: false, cancellationToken);

        var categories = await store.GetPoolEquipmentCategoriesAsync(scope, scopeOwnerId, cancellationToken);
        var items = await store.GetPoolEquipmentItemsAsync(scope, scopeOwnerId, cancellationToken);

        return new PoolEquipmentOverview(BuildPoolEquipmentGroups(scope, scopeOwnerId, categories, items));
    }

    public async Task<IReadOnlyList<HomeOwnerPoolEquipmentPhoto>> GetPoolConfigurationPhotosAsync(string scopeOwnerId, CancellationToken cancellationToken = default)
    {
        await RequireEquipmentAccessAsync(EquipmentScope.HomeOwner, scopeOwnerId, manage: false, cancellationToken);
        return await store.GetHomeOwnerPoolEquipmentPhotosAsync(scopeOwnerId, cancellationToken);
    }

    public async Task<IReadOnlyList<HomeOwnerPoolEquipmentPhoto>> AddPoolConfigurationPhotosAsync(
        string scopeOwnerId,
        IReadOnlyList<HomeOwnerPoolEquipmentPhoto> photos,
        CancellationToken cancellationToken = default)
    {
        await RequireEquipmentAccessAsync(EquipmentScope.HomeOwner, scopeOwnerId, manage: true, cancellationToken);
        foreach (var photo in photos)
        {
            await store.UpsertHomeOwnerPoolEquipmentPhotoAsync(scopeOwnerId, photo, cancellationToken);
        }

        return await store.GetHomeOwnerPoolEquipmentPhotosAsync(scopeOwnerId, cancellationToken);
    }

    public async Task<IReadOnlyList<HomeOwnerPoolEquipmentPhoto>> DeletePoolConfigurationPhotoAsync(
        string scopeOwnerId,
        string photoId,
        CancellationToken cancellationToken = default)
    {
        await RequireEquipmentAccessAsync(EquipmentScope.HomeOwner, scopeOwnerId, manage: true, cancellationToken);
        await store.DeleteHomeOwnerPoolEquipmentPhotoAsync(scopeOwnerId, photoId, cancellationToken);
        return await store.GetHomeOwnerPoolEquipmentPhotosAsync(scopeOwnerId, cancellationToken);
    }

    public async Task UpsertPoolEquipmentCategoryAsync(PoolEquipmentCategory category, CancellationToken cancellationToken = default)
    {
        await RequireEquipmentAccessAsync(category.Scope, category.ScopeOwnerId, manage: true, cancellationToken);
        ValidateCategory(category.Id, category.Name);

        await store.UpsertPoolEquipmentCategoryAsync(category with
        {
            Id = CreateSlug(category.Id),
            ScopeOwnerId = category.ScopeOwnerId.Trim(),
            Manufacturer = string.IsNullOrWhiteSpace(category.Manufacturer) ? "" : category.Manufacturer.Trim(),
            Name = category.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(category.Description) ? "" : category.Description.Trim()
        }, cancellationToken);
    }

    public async Task<PoolEquipmentCategory> CopyPoolEquipmentCategoryAsync(
        EquipmentScope scope,
        string scopeOwnerId,
        string categoryId,
        CancellationToken cancellationToken = default)
    {
        await RequireEquipmentAccessAsync(scope, scopeOwnerId, manage: true, cancellationToken);
        var categories = await store.GetPoolEquipmentCategoriesAsync(scope, scopeOwnerId, cancellationToken);
        var source = categories.FirstOrDefault(c => c.Id == categoryId)
            ?? throw new InvalidOperationException("Equipment category was not found.");
        var copy = source with
        {
            Id = CreateCopyId(source.Id, categories.Select(c => c.Id)),
            Name = $"{source.Name} Custom",
            IsSystemManaged = false,
            IsActive = true
        };

        await store.UpsertPoolEquipmentCategoryAsync(copy, cancellationToken);
        return copy;
    }

    public async Task SetPoolEquipmentCategoryActiveAsync(EquipmentScope scope, string scopeOwnerId, string categoryId, bool isActive, CancellationToken cancellationToken = default)
    {
        await RequireEquipmentAccessAsync(scope, scopeOwnerId, manage: true, cancellationToken);
        var category = (await store.GetPoolEquipmentCategoriesAsync(scope, scopeOwnerId, cancellationToken)).FirstOrDefault(c => c.Id == categoryId)
            ?? throw new InvalidOperationException("Equipment category was not found.");

        await store.UpsertPoolEquipmentCategoryAsync(category with { IsActive = isActive }, cancellationToken);
    }

    public async Task UpsertPoolEquipmentItemAsync(PoolEquipmentItem item, CancellationToken cancellationToken = default)
    {
        await RequireEquipmentAccessAsync(item.Scope, item.ScopeOwnerId, manage: true, cancellationToken);

        if (string.IsNullOrWhiteSpace(item.Id))
        {
            throw new InvalidOperationException("Equipment item ID is required.");
        }

        if (string.IsNullOrWhiteSpace(item.Name))
        {
            throw new InvalidOperationException("Equipment item name is required.");
        }

        if (!string.IsNullOrWhiteSpace(item.CategoryId))
        {
            await EnsureEquipmentCategoryExistsAsync(item.Scope, item.ScopeOwnerId, item.CategoryId, cancellationToken);
        }

        await store.UpsertPoolEquipmentItemAsync(item with
        {
            Id = CreateSlug(item.Id),
            ScopeOwnerId = item.ScopeOwnerId.Trim(),
            CategoryId = string.IsNullOrWhiteSpace(item.CategoryId) ? "" : item.CategoryId.Trim(),
            Name = item.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(item.Description) ? "" : item.Description.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(item.ImageUrl) ? null : item.ImageUrl.Trim(),
            ModelNo = string.IsNullOrWhiteSpace(item.ModelNo) ? "" : item.ModelNo.Trim(),
            Manufacturer = string.IsNullOrWhiteSpace(item.Manufacturer) ? "" : item.Manufacturer.Trim(),
            Comment = string.IsNullOrWhiteSpace(item.Comment) ? "" : item.Comment.Trim()
        }, cancellationToken);
    }

    public async Task<PoolEquipmentSeedResult> SeedPoolEquipmentAsync(
        EquipmentScope scope,
        string scopeOwnerId,
        IReadOnlyList<PoolEquipmentSeedRow> rows,
        CancellationToken cancellationToken = default)
    {
        await RequireEquipmentAccessAsync(scope, scopeOwnerId, manage: true, cancellationToken);

        if (rows.Count == 0)
        {
            throw new InvalidOperationException("Seed file did not include any equipment rows.");
        }

        var categoriesByKey = new Dictionary<string, PoolEquipmentCategory>(StringComparer.OrdinalIgnoreCase);
        var itemsById = new Dictionary<string, PoolEquipmentItem>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Manufacturer) ||
                string.IsNullOrWhiteSpace(row.Category) ||
                string.IsNullOrWhiteSpace(row.Name))
            {
                throw new InvalidOperationException("Manufacturer, Category, and Name are required for every seed row.");
            }

            var manufacturer = row.Manufacturer.Trim();
            var categoryName = row.Category.Trim();
            var equipmentName = row.Name.Trim();
            var modelNo = string.IsNullOrWhiteSpace(row.ModelNo) ? "" : row.ModelNo.Trim();
            var categoryId = CreateSlug(categoryName);
            var itemId = CreateSlug(string.IsNullOrWhiteSpace(modelNo)
                ? $"{manufacturer}-{equipmentName}"
                : $"{manufacturer}-{equipmentName}-{modelNo}");

            categoriesByKey[categoryId] = new PoolEquipmentCategory(
                categoryId,
                scope,
                scopeOwnerId,
                "",
                categoryName,
                $"{categoryName} seeded from equipment catalog.",
                scope == EquipmentScope.Global,
                true);

            itemsById[itemId] = new PoolEquipmentItem(
                itemId,
                scope,
                scopeOwnerId,
                categoryId,
                equipmentName,
                "",
                null,
                true,
                modelNo,
                manufacturer);
        }

        foreach (var category in categoriesByKey.Values)
        {
            await UpsertPoolEquipmentCategoryAsync(category, cancellationToken);
        }

        foreach (var item in itemsById.Values)
        {
            await UpsertPoolEquipmentItemAsync(item, cancellationToken);
        }

        return new PoolEquipmentSeedResult(categoriesByKey.Count, itemsById.Count);
    }

    public async Task<PoolEquipmentItem> CopyPoolEquipmentItemAsync(
        EquipmentScope scope,
        string scopeOwnerId,
        string itemId,
        CancellationToken cancellationToken = default)
    {
        await RequireEquipmentAccessAsync(scope, scopeOwnerId, manage: true, cancellationToken);
        var items = await store.GetPoolEquipmentItemsAsync(scope, scopeOwnerId, cancellationToken);
        var source = items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException("Equipment item was not found.");
        var copy = source with
        {
            Id = CreateCopyId(source.Id, items.Select(i => i.Id)),
            Name = $"{source.Name} Custom",
            IsActive = true
        };

        await store.UpsertPoolEquipmentItemAsync(copy, cancellationToken);
        return copy;
    }

    public async Task SetPoolEquipmentItemActiveAsync(EquipmentScope scope, string scopeOwnerId, string itemId, bool isActive, CancellationToken cancellationToken = default)
    {
        await RequireEquipmentAccessAsync(scope, scopeOwnerId, manage: true, cancellationToken);
        var item = (await store.GetPoolEquipmentItemsAsync(scope, scopeOwnerId, cancellationToken)).FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException("Equipment item was not found.");

        await store.UpsertPoolEquipmentItemAsync(item with { IsActive = isActive }, cancellationToken);
    }

    public async Task DeletePoolEquipmentItemAsync(EquipmentScope scope, string scopeOwnerId, string itemId, CancellationToken cancellationToken = default)
    {
        await RequireEquipmentAccessAsync(scope, scopeOwnerId, manage: true, cancellationToken);
        var exists = (await store.GetPoolEquipmentItemsAsync(scope, scopeOwnerId, cancellationToken))
            .Any(i => i.Id == itemId);
        if (!exists)
        {
            throw new InvalidOperationException("Equipment item was not found.");
        }

        await store.DeletePoolEquipmentItemAsync(scope, scopeOwnerId, itemId, cancellationToken);
    }

    public async Task UpsertMaterialCategoryAsync(MaterialCategory category, CancellationToken cancellationToken = default)
    {
        await RequireCatalogManagementAsync(category.CompanyId, cancellationToken);
        ValidateCategory(category.Id, category.Name);

        await store.UpsertMaterialCategoryAsync(category with
        {
            Id = CreateSlug(category.Id),
            CompanyId = category.CompanyId.Trim(),
            Name = category.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(category.Description) ? "" : category.Description.Trim()
        }, cancellationToken);
    }

    public async Task<MaterialCategory> CopyMaterialCategoryAsync(string companyId, string categoryId, CancellationToken cancellationToken = default)
    {
        await RequireCatalogManagementAsync(companyId, cancellationToken);
        var categories = await store.GetMaterialCategoriesAsync(companyId, cancellationToken);
        var source = categories.FirstOrDefault(c => c.Id == categoryId)
            ?? throw new InvalidOperationException("Material category was not found.");
        var copy = source with
        {
            Id = CreateCopyId(source.Id, categories.Select(c => c.Id)),
            Name = $"{source.Name} Custom",
            IsSystemManaged = false,
            IsActive = true
        };

        await store.UpsertMaterialCategoryAsync(copy, cancellationToken);
        return copy;
    }

    public async Task SetMaterialCategoryActiveAsync(string companyId, string categoryId, bool isActive, CancellationToken cancellationToken = default)
    {
        await RequireCatalogManagementAsync(companyId, cancellationToken);
        var category = (await store.GetMaterialCategoriesAsync(companyId, cancellationToken)).FirstOrDefault(c => c.Id == categoryId)
            ?? throw new InvalidOperationException("Material category was not found.");

        await store.UpsertMaterialCategoryAsync(category with { IsActive = isActive }, cancellationToken);
    }

    public async Task UpsertMaterialAsync(Material material, CancellationToken cancellationToken = default)
    {
        await RequireCatalogManagementAsync(material.CompanyId, cancellationToken);

        if (string.IsNullOrWhiteSpace(material.Id))
        {
            throw new InvalidOperationException("Material ID is required.");
        }

        if (string.IsNullOrWhiteSpace(material.Name))
        {
            throw new InvalidOperationException("Material name is required.");
        }

        if (string.IsNullOrWhiteSpace(material.UnitOfMeasure))
        {
            throw new InvalidOperationException("Unit of measure is required.");
        }

        if (material.DefaultUnitCost < 0 || material.DefaultBillableUnitPrice < 0)
        {
            throw new InvalidOperationException("Material costs and prices must be zero or greater.");
        }

        await EnsureCategoryExistsAsync(material.CompanyId, material.CategoryId, material: true, cancellationToken);
        await store.UpsertMaterialAsync(material with
        {
            Id = CreateSlug(material.Id),
            CompanyId = material.CompanyId.Trim(),
            CategoryId = string.IsNullOrWhiteSpace(material.CategoryId) ? null : material.CategoryId.Trim(),
            Name = material.Name.Trim(),
            UnitOfMeasure = material.UnitOfMeasure.Trim(),
            Brand = string.IsNullOrWhiteSpace(material.Brand) ? "" : material.Brand.Trim(),
            ModelNo = string.IsNullOrWhiteSpace(material.ModelNo) ? "" : material.ModelNo.Trim(),
            Description = string.IsNullOrWhiteSpace(material.Description) ? "" : material.Description.Trim()
        }, cancellationToken);
    }

    public async Task<MaterialSeedResult> SeedMaterialsAsync(
        string companyId,
        IReadOnlyList<MaterialSeedRow> rows,
        CancellationToken cancellationToken = default)
    {
        await RequireCatalogManagementAsync(companyId, cancellationToken);

        if (rows.Count == 0)
        {
            throw new InvalidOperationException("Seed file did not include any material rows.");
        }

        var categoriesByKey = new Dictionary<string, MaterialCategory>(StringComparer.OrdinalIgnoreCase);
        var materialsById = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Brand) ||
                string.IsNullOrWhiteSpace(row.Category) ||
                string.IsNullOrWhiteSpace(row.Name))
            {
                throw new InvalidOperationException("Brand, Category, and Name are required for every seed row.");
            }

            var brand = row.Brand.Trim();
            var categoryName = row.Category.Trim();
            var materialName = row.Name.Trim();
            var modelNo = string.IsNullOrWhiteSpace(row.ModelNo) ? "" : row.ModelNo.Trim();
            var categoryId = CreateSlug(categoryName);
            var materialId = CreateSlug(string.IsNullOrWhiteSpace(modelNo)
                ? $"{brand}-{materialName}"
                : $"{brand}-{materialName}-{modelNo}");

            categoriesByKey[categoryId] = new MaterialCategory(
                categoryId,
                companyId,
                categoryName,
                $"{categoryName} seeded from material catalog.",
                true,
                true);

            materialsById[materialId] = new Material(
                materialId,
                companyId,
                categoryId,
                materialName,
                "Each",
                0m,
                0m,
                true,
                true,
                brand,
                modelNo);
        }

        foreach (var category in categoriesByKey.Values)
        {
            await UpsertMaterialCategoryAsync(category, cancellationToken);
        }

        foreach (var material in materialsById.Values)
        {
            await UpsertMaterialAsync(material, cancellationToken);
        }

        return new MaterialSeedResult(categoriesByKey.Count, materialsById.Count);
    }

    public async Task<Material> CopyMaterialAsync(string companyId, string materialId, CancellationToken cancellationToken = default)
    {
        await RequireCatalogManagementAsync(companyId, cancellationToken);
        var materials = await store.GetMaterialsAsync(companyId, cancellationToken);
        var source = materials.FirstOrDefault(m => m.Id == materialId)
            ?? throw new InvalidOperationException("Material was not found.");
        var copy = source with
        {
            Id = CreateCopyId(source.Id, materials.Select(m => m.Id)),
            Name = $"{source.Name} Custom",
            IsActive = true
        };

        await store.UpsertMaterialAsync(copy, cancellationToken);
        return copy;
    }

    public async Task SetMaterialActiveAsync(string companyId, string materialId, bool isActive, CancellationToken cancellationToken = default)
    {
        await RequireCatalogManagementAsync(companyId, cancellationToken);
        var material = (await store.GetMaterialsAsync(companyId, cancellationToken)).FirstOrDefault(m => m.Id == materialId)
            ?? throw new InvalidOperationException("Material was not found.");

        await store.UpsertMaterialAsync(material with { IsActive = isActive }, cancellationToken);
    }

    public async Task UpsertServiceCategoryAsync(ServiceCategory category, CancellationToken cancellationToken = default)
    {
        await RequireCatalogManagementAsync(category.CompanyId, cancellationToken);
        ValidateCategory(category.Id, category.Name);

        await store.UpsertServiceCategoryAsync(category with
        {
            Id = CreateSlug(category.Id),
            CompanyId = category.CompanyId.Trim(),
            Name = category.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(category.Description) ? "" : category.Description.Trim()
        }, cancellationToken);
    }

    public async Task<ServiceCategory> CopyServiceCategoryAsync(string companyId, string categoryId, CancellationToken cancellationToken = default)
    {
        await RequireCatalogManagementAsync(companyId, cancellationToken);
        var categories = await store.GetServiceCategoriesAsync(companyId, cancellationToken);
        var source = categories.FirstOrDefault(c => c.Id == categoryId)
            ?? throw new InvalidOperationException("Service category was not found.");
        var copy = source with
        {
            Id = CreateCopyId(source.Id, categories.Select(c => c.Id)),
            Name = $"{source.Name} Custom",
            IsSystemManaged = false,
            IsActive = true
        };

        await store.UpsertServiceCategoryAsync(copy, cancellationToken);
        return copy;
    }

    public async Task SetServiceCategoryActiveAsync(string companyId, string categoryId, bool isActive, CancellationToken cancellationToken = default)
    {
        await RequireCatalogManagementAsync(companyId, cancellationToken);
        var category = (await store.GetServiceCategoriesAsync(companyId, cancellationToken)).FirstOrDefault(c => c.Id == categoryId)
            ?? throw new InvalidOperationException("Service category was not found.");

        await store.UpsertServiceCategoryAsync(category with { IsActive = isActive }, cancellationToken);
    }

    public async Task<ServiceSeedResult> SeedServicesAsync(
        string companyId,
        IReadOnlyList<ServiceSeedRow> rows,
        CancellationToken cancellationToken = default)
    {
        await RequireCatalogManagementAsync(companyId, cancellationToken);

        if (rows.Count == 0)
        {
            throw new InvalidOperationException("Seed file did not include any service rows.");
        }

        var categoriesByKey = new Dictionary<string, ServiceCategory>(StringComparer.OrdinalIgnoreCase);
        var servicesById = new Dictionary<string, ServiceOffering>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Category) ||
                string.IsNullOrWhiteSpace(row.Name))
            {
                throw new InvalidOperationException("Category and Name are required for every seed row.");
            }

            var categoryName = row.Category.Trim();
            var serviceName = row.Name.Trim();
            var description = string.IsNullOrWhiteSpace(row.Description) ? "" : row.Description.Trim();
            var categoryId = CreateSlug(categoryName);
            var serviceId = CreateSlug($"{categoryName}-{serviceName}");

            categoriesByKey[categoryId] = new ServiceCategory(
                categoryId,
                companyId,
                categoryName,
                $"{categoryName} seeded from service catalog.",
                true,
                true);

            servicesById[serviceId] = new ServiceOffering(
                serviceId,
                companyId,
                categoryId,
                serviceName,
                description,
                45,
                0m,
                true,
                true);
        }

        foreach (var category in categoriesByKey.Values)
        {
            await UpsertServiceCategoryAsync(category, cancellationToken);
        }

        foreach (var service in servicesById.Values)
        {
            await UpsertServiceAsync(service, cancellationToken);
        }

        return new ServiceSeedResult(categoriesByKey.Count, servicesById.Count);
    }

    public async Task UpsertServiceAsync(ServiceOffering service, CancellationToken cancellationToken = default)
    {
        await RequireCatalogManagementAsync(service.CompanyId, cancellationToken);

        if (string.IsNullOrWhiteSpace(service.Id))
        {
            throw new InvalidOperationException("Service ID is required.");
        }

        if (string.IsNullOrWhiteSpace(service.Name))
        {
            throw new InvalidOperationException("Service name is required.");
        }

        if (service.DefaultDurationMinutes <= 0)
        {
            throw new InvalidOperationException("Default duration must be greater than zero.");
        }

        if (service.DefaultPrice < 0)
        {
            throw new InvalidOperationException("Default price must be zero or greater.");
        }

        await EnsureCategoryExistsAsync(service.CompanyId, service.CategoryId, material: false, cancellationToken);
        await store.UpsertServiceAsync(service with
        {
            Id = CreateSlug(service.Id),
            CompanyId = service.CompanyId.Trim(),
            CategoryId = string.IsNullOrWhiteSpace(service.CategoryId) ? null : service.CategoryId.Trim(),
            Name = service.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(service.Description) ? "" : service.Description.Trim()
        }, cancellationToken);
    }

    public async Task<ServiceOffering> CopyServiceAsync(string companyId, string serviceId, CancellationToken cancellationToken = default)
    {
        await RequireCatalogManagementAsync(companyId, cancellationToken);
        var services = await store.GetServicesAsync(companyId, cancellationToken);
        var source = services.FirstOrDefault(s => s.Id == serviceId)
            ?? throw new InvalidOperationException("Service was not found.");
        var copy = source with
        {
            Id = CreateCopyId(source.Id, services.Select(s => s.Id)),
            Name = $"{source.Name} Custom",
            IsActive = true
        };

        await store.UpsertServiceAsync(copy, cancellationToken);
        return copy;
    }

    public async Task SetServiceActiveAsync(string companyId, string serviceId, bool isActive, CancellationToken cancellationToken = default)
    {
        await RequireCatalogManagementAsync(companyId, cancellationToken);
        var service = (await store.GetServicesAsync(companyId, cancellationToken)).FirstOrDefault(s => s.Id == serviceId)
            ?? throw new InvalidOperationException("Service was not found.");

        await store.UpsertServiceAsync(service with { IsActive = isActive }, cancellationToken);
    }

    public async Task DeleteServiceAsync(string companyId, string serviceId, CancellationToken cancellationToken = default)
    {
        await RequireCatalogManagementAsync(companyId, cancellationToken);
        var exists = (await store.GetServicesAsync(companyId, cancellationToken))
            .Any(s => s.Id == serviceId);
        if (!exists)
        {
            throw new InvalidOperationException("Service was not found.");
        }

        await store.DeleteServiceAsync(companyId, serviceId, cancellationToken);
    }

    public async Task<IReadOnlyList<ServicePackage>> GetServicePackagesAsync(string companyId, CancellationToken cancellationToken = default)
    {
        await RequireCatalogReadAsync(companyId, cancellationToken);
        return (await store.GetServicePackagesAsync(companyId, cancellationToken))
            .OrderBy(package => package.Name)
            .ToList();
    }

    public async Task UpsertServicePackageAsync(
        ServicePackage servicePackage,
        IReadOnlyList<string> accessibleServiceCompanyIds,
        CancellationToken cancellationToken = default)
    {
        await RequireCatalogManagementAsync(servicePackage.CompanyId, cancellationToken);

        if (string.IsNullOrWhiteSpace(servicePackage.Id))
        {
            throw new InvalidOperationException("Service package ID is required.");
        }

        if (string.IsNullOrWhiteSpace(servicePackage.Name))
        {
            throw new InvalidOperationException("Service package name is required.");
        }

        if (string.IsNullOrWhiteSpace(servicePackage.Recurrence))
        {
            throw new InvalidOperationException("Service package recurrence is required.");
        }

        var packageRecurrence = NormalizePackageRecurrence(servicePackage.Recurrence);

        if (servicePackage.Cost < 0)
        {
            throw new InvalidOperationException("Service package cost must be zero or greater.");
        }

        var accessibleServiceIds = await GetAccessibleServiceIdsAsync(accessibleServiceCompanyIds, cancellationToken);
        var normalizedServices = servicePackage.Services
            .Where(service => !string.IsNullOrWhiteSpace(service.ServiceId))
            .GroupBy(service => service.ServiceId.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new ServicePackageService(
                group.Key,
                NormalizePackageServiceRecurrence(group.First().Recurrence)))
            .ToList();

        foreach (var service in normalizedServices)
        {
            if (!accessibleServiceIds.Contains(service.ServiceId))
            {
                throw new InvalidOperationException("Service package contains a service that is not accessible in this scope.");
            }
        }

        await store.UpsertServicePackageAsync(servicePackage with
        {
            Id = CreateSlug(servicePackage.Id),
            CompanyId = servicePackage.CompanyId.Trim(),
            Name = servicePackage.Name.Trim(),
            Recurrence = packageRecurrence,
            Description = string.IsNullOrWhiteSpace(servicePackage.Description) ? "" : servicePackage.Description.Trim(),
            Services = normalizedServices
        }, cancellationToken);
    }

    public async Task SetServicePackageActiveAsync(string companyId, string packageId, bool isActive, CancellationToken cancellationToken = default)
    {
        await RequireCatalogManagementAsync(companyId, cancellationToken);
        var servicePackage = (await store.GetServicePackagesAsync(companyId, cancellationToken)).FirstOrDefault(p => p.Id == packageId)
            ?? throw new InvalidOperationException("Service package was not found.");

        await store.UpsertServicePackageAsync(servicePackage with { IsActive = isActive }, cancellationToken);
    }

    public async Task DeleteServicePackageAsync(string companyId, string packageId, CancellationToken cancellationToken = default)
    {
        await RequireCatalogManagementAsync(companyId, cancellationToken);
        var exists = (await store.GetServicePackagesAsync(companyId, cancellationToken)).Any(p => p.Id == packageId);
        if (!exists)
        {
            throw new InvalidOperationException("Service package was not found.");
        }

        await store.DeleteServicePackageAsync(companyId, packageId, cancellationToken);
    }

    private async Task<HashSet<string>> GetAccessibleServiceIdsAsync(
        IReadOnlyList<string> companyIds,
        CancellationToken cancellationToken)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var companyId in companyIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (var service in await store.GetServicesAsync(companyId, cancellationToken))
            {
                ids.Add(service.Id);
            }
        }

        return ids;
    }

    private static string NormalizePackageRecurrence(string recurrence)
    {
        var normalized = recurrence.Trim();
        var allowed = new[] { "Weekly", "Bi-Weekly", "Monthly", "Bi-Monthly", "Half-Yearly", "Yearly" };
        return allowed.FirstOrDefault(value => string.Equals(value, normalized, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Service package recurrence must be Weekly, Bi-Weekly, Monthly, Bi-Monthly, Half-Yearly, or Yearly.");
    }

    private static string NormalizePackageServiceRecurrence(string recurrence)
    {
        if (string.IsNullOrWhiteSpace(recurrence) || string.Equals(recurrence.Trim(), "Every Visit", StringComparison.OrdinalIgnoreCase))
        {
            return "Every Visit";
        }

        var normalized = recurrence.Trim();
        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 3 &&
            string.Equals(parts[0], "Every", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(parts[1], out var visitCount) &&
            visitCount > 0 &&
            string.Equals(parts[2], "Visits", StringComparison.OrdinalIgnoreCase))
        {
            return $"Every {visitCount} Visits";
        }

        throw new InvalidOperationException("Service recurrence must be Every Visit or Every X Visits.");
    }

    public async Task<IReadOnlyList<ServiceVisit>> GetScheduleAsync(string companyId, DateOnly date, CancellationToken cancellationToken = default)
    {
        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyAdmin, cancellationToken);
        return await store.GetVisitsByDateAsync(companyId, date, cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceVisit>> GetVisitsAsync(string companyId, CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminOrAnyCompanyRoleAsync(companyId, [CompanyRole.CompanyAdmin], cancellationToken);
        return await store.GetVisitsAsync(companyId, cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetInvoicesAsync(string companyId, CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminOrAnyCompanyRoleAsync(companyId, [CompanyRole.CompanyAdmin], cancellationToken);
        return await store.GetInvoicesAsync(companyId, cancellationToken);
    }

    public async Task SetInvoiceStatusAsync(string companyId, string invoiceId, InvoiceStatus status, CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminOrAnyCompanyRoleAsync(companyId, [CompanyRole.CompanyAdmin], cancellationToken);
        var invoice = await store.GetInvoiceAsync(companyId, invoiceId, cancellationToken)
            ?? throw new InvalidOperationException("Invoice was not found.");
        var allowed = (invoice.Status, status) switch
        {
            (InvoiceStatus.New, InvoiceStatus.Invoiced) => true,
            (InvoiceStatus.Invoiced, InvoiceStatus.Paid) => true,
            _ when invoice.Status == status => true,
            _ => false
        };
        if (!allowed)
        {
            throw new InvalidOperationException("Invoice status can only move from New to Invoiced to Paid.");
        }

        await store.UpsertInvoiceAsync(invoice with { Status = status }, cancellationToken);
    }

    public async Task UpsertVisitAsync(ServiceVisit visit, CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminOrAnyCompanyRoleAsync(visit.CompanyId, [CompanyRole.CompanyAdmin], cancellationToken);

        if (string.IsNullOrWhiteSpace(visit.Id))
        {
            throw new InvalidOperationException("Visit ID is required.");
        }

        if (string.IsNullOrWhiteSpace(visit.CompanyClientId))
        {
            throw new InvalidOperationException("Business client is required.");
        }

        var client = await store.GetClientAsync(visit.CompanyId, visit.CompanyClientId, cancellationToken)
            ?? throw new InvalidOperationException("Business client was not found.");
        if (!client.IsActive)
        {
            throw new InvalidOperationException("Business client is inactive.");
        }

        if (!string.IsNullOrWhiteSpace(visit.AssignedUserId))
        {
            await EnsureActiveVisitAssigneeAsync(visit.CompanyId, visit.AssignedUserId, cancellationToken);
        }

        var company = await store.GetCompanyAsync(visit.CompanyId, cancellationToken)
            ?? throw new InvalidOperationException("Service client was not found.");
        var companyType = (await store.GetCompanyTypesAsync(cancellationToken))
            .FirstOrDefault(type => string.Equals(type.Id, company.CompanyTypeId, StringComparison.OrdinalIgnoreCase));
        var globalCatalogCompanyId = GlobalCatalogScope.For(companyType is null ? SystemMode.Pool : GetSystemModeForCompanyType(companyType));
        var serviceIds = (await store.GetServicesAsync(visit.CompanyId, cancellationToken))
            .Concat(await store.GetServicesAsync(globalCatalogCompanyId, cancellationToken))
            .Where(service => service.IsActive)
            .Select(service => service.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var serviceId in visit.PlannedServiceIds.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            if (!serviceIds.Contains(serviceId))
            {
                throw new InvalidOperationException("Visit contains a service that is not active for the service client.");
            }
        }

        await store.UpsertVisitAsync(NormalizeVisit(visit), cancellationToken);
    }

    public async Task DeleteVisitAsync(string companyId, string visitId, CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminOrAnyCompanyRoleAsync(companyId, [CompanyRole.CompanyAdmin], cancellationToken);
        var visit = await store.GetVisitAsync(companyId, visitId, cancellationToken)
            ?? throw new InvalidOperationException("Visit was not found.");
        if (visit.Status is VisitStatus.Completed or VisitStatus.Closed)
        {
            throw new InvalidOperationException("Completed or closed visits cannot be deleted.");
        }

        await store.DeleteVisitAsync(companyId, visitId, cancellationToken);
    }

    public async Task SetVisitStatusAsync(string companyId, string visitId, VisitStatus status, CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminOrAnyCompanyRoleAsync(companyId, [CompanyRole.CompanyAdmin], cancellationToken);
        if (status is not VisitStatus.InProgress and not VisitStatus.Completed and not VisitStatus.Closed)
        {
            throw new InvalidOperationException("Only In Progress, Complete, or Closed can be set directly.");
        }

        var visit = await store.GetVisitAsync(companyId, visitId, cancellationToken)
            ?? throw new InvalidOperationException("Visit was not found.");
        await store.UpsertVisitAsync(visit with
        {
            Status = status,
            StartedUtc = status == VisitStatus.InProgress ? DateTimeOffset.UtcNow : visit.StartedUtc,
            CompletedUtc = status == VisitStatus.Completed && visit.CompletedUtc is null ? DateTimeOffset.UtcNow : visit.CompletedUtc
        }, cancellationToken);
    }

    public async Task AssignVisitAsync(string companyId, string visitId, string userId, CancellationToken cancellationToken = default)
    {
        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyAdmin, cancellationToken);

        await EnsureActiveVisitAssigneeAsync(companyId, userId, cancellationToken);

        var visit = await store.GetVisitAsync(companyId, visitId, cancellationToken)
            ?? throw new InvalidOperationException("Visit was not found.");

        await store.UpsertVisitAsync(visit with
        {
            AssignedUserId = userId,
            Status = visit.ScheduledDate == default ? VisitStatus.New : VisitStatus.Assigned
        }, cancellationToken);
    }

    private async Task EnsureActiveVisitAssigneeAsync(string companyId, string userId, CancellationToken cancellationToken)
    {
        var memberships = await store.GetMembershipsForCompanyAsync(companyId, cancellationToken);
        var assignee = memberships.FirstOrDefault(m =>
            m.UserId == userId &&
            m.Role is CompanyRole.CompanyUser or CompanyRole.CompanyAdmin &&
            m.Status == MembershipStatus.Active);

        if (assignee is null)
        {
            throw new InvalidOperationException("Visits can only be assigned to active business employees or business owners.");
        }
    }

    private static ServiceVisit NormalizeVisit(ServiceVisit visit)
    {
        var status = visit.Status;
        if (status is not VisitStatus.InProgress and not VisitStatus.Completed and not VisitStatus.Closed and not VisitStatus.Canceled and not VisitStatus.Skipped)
        {
            status = visit.ScheduledDate == default || string.IsNullOrWhiteSpace(visit.AssignedUserId)
                ? VisitStatus.New
                : VisitStatus.Assigned;
        }

        return visit with
        {
            Id = CreateSlug(visit.Id),
            CompanyId = visit.CompanyId.Trim(),
            CompanyClientId = visit.CompanyClientId.Trim(),
            AssignedUserId = string.IsNullOrWhiteSpace(visit.AssignedUserId) ? null : visit.AssignedUserId.Trim(),
            VisitName = string.IsNullOrWhiteSpace(visit.VisitName) ? "Service Visit" : visit.VisitName.Trim(),
            Notes = string.IsNullOrWhiteSpace(visit.Notes) ? "" : visit.Notes.Trim(),
            NotesToBusinessClient = string.IsNullOrWhiteSpace(visit.NotesToBusinessClient) ? "" : visit.NotesToBusinessClient.Trim(),
            NotesToServiceClient = string.IsNullOrWhiteSpace(visit.NotesToServiceClient) ? "" : visit.NotesToServiceClient.Trim(),
            InternalNotes = string.IsNullOrWhiteSpace(visit.InternalNotes) ? "" : visit.InternalNotes.Trim(),
            PlannedServiceIds = visit.PlannedServiceIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            OutOfScopeServiceIds = visit.OutOfScopeServiceIds?.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? [],
            OutOfScopeMaterials = visit.OutOfScopeMaterials ?? [],
            CompletedByUserId = string.IsNullOrWhiteSpace(visit.CompletedByUserId) ? null : visit.CompletedByUserId.Trim(),
            CompletedServiceIds = visit.CompletedServiceIds?.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? [],
            MaterialsUsed = visit.MaterialsUsed ?? [],
            Status = status
        };
    }

    public async Task<IReadOnlyList<AccessRequest>> GetPendingAccessRequestsAsync(
        string companyId,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyAdmin, cancellationToken);

        var company = await store.GetCompanyAsync(companyId, cancellationToken)
            ?? throw new InvalidOperationException("Company was not found.");
        var users = await store.GetUsersAsync(cancellationToken);
        var roles = (await store.GetRoleDefinitionsAsync(cancellationToken))
            .Select(NormalizeCompanyRoleDefinition)
            .ToList();
        var memberships = await store.GetMembershipsForCompanyAsync(companyId, cancellationToken);

        return BuildAccessRequests(company, users, roles, memberships);
    }

    public async Task<CompanyUserManagementOverview> GetCompanyUserManagementOverviewAsync(
        string companyId,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminOrAnyCompanyRoleAsync(companyId, [CompanyRole.CompanyAdmin], cancellationToken);

        var company = await store.GetCompanyAsync(companyId, cancellationToken)
            ?? throw new InvalidOperationException("Company was not found.");
        var users = await store.GetUsersAsync(cancellationToken);
        var roles = (await store.GetRoleDefinitionsAsync(cancellationToken))
            .Select(NormalizeCompanyRoleDefinition)
            .ToList();
        var memberships = await store.GetMembershipsForCompanyAsync(companyId, cancellationToken);
        var pending = BuildAccessRequests(company, users, roles, memberships);
        var rows = memberships
            .Where(m => m.Status is MembershipStatus.Pending or MembershipStatus.Active or MembershipStatus.Inactive or MembershipStatus.Removed)
            .OrderByDescending(m => m.Status == MembershipStatus.Active)
            .ThenByDescending(m => m.Status == MembershipStatus.Pending)
            .ThenBy(m => users.FirstOrDefault(u => u.Id == m.UserId)?.DisplayName ?? m.UserId)
            .Select(m => new CompanyUserManagementRow(
                users.First(u => u.Id == m.UserId),
                m,
                roles.First(r => r.Role == m.Role)))
            .ToList();

        return new CompanyUserManagementOverview(
            company,
            rows.Count(r => r.Membership.Status == MembershipStatus.Active),
            pending.Count,
            roles,
            rows,
            pending);
    }

    public async Task SetCompanyUserAccessStatusAsync(
        string companyId,
        string userId,
        CompanyRole role,
        MembershipStatus status,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyAdmin, cancellationToken);

        if (status is not MembershipStatus.Active and not MembershipStatus.Inactive)
        {
            throw new InvalidOperationException("Company user access can only be activated or deactivated.");
        }

        if (currentUser.UserId == userId && role == CompanyRole.CompanyAdmin && status == MembershipStatus.Inactive)
        {
            throw new InvalidOperationException("A company admin cannot deactivate their own company admin access.");
        }

        var memberships = await store.GetMembershipsForCompanyAsync(companyId, cancellationToken);
        var membership = memberships.FirstOrDefault(m => m.UserId == userId && m.Role == role)
            ?? throw new InvalidOperationException("Company user membership was not found.");

        if (membership.Status is not MembershipStatus.Active and not MembershipStatus.Inactive and not MembershipStatus.Removed)
        {
            throw new InvalidOperationException("Only approved company users can be activated or deactivated.");
        }

        if (membership.Role == CompanyRole.CompanyAdmin && status == MembershipStatus.Inactive)
        {
            EnsureAnotherActiveCompanyAdminExists(memberships, userId);
        }

        await store.UpsertMembershipAsync(membership with
        {
            Status = status,
            DecidedUtc = DateTimeOffset.UtcNow,
            DecidedByUserId = currentUser.UserId
        }, cancellationToken);
    }

    public async Task UpdateCompanyUserRoleAsync(
        string companyId,
        string userId,
        CompanyRole currentRole,
        CompanyRole newRole,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyAdmin, cancellationToken);

        if (currentUser.UserId == userId && currentRole == CompanyRole.CompanyAdmin && newRole != CompanyRole.CompanyAdmin)
        {
            throw new InvalidOperationException("A company admin cannot remove their own company admin role.");
        }

        var memberships = await store.GetMembershipsForCompanyAsync(companyId, cancellationToken);
        var membership = memberships.FirstOrDefault(m => m.UserId == userId && m.Role == currentRole)
            ?? throw new InvalidOperationException("Company user membership was not found.");

        if (membership.Status is not MembershipStatus.Active and not MembershipStatus.Inactive)
        {
            throw new InvalidOperationException("Only approved company users can have roles updated.");
        }

        if (currentRole == newRole)
        {
            return;
        }

        if (currentRole == CompanyRole.CompanyAdmin)
        {
            EnsureAnotherActiveCompanyAdminExists(memberships, userId);
        }

        await store.UpsertMembershipAsync(membership with
        {
            Status = MembershipStatus.Removed,
            DecidedUtc = DateTimeOffset.UtcNow,
            DecidedByUserId = currentUser.UserId
        }, cancellationToken);

        var replacement = memberships.FirstOrDefault(m => m.UserId == userId && m.Role == newRole);
        await store.UpsertMembershipAsync((replacement ?? new CompanyMembership(
            companyId,
            userId,
            newRole,
            membership.Status,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            currentUser.UserId)) with
        {
            Status = membership.Status,
            DecidedUtc = DateTimeOffset.UtcNow,
            DecidedByUserId = currentUser.UserId
        }, cancellationToken);
    }

    public async Task DecideAccessRequestAsync(
        string companyId,
        string userId,
        CompanyRole role,
        MembershipStatus decision,
        CancellationToken cancellationToken = default)
    {
        using var activity = ServiceBusinessTelemetry.ActivitySource.StartActivity("DecideAccessRequest");
        activity?.SetTag("company.id", companyId);
        activity?.SetTag("user.id", userId);
        activity?.SetTag("company.role", role.ToString());
        activity?.SetTag("membership.decision", decision.ToString());

        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyAdmin, cancellationToken);

        if (decision is not MembershipStatus.Active and not MembershipStatus.Rejected)
        {
            throw new InvalidOperationException("Access requests can only be approved or rejected.");
        }

        var memberships = await store.GetMembershipsForCompanyAsync(companyId, cancellationToken);
        var membership = memberships.FirstOrDefault(m =>
            m.UserId == userId &&
            m.Role == role &&
            m.Status == MembershipStatus.Pending)
            ?? throw new InvalidOperationException("Pending access request was not found.");

        var decided = membership with
        {
            Status = decision,
            DecidedUtc = DateTimeOffset.UtcNow,
            DecidedByUserId = currentUser.UserId
        };

        await store.UpsertMembershipAsync(decided, cancellationToken);

        var request = (await GetAccessRequestAsync(companyId, userId, role, cancellationToken))
            ?? throw new InvalidOperationException("Access request was not found after update.");
        await notificationQueue.QueueAccountApprovalDecisionEmailAsync(request, decision, cancellationToken);
        ServiceBusinessTelemetry.AccountApprovalDecisions.Add(1);
    }

    private async Task<AccessRequest?> GetAccessRequestAsync(
        string companyId,
        string userId,
        CompanyRole role,
        CancellationToken cancellationToken)
    {
        var company = await store.GetCompanyAsync(companyId, cancellationToken);
        var user = await store.GetUserAsync(userId, cancellationToken);
        var roleDefinition = (await store.GetRoleDefinitionsAsync(cancellationToken))
            .Select(NormalizeCompanyRoleDefinition)
            .FirstOrDefault(r => r.Role == role);
        var membership = (await store.GetMembershipsForCompanyAsync(companyId, cancellationToken))
            .FirstOrDefault(m => m.UserId == userId && m.Role == role);

        return company is null || user is null || roleDefinition is null || membership is null
            ? null
            : new AccessRequest(membership, user, company, roleDefinition);
    }

    private static IReadOnlyList<AccessRequest> BuildAccessRequests(
        Company company,
        IReadOnlyList<AppUser> users,
        IReadOnlyList<RoleDefinition> roles,
        IReadOnlyList<CompanyMembership> memberships) =>
        memberships
            .Where(m => m.Status == MembershipStatus.Pending)
            .OrderBy(m => m.RequestedUtc)
            .Select(m => new AccessRequest(
                m,
                users.First(u => u.Id == m.UserId),
                company,
                roles.First(r => r.Role == m.Role)))
            .ToList();

    private static RoleDefinition NormalizeCompanyRoleDefinition(RoleDefinition roleDefinition)
    {
        if (roleDefinition.Permissions is { Count: > 0 })
        {
            return roleDefinition;
        }

        var permissions = roleDefinition.Role switch
        {
            CompanyRole.CompanyAdmin => new[] { "catalog.manage", "clients.manage", "company.configure", "reports.view", "schedule.manage", "users.approve" },
            CompanyRole.CompanyUser => ["materials.record", "visits.assigned.view", "visits.complete", "visits.start"],
            CompanyRole.CompanyClientUser => ["client.billing.view", "client.history.view", "client.messages.create"],
            _ => []
        };

        return roleDefinition with { Permissions = permissions };
    }

    private static void EnsureAnotherActiveCompanyAdminExists(
        IReadOnlyList<CompanyMembership> memberships,
        string userId)
    {
        var anotherAdminExists = memberships.Any(m =>
            m.UserId != userId &&
            m.Role == CompanyRole.CompanyAdmin &&
            m.Status == MembershipStatus.Active);

        if (!anotherAdminExists)
        {
            throw new InvalidOperationException("At least one active company admin is required.");
        }
    }

    private async Task RequireCatalogManagementAsync(string companyId, CancellationToken cancellationToken)
    {
        if (await IsIndependentHomeOwnerCatalogAsync(companyId, cancellationToken))
        {
            return;
        }

        await authorization.RequireSystemAdminOrAnyCompanyRoleAsync(companyId, [CompanyRole.CompanyAdmin], cancellationToken);
    }

    private async Task RequireCatalogReadAsync(string companyId, CancellationToken cancellationToken)
    {
        if (GlobalCatalogScope.IsGlobal(companyId))
        {
            await authorization.RequireCurrentUserAsync(cancellationToken);
            return;
        }

        if (await IsIndependentHomeOwnerCatalogAsync(companyId, cancellationToken))
        {
            return;
        }

        await authorization.RequireSystemAdminOrAnyCompanyRoleAsync(companyId, [CompanyRole.CompanyAdmin, CompanyRole.CompanyUser], cancellationToken);
    }

    private async Task<bool> IsIndependentHomeOwnerCatalogAsync(string ownerId, CancellationToken cancellationToken)
    {
        var user = await authorization.RequireCurrentUserAsync(cancellationToken);
        if (!string.Equals(user.Id, ownerId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var memberships = await store.GetMembershipsForUserAsync(user.Id, cancellationToken);
        return !user.IsSystemAdmin && !memberships.Any(m => m.Status is MembershipStatus.Active or MembershipStatus.Pending);
    }

    private async Task RequireEquipmentAccessAsync(
        EquipmentScope scope,
        string scopeOwnerId,
        bool manage,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(scopeOwnerId))
        {
            throw new InvalidOperationException("Equipment scope owner is required.");
        }

        switch (scope)
        {
            case EquipmentScope.Global:
                if (manage)
                {
                    await authorization.RequireSystemAdminAsync(cancellationToken);
                }
                else
                {
                    await authorization.RequireCurrentUserAsync(cancellationToken);
                }
                break;
            case EquipmentScope.Company:
                await authorization.RequireSystemAdminOrAnyCompanyRoleAsync(
                    scopeOwnerId,
                    manage ? [CompanyRole.CompanyAdmin] : [CompanyRole.CompanyAdmin, CompanyRole.CompanyUser],
                    cancellationToken);
                break;
            case EquipmentScope.HomeOwner:
                var user = await authorization.RequireCurrentUserAsync(cancellationToken);
                if (user.IsSystemAdmin)
                {
                    break;
                }

                if (string.Equals(user.Id, scopeOwnerId, StringComparison.OrdinalIgnoreCase) &&
                    await IsIndependentHomeOwnerCatalogAsync(scopeOwnerId, cancellationToken))
                {
                    break;
                }

                var owningCompany = await GetCompanyForClientAsync(scopeOwnerId, cancellationToken);
                if (owningCompany is not null)
                {
                    await authorization.RequireCompanyRoleAsync(owningCompany.Id, CompanyRole.CompanyAdmin, cancellationToken);
                    break;
                }

                if (!string.Equals(user.Id, scopeOwnerId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException("You can only manage your own homeowner equipment catalog.");
                }

                throw new UnauthorizedAccessException("Pool configuration access is available only to independent homeowners or the business owner for the selected client.");

            default:
                throw new InvalidOperationException("Unsupported equipment scope.");
        }
    }

    private async Task<Company?> GetCompanyForClientAsync(string clientId, CancellationToken cancellationToken)
    {
        var companies = await store.GetCompaniesAsync(cancellationToken);
        foreach (var company in companies)
        {
            var clients = await store.GetClientsAsync(company.Id, cancellationToken);
            if (clients.Any(client => string.Equals(client.Id, clientId, StringComparison.OrdinalIgnoreCase)))
            {
                return company;
            }
        }

        return null;
    }

    private static void ValidateCategory(string id, string name)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException("Category ID is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Category name is required.");
        }
    }

    private async Task EnsureCategoryExistsAsync(
        string companyId,
        string? categoryId,
        bool material,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
        {
            return;
        }

        var exists = material
            ? (await store.GetMaterialCategoriesAsync(companyId, cancellationToken)).Any(c => c.Id == categoryId)
            : (await store.GetServiceCategoriesAsync(companyId, cancellationToken)).Any(c => c.Id == categoryId);

        if (!exists)
        {
            throw new InvalidOperationException("Selected category was not found.");
        }
    }

    private async Task EnsureEquipmentCategoryExistsAsync(
        EquipmentScope scope,
        string scopeOwnerId,
        string categoryId,
        CancellationToken cancellationToken)
    {
        var exists = (await store.GetPoolEquipmentCategoriesAsync(scope, scopeOwnerId, cancellationToken))
            .Any(c => string.Equals(c.Id, categoryId, StringComparison.OrdinalIgnoreCase));

        if (!exists)
        {
            throw new InvalidOperationException("Selected equipment category was not found.");
        }
    }

    private static string CreateSlug(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        var slug = string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(slug) ? $"item-{Guid.NewGuid():N}" : slug;
    }

    private static string CreateCopyId(string sourceId, IEnumerable<string> existingIds)
    {
        var existing = existingIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var baseId = $"{CreateSlug(sourceId)}-custom";
        if (!existing.Contains(baseId))
        {
            return baseId;
        }

        for (var index = 2; index < 1000; index++)
        {
            var candidate = $"{baseId}-{index}";
            if (!existing.Contains(candidate))
            {
                return candidate;
            }
        }

        return $"{baseId}-{Guid.NewGuid():N}";
    }

    private static IReadOnlyList<ServiceCategoryGroup> BuildServiceGroups(
        string companyId,
        IReadOnlyList<ServiceCategory> categories,
        IReadOnlyList<ServiceOffering> services)
    {
        var knownCategories = categories
            .OrderByDescending(c => c.IsActive)
            .ThenBy(c => c.Name)
            .ToList();
        var categoryIds = knownCategories.Select(c => c.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var groups = knownCategories
            .Select(category => new ServiceCategoryGroup(
                category,
                services
                    .Where(service => string.Equals(service.CategoryId, category.Id, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(service => service.Name)
                    .ToList()))
            .ToList();

        var uncategorized = services
            .Where(service => string.IsNullOrWhiteSpace(service.CategoryId) || !categoryIds.Contains(service.CategoryId))
            .OrderBy(service => service.Name)
            .ToList();

        if (uncategorized.Count > 0)
        {
            groups.Add(new ServiceCategoryGroup(
                new ServiceCategory("uncategorized-services", companyId, "Uncategorized Services", "Services without an assigned category.", false, true),
                uncategorized));
        }

        return groups;
    }

    private static IReadOnlyList<MaterialCategoryGroup> BuildMaterialGroups(
        string companyId,
        IReadOnlyList<MaterialCategory> categories,
        IReadOnlyList<Material> materials)
    {
        var knownCategories = categories
            .OrderByDescending(c => c.IsActive)
            .ThenBy(c => c.Name)
            .ToList();
        var categoryIds = knownCategories.Select(c => c.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var groups = knownCategories
            .Select(category => new MaterialCategoryGroup(
                category,
                materials
                    .Where(material => string.Equals(material.CategoryId, category.Id, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(material => material.Name)
                    .ToList()))
            .ToList();

        var uncategorized = materials
            .Where(material => string.IsNullOrWhiteSpace(material.CategoryId) || !categoryIds.Contains(material.CategoryId))
            .OrderBy(material => material.Name)
            .ToList();

        if (uncategorized.Count > 0)
        {
            groups.Add(new MaterialCategoryGroup(
                new MaterialCategory("uncategorized-materials", companyId, "Uncategorized Materials", "Materials without an assigned category.", false, true),
                uncategorized));
        }

        return groups;
    }

    private static IReadOnlyList<PoolEquipmentCategoryGroup> BuildPoolEquipmentGroups(
        EquipmentScope scope,
        string scopeOwnerId,
        IReadOnlyList<PoolEquipmentCategory> categories,
        IReadOnlyList<PoolEquipmentItem> items)
    {
        var knownCategories = categories
            .OrderByDescending(c => c.IsActive)
            .ThenBy(c => c.Name)
            .ToList();
        var categoryIds = knownCategories.Select(c => c.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var groups = knownCategories
            .Select(category => new PoolEquipmentCategoryGroup(
                category,
                items
                    .Where(item => string.Equals(item.CategoryId, category.Id, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(item => item.Name)
                    .ToList()))
            .ToList();

        var uncategorized = items
            .Where(item => string.IsNullOrWhiteSpace(item.CategoryId) || !categoryIds.Contains(item.CategoryId))
            .OrderBy(item => item.Name)
            .ToList();

        if (uncategorized.Count > 0)
        {
            groups.Add(new PoolEquipmentCategoryGroup(
                new PoolEquipmentCategory("uncategorized-equipment", scope, scopeOwnerId, "", "Uncategorized Equipment", "Equipment without an assigned category.", false, true),
                uncategorized));
        }

        return groups;
    }
}

public sealed class FieldWorkService(
    IServiceBusinessStore store,
    TenantAuthorizationService authorization,
    ICurrentUserContext currentUser,
    INotificationQueue notificationQueue)
{
    public async Task<IReadOnlyList<ServiceHistoryItem>> GetAssignedVisitsAsync(
        string companyId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyUser, cancellationToken);

        var visits = await store.GetVisitsForUserByDateAsync(companyId, currentUser.UserId, date, cancellationToken);
        var clients = await store.GetClientsAsync(companyId, cancellationToken);

        return visits
            .OrderBy(v => v.RouteOrder)
            .Select(visit => new ServiceHistoryItem(
                visit,
                clients.First(c => c.Id == visit.CompanyClientId),
                null))
            .ToList();
    }

    public async Task<IReadOnlyList<ServiceHistoryItem>> GetTodayAssignedVisitsAsync(
        string companyId,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyUser, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var visits = (await store.GetVisitsForUserByDateAsync(companyId, currentUser.UserId, today, cancellationToken))
            .Where(visit => visit.Status is VisitStatus.Assigned or VisitStatus.InProgress)
            .OrderBy(visit => visit.ServiceWindowStart)
            .ThenBy(visit => visit.RouteOrder)
            .ToList();
        var clients = await store.GetClientsAsync(companyId, cancellationToken);

        return visits
            .Select(visit => new ServiceHistoryItem(
                visit,
                clients.First(c => c.Id == visit.CompanyClientId),
                null))
            .ToList();
    }

    public async Task<IReadOnlyList<ServiceHistoryItem>> GetUpcomingAssignedVisitsAsync(
        string companyId,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyUser, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var visits = (await store.GetVisitsAsync(companyId, cancellationToken))
            .Where(visit =>
                string.Equals(visit.AssignedUserId, currentUser.UserId, StringComparison.OrdinalIgnoreCase) &&
                visit.Status is VisitStatus.Assigned or VisitStatus.InProgress &&
                (visit.ScheduledDate == default || visit.ScheduledDate > today))
            .OrderBy(visit => visit.ScheduledDate == default ? DateOnly.MaxValue : visit.ScheduledDate)
            .ThenBy(visit => visit.ServiceWindowStart)
            .ThenBy(visit => visit.RouteOrder)
            .ToList();
        var clients = await store.GetClientsAsync(companyId, cancellationToken);

        return visits
            .Select(visit => new ServiceHistoryItem(
                visit,
                clients.First(c => c.Id == visit.CompanyClientId),
                null))
            .ToList();
    }

    public async Task<IReadOnlyList<ServiceHistoryItem>> GetRecentlyCompletedAssignedVisitsAsync(
        string companyId,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyUser, cancellationToken);

        var visits = (await store.GetVisitsAsync(companyId, cancellationToken))
            .Where(visit =>
                visit.Status == VisitStatus.Completed &&
                (string.Equals(visit.AssignedUserId, currentUser.UserId, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(visit.CompletedByUserId, currentUser.UserId, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(visit => visit.CompletedUtc ?? new DateTimeOffset(visit.ScheduledDate == default ? DateTime.MinValue : visit.ScheduledDate.ToDateTime(TimeOnly.MinValue)))
            .Take(25)
            .ToList();
        var clients = await store.GetClientsAsync(companyId, cancellationToken);

        return visits
            .Select(visit => new ServiceHistoryItem(
                visit,
                clients.First(c => c.Id == visit.CompanyClientId),
                null))
            .ToList();
    }

    public async Task StartVisitAsync(string companyId, string visitId, CancellationToken cancellationToken = default)
    {
        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyUser, cancellationToken);

        var visit = await store.GetVisitAsync(companyId, visitId, cancellationToken)
            ?? throw new InvalidOperationException("Visit was not found.");

        if (visit.AssignedUserId != currentUser.UserId)
        {
            throw new UnauthorizedAccessException("Only the assigned user can start this visit.");
        }

        await store.UpsertVisitAsync(visit with
        {
            Status = VisitStatus.InProgress,
            StartedUtc = DateTimeOffset.UtcNow
        }, cancellationToken);
    }

    public async Task MarkVisitInProgressAsync(string companyId, string visitId, CancellationToken cancellationToken = default)
    {
        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyUser, cancellationToken);

        var visit = await store.GetVisitAsync(companyId, visitId, cancellationToken)
            ?? throw new InvalidOperationException("Visit was not found.");

        if (!string.Equals(visit.AssignedUserId, currentUser.UserId, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(visit.CompletedByUserId, currentUser.UserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Only the assigned user can update this visit.");
        }

        if (visit.Status != VisitStatus.Completed)
        {
            throw new InvalidOperationException("Only completed visits can be moved back to in progress.");
        }

        await store.UpsertVisitAsync(visit with
        {
            Status = VisitStatus.InProgress,
            StartedUtc = DateTimeOffset.UtcNow
        }, cancellationToken);
    }

    public async Task UpdateAssignedVisitDetailsAsync(
        string companyId,
        string visitId,
        string notesToBusinessClient,
        string notesToServiceClient,
        string internalNotes,
        IReadOnlyList<string> completedServiceIds,
        IReadOnlyList<string> outOfScopeServiceIds,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyUser, cancellationToken);

        var visit = await store.GetVisitAsync(companyId, visitId, cancellationToken)
            ?? throw new InvalidOperationException("Visit was not found.");

        if (!string.Equals(visit.AssignedUserId, currentUser.UserId, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(visit.CompletedByUserId, currentUser.UserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Only the assigned user can edit this visit.");
        }

        if (visit.Status is VisitStatus.Closed or VisitStatus.Canceled or VisitStatus.Skipped)
        {
            throw new InvalidOperationException("Closed, canceled, or skipped visits cannot be edited.");
        }

        var plannedIds = visit.PlannedServiceIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var normalizedCompletedIds = completedServiceIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (normalizedCompletedIds.Any(id => !plannedIds.Contains(id)))
        {
            throw new InvalidOperationException("Completed services must be part of the planned visit services.");
        }

        var activeServiceIds = (await store.GetServicesAsync(companyId, cancellationToken))
            .Concat(await store.GetServicesAsync(GlobalCatalogScope.Pool, cancellationToken))
            .Concat(await store.GetServicesAsync(GlobalCatalogScope.Landscape, cancellationToken))
            .Where(service => service.IsActive)
            .Select(service => service.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var normalizedOutOfScopeIds = outOfScopeServiceIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (normalizedOutOfScopeIds.Any(id => plannedIds.Contains(id) || !activeServiceIds.Contains(id)))
        {
            throw new InvalidOperationException("Out-of-scope services must be active services that are not already planned for the visit.");
        }

        await store.UpsertVisitAsync(visit with
        {
            NotesToBusinessClient = string.IsNullOrWhiteSpace(notesToBusinessClient) ? "" : notesToBusinessClient.Trim(),
            NotesToServiceClient = string.IsNullOrWhiteSpace(notesToServiceClient) ? "" : notesToServiceClient.Trim(),
            InternalNotes = string.IsNullOrWhiteSpace(internalNotes) ? "" : internalNotes.Trim(),
            CompletedServiceIds = normalizedCompletedIds,
            OutOfScopeServiceIds = normalizedOutOfScopeIds
        }, cancellationToken);
    }

    public async Task CompleteVisitAsync(
        string companyId,
        string visitId,
        IReadOnlyList<string> serviceIds,
        IReadOnlyList<MaterialUsage> materials,
        string customerNotes,
        string internalNotes,
        CancellationToken cancellationToken = default)
    {
        using var activity = ServiceBusinessTelemetry.ActivitySource.StartActivity("CompleteVisit");
        activity?.SetTag("company.id", companyId);
        activity?.SetTag("visit.id", visitId);
        activity?.SetTag("service.count", serviceIds.Count);
        activity?.SetTag("material.count", materials.Count);

        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyUser, cancellationToken);

        if (serviceIds.Count == 0 && string.IsNullOrWhiteSpace(customerNotes) && string.IsNullOrWhiteSpace(internalNotes))
        {
            throw new InvalidOperationException("A completed visit needs at least one service or note.");
        }

        var visit = await store.GetVisitAsync(companyId, visitId, cancellationToken)
            ?? throw new InvalidOperationException("Visit was not found.");

        if (visit.AssignedUserId != currentUser.UserId)
        {
            throw new UnauthorizedAccessException("Only the assigned user can complete this visit.");
        }

        var completedUtc = DateTimeOffset.UtcNow;
        var completedVisit = visit with
        {
            Status = VisitStatus.Completed,
            CompletedUtc = completedUtc,
            CompletedByUserId = currentUser.UserId,
            CompletedServiceIds = serviceIds,
            MaterialsUsed = materials,
            NotesToBusinessClient = string.IsNullOrWhiteSpace(customerNotes) ? visit.NotesToBusinessClient : customerNotes.Trim(),
            InternalNotes = string.IsNullOrWhiteSpace(internalNotes) ? visit.InternalNotes : internalNotes.Trim()
        };

        await store.UpsertVisitAsync(completedVisit, cancellationToken);

        var client = await store.GetClientAsync(companyId, visit.CompanyClientId, cancellationToken)
            ?? throw new InvalidOperationException("Client was not found.");
        var user = await store.GetUserAsync(currentUser.UserId, cancellationToken);

        await notificationQueue.QueueVisitCompletedEmailAsync(
            new ServiceHistoryItem(completedVisit, client, user),
            cancellationToken);
        ServiceBusinessTelemetry.CompletedVisits.Add(1);
    }
}

public sealed class InvoicingJobService(IServiceBusinessStore store)
{
    public async Task<IReadOnlyList<Invoice>> CreateInvoicesForCompletedVisitsAsync(CancellationToken cancellationToken = default)
    {
        var created = new List<Invoice>();
        var companies = await store.GetCompaniesAsync(cancellationToken);

        foreach (var company in companies.Where(company => company.Status == CompanyStatus.Active))
        {
            var visits = await store.GetVisitsAsync(company.Id, cancellationToken);
            foreach (var visit in visits.Where(visit => visit.Status == VisitStatus.Completed && string.IsNullOrWhiteSpace(visit.InvoiceId)))
            {
                var client = await store.GetClientAsync(company.Id, visit.CompanyClientId, cancellationToken);
                if (client is null)
                {
                    continue;
                }

                var invoice = await BuildInvoiceAsync(company, client, visit, cancellationToken);
                await store.UpsertInvoiceAsync(invoice, cancellationToken);
                await store.UpsertVisitAsync(visit with { InvoiceId = invoice.InvoiceId }, cancellationToken);
                await QueueInvoiceEmailAsync(company, client, invoice, cancellationToken);
                created.Add(invoice);
            }
        }

        return created;
    }

    private async Task<Invoice> BuildInvoiceAsync(
        Company company,
        CompanyClient client,
        ServiceVisit visit,
        CancellationToken cancellationToken)
    {
        var serviceCatalog = await GetAccessibleServicesAsync(company, cancellationToken);
        var materialCatalog = await GetAccessibleMaterialsAsync(company, cancellationToken);
        var billableServiceIds = visit.VisitType == VisitType.AdHocVisit
            ? visit.PlannedServiceIds.Concat(visit.OutOfScopeServiceIds ?? [])
            : visit.OutOfScopeServiceIds ?? [];
        var serviceLines = billableServiceIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(id =>
            {
                var service = serviceCatalog.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
                return new InvoiceServiceLine(
                    id,
                    service?.Name ?? id,
                    service?.DefaultPrice ?? 0m);
            })
            .ToList();
        var materialLines = (visit.OutOfScopeMaterials ?? [])
            .Where(material => !string.IsNullOrWhiteSpace(material.MaterialId) && material.Quantity > 0)
            .Select(material =>
            {
                var catalogItem = materialCatalog.FirstOrDefault(m => string.Equals(m.Id, material.MaterialId, StringComparison.OrdinalIgnoreCase));
                var unitAmount = catalogItem?.DefaultBillableUnitPrice ?? 0m;
                return new InvoiceMaterialLine(
                    material.MaterialId,
                    catalogItem?.Name ?? material.MaterialId,
                    catalogItem?.UnitOfMeasure ?? "Each",
                    material.Quantity,
                    unitAmount,
                    material.Quantity * unitAmount);
            })
            .ToList();
        var servicePackageId = visit.VisitType == VisitType.ServicePackageVisit
            ? client.ServicePackageId ?? company.ServicePackageId
            : null;
        var invoiceId = await GetNextInvoiceIdAsync(company.Id, cancellationToken);
        var total = serviceLines.Sum(line => line.Amount) + materialLines.Sum(line => line.Amount);
        var invoice = new Invoice(
            Guid.NewGuid().ToString("N"),
            company.Id,
            invoiceId,
            client.Id,
            visit.Id,
            servicePackageId,
            serviceLines,
            materialLines,
            total,
            InvoiceStatus.New,
            "",
            DateTimeOffset.UtcNow);

        return invoice with { InvoiceHtml = BuildInvoiceHtml(company, client, visit, invoice) };
    }

    private async Task<string> GetNextInvoiceIdAsync(string companyId, CancellationToken cancellationToken)
    {
        var invoices = await store.GetInvoicesAsync(companyId, cancellationToken);
        var next = invoices
            .Select(invoice => int.TryParse(invoice.InvoiceId, out var value) ? value : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;
        return next.ToString("D6");
    }

    private async Task<IReadOnlyList<ServiceOffering>> GetAccessibleServicesAsync(Company company, CancellationToken cancellationToken)
    {
        var globalCatalogCompanyId = GlobalCatalogScope.For(await GetSystemModeForCompanyAsync(company, cancellationToken));
        return (await store.GetServicesAsync(company.Id, cancellationToken))
            .Concat(await store.GetServicesAsync(globalCatalogCompanyId, cancellationToken))
            .Where(service => service.IsActive)
            .GroupBy(service => service.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private async Task<IReadOnlyList<Material>> GetAccessibleMaterialsAsync(Company company, CancellationToken cancellationToken)
    {
        var globalCatalogCompanyId = GlobalCatalogScope.For(await GetSystemModeForCompanyAsync(company, cancellationToken));
        return (await store.GetMaterialsAsync(company.Id, cancellationToken))
            .Concat(await store.GetMaterialsAsync(globalCatalogCompanyId, cancellationToken))
            .Where(material => material.IsActive)
            .GroupBy(material => material.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private async Task<SystemMode> GetSystemModeForCompanyAsync(Company company, CancellationToken cancellationToken)
    {
        var companyType = (await store.GetCompanyTypesAsync(cancellationToken))
            .FirstOrDefault(type => string.Equals(type.Id, company.CompanyTypeId, StringComparison.OrdinalIgnoreCase));
        var searchable = companyType is null
            ? company.CompanyTypeId
            : $"{companyType.Id} {companyType.Name} {companyType.Description}";
        return searchable.Contains("landscape", StringComparison.OrdinalIgnoreCase) ||
            searchable.Contains("lawn", StringComparison.OrdinalIgnoreCase) ||
            searchable.Contains("yard", StringComparison.OrdinalIgnoreCase) ||
            searchable.Contains("tree", StringComparison.OrdinalIgnoreCase)
            ? SystemMode.Landscape
            : SystemMode.Pool;
    }

    private async Task QueueInvoiceEmailAsync(
        Company company,
        CompanyClient client,
        Invoice invoice,
        CancellationToken cancellationToken)
    {
        var log = new EmailLogEntry(
            $"invoice-{invoice.InvoiceGuid}",
            company.Id,
            "Invoice",
            client.Id,
            client.Email,
            client.Email,
            $"Invoice {invoice.InvoiceId} from {company.Name}",
            invoice.InvoiceHtml,
            EmailDeliveryStatus.New,
            null,
            null,
            DateTimeOffset.UtcNow,
            null,
            company.BusinessEmail,
            "");

        await store.UpsertEmailLogAsync(log, cancellationToken);
    }

    private static string BuildInvoiceHtml(Company company, CompanyClient client, ServiceVisit visit, Invoice invoice)
    {
        var serviceRows = invoice.AdditionalServices.Count == 0
            ? "<tr><td colspan=\"3\">No additional services.</td></tr>"
            : string.Concat(invoice.AdditionalServices.Select(line =>
                $"<tr><td>{HtmlEncode(line.Name)}</td><td>Service</td><td>{line.Amount:C}</td></tr>"));
        var materialRows = invoice.Materials.Count == 0
            ? "<tr><td colspan=\"3\">No additional materials.</td></tr>"
            : string.Concat(invoice.Materials.Select(line =>
                $"<tr><td>{HtmlEncode(line.Name)} ({line.Quantity} {HtmlEncode(line.Unit)})</td><td>Material</td><td>{line.Amount:C}</td></tr>"));

        return $"""
        <html>
        <body>
            <h1>Invoice {HtmlEncode(invoice.InvoiceId)}</h1>
            <p><strong>{HtmlEncode(company.Name)}</strong></p>
            <p>Bill To: {HtmlEncode(client.DisplayName)}<br />{HtmlEncode(client.ServiceAddress)}</p>
            <p>Visit: {HtmlEncode(string.IsNullOrWhiteSpace(visit.VisitName) ? visit.Id : visit.VisitName)} on {(visit.ScheduledDate == default ? "Unscheduled" : visit.ScheduledDate)}</p>
            <table>
                <thead><tr><th>Item</th><th>Type</th><th>Amount</th></tr></thead>
                <tbody>{serviceRows}{materialRows}</tbody>
            </table>
            <h2>Total: {invoice.TotalCost:C}</h2>
        </body>
        </html>
        """;
    }

    private static string HtmlEncode(string? value) => WebUtility.HtmlEncode(value ?? "");
}

public sealed class EmailJobService(IServiceBusinessStore store)
{
    public async Task<int> ProcessNewEmailLogsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await store.GetSystemSettingsAsync(cancellationToken);
        var logs = (await store.GetEmailLogsAsync(cancellationToken))
            .Where(log => log.Status == EmailDeliveryStatus.New)
            .OrderBy(log => log.CreatedUtc)
            .ToList();
        var processed = 0;

        foreach (var log in logs)
        {
            var updated = settings.DevTest || IsValidEmail(log.RecipientEmail)
                ? log with
                {
                    Status = EmailDeliveryStatus.Sent,
                    SentUtc = DateTimeOffset.UtcNow,
                    FailureReason = null
                }
                : log with
                {
                    Status = EmailDeliveryStatus.Failed,
                    FailureReason = "Recipient email address is not valid."
                };

            await store.UpsertEmailLogAsync(updated, cancellationToken);
            processed++;
        }

        return processed;
    }

    private static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) && email.Contains('@', StringComparison.Ordinal);
}

public sealed class ClientPortalService(
    IServiceBusinessStore store,
    TenantAuthorizationService authorization,
    ICurrentUserContext currentUser)
{
    public async Task<CompanyClient> GetCurrentUserClientAsync(CancellationToken cancellationToken = default)
    {
        var context = await GetCurrentClientContextAsync(cancellationToken);
        return context.Client;
    }

    public async Task<ServicePackage?> GetCurrentUserServicePackageAsync(
        string globalCatalogCompanyId,
        CancellationToken cancellationToken = default)
    {
        var context = await GetCurrentClientContextAsync(cancellationToken);
        var company = await store.GetCompanyAsync(context.Membership.CompanyId, cancellationToken);
        var servicePackageId = context.Client.ServicePackageId ?? company?.ServicePackageId;
        if (string.IsNullOrWhiteSpace(servicePackageId))
        {
            return null;
        }

        var servicePackages = (await store.GetServicePackagesAsync(context.Membership.CompanyId, cancellationToken))
            .Concat(await store.GetServicePackagesAsync(globalCatalogCompanyId, cancellationToken));
        return servicePackages.FirstOrDefault(package => string.Equals(package.Id, servicePackageId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<PoolEquipmentOverview> GetCurrentUserPoolEquipmentOverviewAsync(
        CancellationToken cancellationToken = default)
    {
        var context = await GetCurrentClientContextAsync(cancellationToken);
        var categories = await store.GetPoolEquipmentCategoriesAsync(EquipmentScope.HomeOwner, context.Client.Id, cancellationToken);
        var items = await store.GetPoolEquipmentItemsAsync(EquipmentScope.HomeOwner, context.Client.Id, cancellationToken);
        return BuildPoolEquipmentGroups(EquipmentScope.HomeOwner, context.Client.Id, categories, items);
    }

    public async Task<IReadOnlyList<ServiceOffering>> GetCurrentUserServicesAsync(CancellationToken cancellationToken = default)
    {
        var context = await GetCurrentClientContextAsync(cancellationToken);
        return (await store.GetServicesAsync(context.Membership.CompanyId, cancellationToken))
            .Where(service => service.IsActive)
            .OrderBy(service => service.Name)
            .ToList();
    }

    public async Task<IReadOnlyList<ServiceHistoryItem>> GetCurrentUserServiceHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        var context = await GetCurrentClientContextAsync(cancellationToken);
        return await GetServiceHistoryAsync(context.Membership.CompanyId, context.Client.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceVisit>> GetCurrentUserVisitsAsync(
        CancellationToken cancellationToken = default)
    {
        var context = await GetCurrentClientContextAsync(cancellationToken);
        return (await store.GetVisitsForClientAsync(context.Membership.CompanyId, context.Client.Id, cancellationToken))
            .OrderBy(visit => visit.ScheduledDate == default ? DateOnly.MaxValue : visit.ScheduledDate)
            .ThenBy(visit => visit.ServiceWindowStart)
            .ToList();
    }

    public async Task UpdateCurrentUserVisitServiceProviderNotesAsync(
        string visitId,
        string? notesToServiceClient,
        CancellationToken cancellationToken = default)
    {
        var context = await GetCurrentClientContextAsync(cancellationToken);
        var visit = await store.GetVisitAsync(context.Membership.CompanyId, visitId, cancellationToken)
            ?? throw new InvalidOperationException("Visit was not found.");
        if (!string.Equals(visit.CompanyClientId, context.Client.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("The visit is not available to the current client.");
        }

        await store.UpsertVisitAsync(visit with
        {
            NotesToServiceClient = string.IsNullOrWhiteSpace(notesToServiceClient) ? "" : notesToServiceClient.Trim()
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<IndependentHomeOwnerServiceHistoryItem>> GetCurrentUserAddedServiceHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        var context = await GetCurrentClientContextAsync(cancellationToken);
        return (await store.GetIndependentHomeOwnerServiceHistoryAsync(context.Client.Id, cancellationToken))
            .Where(item => !item.IsDeleted)
            .OrderByDescending(item => item.ServiceDateTime)
            .ToList();
    }

    public async Task<IndependentHomeOwnerServiceHistoryItem> AddCurrentUserServiceHistoryItemAsync(
        string serviceId,
        DateOnly serviceDate,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var context = await GetCurrentClientContextAsync(cancellationToken);
        var service = await GetRequiredClientServiceAsync(context.Membership.CompanyId, serviceId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var item = new IndependentHomeOwnerServiceHistoryItem(
            $"client-service-{now:yyyyMMddHHmmssfff}",
            context.Client.Id,
            new DateTimeOffset(serviceDate.ToDateTime(TimeOnly.MinValue)),
            string.IsNullOrWhiteSpace(notes) ? "" : notes.Trim(),
            now,
            service.Id,
            service.Name);

        await store.UpsertIndependentHomeOwnerServiceHistoryItemAsync(item, cancellationToken);
        return item;
    }

    public async Task<IndependentHomeOwnerServiceHistoryItem> UpdateCurrentUserServiceHistoryItemAsync(
        string itemId,
        string serviceId,
        DateOnly serviceDate,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var context = await GetCurrentClientContextAsync(cancellationToken);
        var service = await GetRequiredClientServiceAsync(context.Membership.CompanyId, serviceId, cancellationToken);
        var existing = (await store.GetIndependentHomeOwnerServiceHistoryAsync(context.Client.Id, cancellationToken))
            .FirstOrDefault(item => item.Id == itemId && !item.IsDeleted)
            ?? throw new InvalidOperationException("Service history item was not found.");

        var updated = existing with
        {
            ServiceDateTime = new DateTimeOffset(serviceDate.ToDateTime(TimeOnly.MinValue)),
            Notes = string.IsNullOrWhiteSpace(notes) ? "" : notes.Trim(),
            ServiceId = service.Id,
            ServiceName = service.Name
        };

        await store.UpsertIndependentHomeOwnerServiceHistoryItemAsync(updated, cancellationToken);
        return updated;
    }

    public async Task DeleteCurrentUserServiceHistoryItemAsync(string itemId, CancellationToken cancellationToken = default)
    {
        var context = await GetCurrentClientContextAsync(cancellationToken);
        var existing = (await store.GetIndependentHomeOwnerServiceHistoryAsync(context.Client.Id, cancellationToken))
            .FirstOrDefault(item => item.Id == itemId && !item.IsDeleted)
            ?? throw new InvalidOperationException("Service history item was not found.");

        await store.UpsertIndependentHomeOwnerServiceHistoryItemAsync(existing with { IsDeleted = true }, cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceHistoryItem>> GetServiceHistoryAsync(
        string companyId,
        string clientId,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyClientUser, cancellationToken);

        var visits = await store.GetVisitsForClientAsync(companyId, clientId, cancellationToken);
        var client = await store.GetClientAsync(companyId, clientId, cancellationToken)
            ?? throw new InvalidOperationException("Client was not found.");
        var users = await store.GetUsersAsync(cancellationToken);

        return visits
            .Where(v => v.Status == VisitStatus.Completed)
            .OrderByDescending(v => v.CompletedUtc)
            .Select(visit => new ServiceHistoryItem(
                visit,
                client,
                users.FirstOrDefault(u => u.Id == (visit.CompletedByUserId ?? visit.AssignedUserId))))
            .ToList();
    }

    private static CompanyClient? ResolveClientForUser(
        CompanyMembership membership,
        AppUser user,
        IReadOnlyList<CompanyClient> clients)
    {
        if (!string.IsNullOrWhiteSpace(membership.CompanyClientId))
        {
            var selectedClient = clients.FirstOrDefault(c => string.Equals(c.Id, membership.CompanyClientId, StringComparison.OrdinalIgnoreCase));
            if (selectedClient is not null)
            {
                return selectedClient;
            }
        }

        var exact = clients.FirstOrDefault(c => c.Id == user.Id);
        if (exact is not null)
        {
            return exact;
        }

        var generatedClientPrefix = $"{membership.CompanyId}-client-";
        if (user.Id.StartsWith(generatedClientPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var suffix = user.Id[generatedClientPrefix.Length..];
            var generatedClient = clients.FirstOrDefault(c => c.Id == $"{membership.CompanyId}-home-{suffix}");
            if (generatedClient is not null)
            {
                return generatedClient;
            }
        }

        var demoClientPrefix = "demo-client-";
        if (user.Id.StartsWith(demoClientPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var suffix = user.Id[demoClientPrefix.Length..];
            var demoClient = clients.FirstOrDefault(c => c.Id == $"client-{suffix}");
            if (demoClient is not null)
            {
                return demoClient;
            }
        }

        return clients.FirstOrDefault(c => string.Equals(c.Email, user.Email, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<ClientPortalContext> GetCurrentClientContextAsync(CancellationToken cancellationToken)
    {
        var memberships = await store.GetMembershipsForUserAsync(currentUser.UserId, cancellationToken);
        var membership = memberships
            .Where(m =>
                m.Role == CompanyRole.CompanyClientUser &&
                m.Status == MembershipStatus.Active)
            .OrderBy(m => m.CompanyId)
            .FirstOrDefault()
            ?? throw new UnauthorizedAccessException("An active company client membership is required.");

        var user = await store.GetUserAsync(currentUser.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("The current user profile was not found.");
        var clients = await store.GetClientsAsync(membership.CompanyId, cancellationToken);
        var client = ResolveClientForUser(membership, user, clients)
            ?? throw new InvalidOperationException("A customer record was not found for the current client user.");

        return new ClientPortalContext(user, membership, client);
    }

    private async Task<ServiceOffering> GetRequiredClientServiceAsync(
        string companyId,
        string serviceId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(serviceId))
        {
            throw new InvalidOperationException("Choose a service.");
        }

        return (await store.GetServicesAsync(companyId, cancellationToken))
            .FirstOrDefault(service => service.IsActive && string.Equals(service.Id, serviceId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Choose an active service.");
    }

    private static PoolEquipmentOverview BuildPoolEquipmentGroups(
        EquipmentScope scope,
        string scopeOwnerId,
        IReadOnlyList<PoolEquipmentCategory> categories,
        IReadOnlyList<PoolEquipmentItem> items)
    {
        var knownCategories = categories
            .OrderByDescending(c => c.IsActive)
            .ThenBy(c => c.Name)
            .ToList();
        var categoryIds = knownCategories.Select(c => c.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var groups = knownCategories
            .Select(category => new PoolEquipmentCategoryGroup(
                category,
                items
                    .Where(item => string.Equals(item.CategoryId, category.Id, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(item => item.Name)
                    .ToList()))
            .ToList();

        var uncategorized = items
            .Where(item => string.IsNullOrWhiteSpace(item.CategoryId) || !categoryIds.Contains(item.CategoryId))
            .OrderBy(item => item.Name)
            .ToList();

        if (uncategorized.Count > 0)
        {
            groups.Add(new PoolEquipmentCategoryGroup(
                new PoolEquipmentCategory("uncategorized-equipment", scope, scopeOwnerId, "", "Uncategorized Equipment", "Equipment without an assigned category.", false, true),
                uncategorized));
        }

        return new PoolEquipmentOverview(groups);
    }

    private sealed record ClientPortalContext(
        AppUser User,
        CompanyMembership Membership,
        CompanyClient Client);
}

public sealed class OnboardingService(IServiceBusinessStore store)
{
    public async Task<IReadOnlyList<Company>> GetAvailableCompaniesAsync(CancellationToken cancellationToken = default)
    {
        var companies = await store.GetCompaniesAsync(cancellationToken);
        var companyTypes = await GetCompanyTypesForCurrentSystemModeAsync(cancellationToken);
        var companyTypeIds = companyTypes.Select(type => type.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return companies
            .Where(c => c.Status == CompanyStatus.Active && companyTypeIds.Contains(c.CompanyTypeId))
            .OrderBy(c => c.Name)
            .ToList();
    }

    public async Task<IReadOnlyList<CompanyClient>> GetAvailableBusinessClientsAsync(
        string companyId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(companyId))
        {
            return [];
        }

        var company = await store.GetCompanyAsync(companyId, cancellationToken);
        if (company is null || company.Status != CompanyStatus.Active)
        {
            return [];
        }

        var allowedCompanies = await GetAvailableCompaniesAsync(cancellationToken);
        if (!allowedCompanies.Any(c => string.Equals(c.Id, company.Id, StringComparison.OrdinalIgnoreCase)))
        {
            return [];
        }

        return (await store.GetClientsAsync(company.Id, cancellationToken))
            .Where(client => client.IsActive)
            .OrderBy(client => client.ServiceAddress)
            .ThenBy(client => client.DisplayName)
            .ToList();
    }

    public async Task<RegistrationResult> RegisterAsync(
        RegistrationSubmission submission,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(submission.Email))
        {
            throw new InvalidOperationException("A Gmail account email is required.");
        }

        if (!submission.Email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Registration currently requires a Gmail account.");
        }

        if (string.IsNullOrWhiteSpace(submission.DisplayName))
        {
            throw new InvalidOperationException("Display name is required.");
        }

        var user = await GetOrCreateUserAsync(submission, cancellationToken);

        return submission.AccountType switch
        {
            RegistrationAccountType.BusinessOwner => await RegisterBusinessOwnerAsync(user, submission, cancellationToken),
            RegistrationAccountType.BusinessUser => await RegisterCompanyScopedUserAsync(
                user,
                submission,
                CompanyRole.CompanyUser,
                "Your employee account request was submitted and is waiting for business owner approval.",
                cancellationToken),
            RegistrationAccountType.BusinessClient => await RegisterCompanyScopedUserAsync(
                user,
                submission,
                CompanyRole.CompanyClientUser,
                "Your client portal request was submitted and is waiting for business owner approval.",
                cancellationToken),
            RegistrationAccountType.IndependentHomeOwner => await RegisterIndependentHomeOwnerAsync(user, submission, cancellationToken),
            _ => throw new InvalidOperationException("Unsupported registration account type.")
        };
    }

    public async Task<UserAccessOverview?> SignInAsync(string userIdentifier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userIdentifier))
        {
            return null;
        }

        var trimmedIdentifier = userIdentifier.Trim();
        var user = await store.GetUserByEmailAsync(trimmedIdentifier, cancellationToken)
            ?? await store.GetUserAsync(trimmedIdentifier, cancellationToken);
        if (user is null)
        {
            return null;
        }

        if (user.Status == UserStatus.Disabled)
        {
            throw new InvalidOperationException("This user account is disabled.");
        }

        if (!user.IsTestUser && !user.Email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Non-test users must sign in with a Gmail account.");
        }

        var memberships = await store.GetMembershipsForUserAsync(user.Id, cancellationToken);
        var companies = await store.GetCompaniesAsync(cancellationToken);

        var access = memberships
            .Select(m => new CompanyAccess(
                companies.First(c => c.Id == m.CompanyId),
                m.Role,
                m.Status))
            .OrderBy(a => a.Company.Name)
            .ThenBy(a => a.Role)
            .ToList();

        return new UserAccessOverview(user, user.IsTestUser, access);
    }

    public async Task<UserAccessOverview> CompleteGoogleSignInAsync(
        GoogleUserProfile googleProfile,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(googleProfile.GoogleSubjectId))
        {
            throw new InvalidOperationException("Google did not return a stable subject identifier.");
        }

        if (string.IsNullOrWhiteSpace(googleProfile.Email))
        {
            throw new InvalidOperationException("Google did not return an email address.");
        }

        if (!googleProfile.Email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only Gmail accounts can sign in.");
        }

        var user = await store.GetUserByGoogleSubjectAsync(googleProfile.GoogleSubjectId, cancellationToken)
            ?? await store.GetUserByEmailAsync(googleProfile.Email, cancellationToken);

        if (user is null)
        {
            user = new AppUser(
                CreateSlug(googleProfile.Email.Split('@')[0]),
                googleProfile.GoogleSubjectId,
                googleProfile.Email.Trim().ToLowerInvariant(),
                googleProfile.Email.Trim().ToLowerInvariant(),
                googleProfile.DisplayName.Trim(),
                null,
                googleProfile.ProfileImageUrl,
                false,
                false,
                true,
                UserStatus.Active);
        }
        else
        {
            if (user.Status == UserStatus.Disabled)
            {
                throw new InvalidOperationException("This user account is disabled.");
            }

            user = user with
            {
                GoogleSubjectId = googleProfile.GoogleSubjectId,
                Email = googleProfile.Email.Trim().ToLowerInvariant(),
                NotificationEmail = string.IsNullOrWhiteSpace(user.NotificationEmail)
                    ? googleProfile.Email.Trim().ToLowerInvariant()
                    : user.NotificationEmail,
                EmailNotificationsEnabled = user.EmailNotificationsEnabled ?? true,
                DisplayName = string.IsNullOrWhiteSpace(googleProfile.DisplayName) ? user.DisplayName : googleProfile.DisplayName.Trim(),
                ProfileImageUrl = googleProfile.ProfileImageUrl
            };
        }

        await store.UpsertUserAsync(user, cancellationToken);
        return await GetAccessOverviewAsync(user.Id, cancellationToken);
    }

    public async Task<UserAccessOverview> GetAccessOverviewAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await store.GetUserAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");
        var memberships = await store.GetMembershipsForUserAsync(user.Id, cancellationToken);
        var companies = await store.GetCompaniesAsync(cancellationToken);

        return new UserAccessOverview(
            user,
            user.IsTestUser,
            memberships
                .Select(m => new CompanyAccess(
                    companies.First(c => c.Id == m.CompanyId),
                    m.Role,
                    m.Status))
                .OrderBy(a => a.Company.Name)
                .ThenBy(a => a.Role)
                .ToList());
    }

    public async Task<IndependentHomeOwnerProfile?> GetIndependentHomeOwnerProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await store.GetUserAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");

        return await store.GetIndependentHomeOwnerProfileAsync(user.Id, cancellationToken);
    }

    private async Task<AppUser> GetOrCreateUserAsync(
        RegistrationSubmission submission,
        CancellationToken cancellationToken)
    {
        var existing = await store.GetUserByEmailAsync(submission.Email, cancellationToken);
        if (existing is not null)
        {
            if (existing.IsSystemAdmin && submission.AccountType != RegistrationAccountType.BusinessOwner)
            {
                throw new InvalidOperationException("This email is already registered as a system administrator. Use a different Gmail account for homeowner or business user registration.");
            }

            var updated = existing with
            {
                DisplayName = submission.DisplayName.Trim(),
                Phone = string.IsNullOrWhiteSpace(submission.Phone) ? existing.Phone : submission.Phone.Trim(),
                IsTestUser = existing.IsTestUser || submission.AuthenticationSkipped
            };
            await store.UpsertUserAsync(updated, cancellationToken);
            return updated;
        }

        var user = new AppUser(
            CreateSlug(submission.Email.Split('@')[0]),
            null,
            submission.Email.Trim().ToLowerInvariant(),
            submission.Email.Trim().ToLowerInvariant(),
            submission.DisplayName.Trim(),
            submission.Phone.Trim(),
            null,
            false,
            submission.AuthenticationSkipped,
            true,
            UserStatus.Active);

        await store.UpsertUserAsync(user, cancellationToken);
        return user;
    }

    private async Task<RegistrationResult> RegisterBusinessOwnerAsync(
        AppUser user,
        RegistrationSubmission submission,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(submission.BusinessName))
        {
            throw new InvalidOperationException("Business name is required.");
        }

        var companyId = CreateSlug(submission.BusinessName);
        var companyTypes = await GetCompanyTypesForCurrentSystemModeAsync(cancellationToken);
        var companyType = companyTypes.FirstOrDefault()
            ?? throw new InvalidOperationException("No active business type is configured for this service.");
        var company = new Company(
            companyId,
            companyType.Id,
            submission.BusinessName.Trim(),
            string.IsNullOrWhiteSpace(submission.BusinessEmail) ? submission.Email : submission.BusinessEmail.Trim(),
            string.IsNullOrWhiteSpace(submission.BusinessPhone) ? submission.Phone : submission.BusinessPhone.Trim(),
            "America/Los_Angeles",
            CompanyStatus.Active);

        await store.UpsertCompanyAsync(company, cancellationToken);
        await store.UpsertClientTypeAsync(BusinessClientTypeReferenceData.HomeOwner(company.Id), cancellationToken);

        var membership = new CompanyMembership(
            company.Id,
            user.Id,
            CompanyRole.CompanyAdmin,
            MembershipStatus.Active,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            user.Id);
        await store.UpsertMembershipAsync(membership, cancellationToken);

        foreach (var serviceName in submission.InitialServices.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await store.UpsertServiceAsync(new ServiceOffering(
                CreateSlug(serviceName),
                company.Id,
                null,
                serviceName.Trim(),
                "Created during owner registration.",
                45,
                0m,
                true,
                true), cancellationToken);
        }

        return new RegistrationResult(
            user,
            company,
            membership,
            RequiresApproval: false,
            Message: "Your business profile is ready. You can open the company dashboard and finish setup.");
    }

    private async Task<RegistrationResult> RegisterCompanyScopedUserAsync(
        AppUser user,
        RegistrationSubmission submission,
        CompanyRole role,
        string message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(submission.CompanyId))
        {
            throw new InvalidOperationException("Choose the business you want to join.");
        }

        var company = await store.GetCompanyAsync(submission.CompanyId, cancellationToken)
            ?? throw new InvalidOperationException("Selected business was not found.");

        string? companyClientId = null;
        if (role == CompanyRole.CompanyClientUser)
        {
            if (string.IsNullOrWhiteSpace(submission.BusinessClientId))
            {
                throw new InvalidOperationException("Choose the business client address you want to access.");
            }

            var client = await store.GetClientAsync(company.Id, submission.BusinessClientId.Trim(), cancellationToken)
                ?? throw new InvalidOperationException("Selected business client was not found.");
            if (!client.IsActive)
            {
                throw new InvalidOperationException("Selected business client is inactive.");
            }

            companyClientId = client.Id;
        }

        var membership = new CompanyMembership(
            company.Id,
            user.Id,
            role,
            MembershipStatus.Pending,
            DateTimeOffset.UtcNow,
            null,
            null,
            companyClientId);
        await store.UpsertMembershipAsync(membership, cancellationToken);

        return new RegistrationResult(user, company, membership, RequiresApproval: true, Message: message);
    }

    private async Task<IReadOnlyList<CompanyType>> GetCompanyTypesForCurrentSystemModeAsync(CancellationToken cancellationToken)
    {
        var settings = await store.GetSystemSettingsAsync(cancellationToken);
        var companyTypes = await store.GetCompanyTypesAsync(cancellationToken);

        return companyTypes
            .Where(type => type.IsActive && CompanyTypeMatchesSystemMode(type, settings.SystemMode))
            .OrderBy(type => type.Name)
            .ToList();
    }

    private static bool CompanyTypeMatchesSystemMode(CompanyType type, SystemMode systemMode)
    {
        var searchable = $"{type.Id} {type.Name} {type.Description}";
        return systemMode == SystemMode.Pool
            ? searchable.Contains("pool", StringComparison.OrdinalIgnoreCase)
            : ContainsAny(searchable, "landscape", "landscaping", "lawn", "yard", "tree");
    }

    private static bool ContainsAny(string value, params string[] terms) =>
        terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));

    private async Task<RegistrationResult> RegisterIndependentHomeOwnerAsync(
        AppUser user,
        RegistrationSubmission submission,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(submission.HomeAddress))
        {
            throw new InvalidOperationException("Home address is required for homeowner registration.");
        }

        var now = DateTimeOffset.UtcNow;
        var existingProfile = await store.GetIndependentHomeOwnerProfileAsync(user.Id, cancellationToken);
        await store.UpsertIndependentHomeOwnerProfileAsync(new IndependentHomeOwnerProfile(
            user.Id,
            submission.HomeAddress.Trim(),
            string.IsNullOrWhiteSpace(submission.HomeAccessNotes) ? "" : submission.HomeAccessNotes.Trim(),
            existingProfile?.CreatedUtc ?? now,
            now), cancellationToken);

        var categories = await store.GetPoolEquipmentCategoriesAsync(EquipmentScope.HomeOwner, user.Id, cancellationToken);
        if (categories.Count == 0)
        {
            var category = new PoolEquipmentCategory(
                "my-pool-equipment",
                EquipmentScope.HomeOwner,
                user.Id,
                "My Equipment",
                "My Pool Equipment",
                "Personal pool equipment records.",
                false,
                true);
            await store.UpsertPoolEquipmentCategoryAsync(category, cancellationToken);

            await store.UpsertPoolEquipmentItemAsync(new PoolEquipmentItem(
                "primary-pool-equipment",
                EquipmentScope.HomeOwner,
                user.Id,
                category.Id,
                "Primary Pool Equipment",
                "Add your pump, filter, heater, or other equipment details.",
                "/images/pool-waterfall-hero.png",
                true), cancellationToken);
        }

        return new RegistrationResult(
            user,
            Company: null,
            Membership: null,
            RequiresApproval: false,
            Message: "Your homeowner account is ready. You can manage your pool equipment now.");
    }

    private static string CreateSlug(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        var slug = string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(slug) ? $"item-{Guid.NewGuid():N}" : slug;
    }
}
