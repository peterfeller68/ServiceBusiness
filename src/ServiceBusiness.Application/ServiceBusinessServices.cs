using ServiceBusiness.Domain;

namespace ServiceBusiness.Application;

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
        await authorization.RequireSystemAdminAsync(cancellationToken);
        return await store.GetSystemSettingsAsync(cancellationToken);
    }

    public async Task<SystemSettings> UpdateSystemModeAsync(SystemMode systemMode, CancellationToken cancellationToken = default)
    {
        await authorization.RequireSystemAdminAsync(cancellationToken);

        var settings = new SystemSettings(systemMode);
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

        await store.UpsertCompanyAsync(company with
        {
            Id = CreateSlug(company.Id),
            CompanyTypeId = company.CompanyTypeId.Trim(),
            Name = company.Name.Trim(),
            BusinessEmail = company.BusinessEmail.Trim().ToLowerInvariant(),
            BusinessPhone = string.IsNullOrWhiteSpace(company.BusinessPhone) ? "" : company.BusinessPhone.Trim(),
            TimeZone = string.IsNullOrWhiteSpace(company.TimeZone) ? "America/Los_Angeles" : company.TimeZone.Trim()
        }, cancellationToken);
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
        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyAdmin, cancellationToken);
        return await store.GetClientsAsync(companyId, cancellationToken);
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
        await authorization.RequireSystemAdminOrAnyCompanyRoleAsync(companyId, [CompanyRole.CompanyAdmin, CompanyRole.CompanyUser], cancellationToken);

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

    public async Task UpsertPoolEquipmentCategoryAsync(PoolEquipmentCategory category, CancellationToken cancellationToken = default)
    {
        await RequireEquipmentAccessAsync(category.Scope, category.ScopeOwnerId, manage: true, cancellationToken);
        ValidateCategory(category.Id, category.Name);

        if (string.IsNullOrWhiteSpace(category.Manufacturer))
        {
            throw new InvalidOperationException("Manufacturer is required.");
        }

        await store.UpsertPoolEquipmentCategoryAsync(category with
        {
            Id = CreateSlug(category.Id),
            ScopeOwnerId = category.ScopeOwnerId.Trim(),
            Manufacturer = category.Manufacturer.Trim(),
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

        if (string.IsNullOrWhiteSpace(item.CategoryId))
        {
            throw new InvalidOperationException("Equipment category is required.");
        }

        if (string.IsNullOrWhiteSpace(item.Name))
        {
            throw new InvalidOperationException("Equipment item name is required.");
        }

        await EnsureEquipmentCategoryExistsAsync(item.Scope, item.ScopeOwnerId, item.CategoryId, cancellationToken);
        await store.UpsertPoolEquipmentItemAsync(item with
        {
            Id = CreateSlug(item.Id),
            ScopeOwnerId = item.ScopeOwnerId.Trim(),
            CategoryId = item.CategoryId.Trim(),
            Name = item.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(item.Description) ? "" : item.Description.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(item.ImageUrl) ? null : item.ImageUrl.Trim()
        }, cancellationToken);
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
            UnitOfMeasure = material.UnitOfMeasure.Trim()
        }, cancellationToken);
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

    public async Task<IReadOnlyList<ServiceVisit>> GetScheduleAsync(string companyId, DateOnly date, CancellationToken cancellationToken = default)
    {
        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyAdmin, cancellationToken);
        return await store.GetVisitsByDateAsync(companyId, date, cancellationToken);
    }

    public async Task AssignVisitAsync(string companyId, string visitId, string userId, CancellationToken cancellationToken = default)
    {
        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyAdmin, cancellationToken);

        var memberships = await store.GetMembershipsForCompanyAsync(companyId, cancellationToken);
        var assignee = memberships.FirstOrDefault(m =>
            m.UserId == userId &&
            m.Role == CompanyRole.CompanyUser &&
            m.Status == MembershipStatus.Active);

        if (assignee is null)
        {
            throw new InvalidOperationException("Visits can only be assigned to active company users.");
        }

        var visit = await store.GetVisitAsync(companyId, visitId, cancellationToken)
            ?? throw new InvalidOperationException("Visit was not found.");

        await store.UpsertVisitAsync(visit with
        {
            AssignedUserId = userId,
            Status = visit.Status == VisitStatus.Scheduled ? VisitStatus.Assigned : visit.Status
        }, cancellationToken);
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
        await authorization.RequireCompanyRoleAsync(companyId, CompanyRole.CompanyAdmin, cancellationToken);

        var company = await store.GetCompanyAsync(companyId, cancellationToken)
            ?? throw new InvalidOperationException("Company was not found.");
        var users = await store.GetUsersAsync(cancellationToken);
        var roles = (await store.GetRoleDefinitionsAsync(cancellationToken))
            .Select(NormalizeCompanyRoleDefinition)
            .ToList();
        var memberships = await store.GetMembershipsForCompanyAsync(companyId, cancellationToken);
        var pending = BuildAccessRequests(company, users, roles, memberships);
        var rows = memberships
            .Where(m => m.Status is MembershipStatus.Active or MembershipStatus.Inactive or MembershipStatus.Removed)
            .OrderByDescending(m => m.Status == MembershipStatus.Active)
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
        await authorization.RequireSystemAdminOrAnyCompanyRoleAsync(companyId, [CompanyRole.CompanyAdmin], cancellationToken);
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
                await authorization.RequireSystemAdminAsync(cancellationToken);
                break;
            case EquipmentScope.Company:
                await authorization.RequireSystemAdminOrAnyCompanyRoleAsync(
                    scopeOwnerId,
                    manage ? [CompanyRole.CompanyAdmin] : [CompanyRole.CompanyAdmin, CompanyRole.CompanyUser],
                    cancellationToken);
                break;
            case EquipmentScope.HomeOwner:
                var user = await authorization.RequireCurrentUserAsync(cancellationToken);
                if (!user.IsSystemAdmin && !string.Equals(user.Id, scopeOwnerId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException("You can only manage your own homeowner equipment catalog.");
                }

                break;
            default:
                throw new InvalidOperationException("Unsupported equipment scope.");
        }
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
            .ThenBy(c => c.Manufacturer)
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
                new PoolEquipmentCategory("uncategorized-equipment", scope, scopeOwnerId, "Unknown", "Uncategorized Equipment", "Equipment without an assigned category.", false, true),
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
        var completions = await Task.WhenAll(visits.Select(v => store.GetVisitCompletionAsync(companyId, v.Id, cancellationToken)));

        return visits
            .OrderBy(v => v.RouteOrder)
            .Select((visit, index) => new ServiceHistoryItem(
                visit,
                clients.First(c => c.Id == visit.CompanyClientId),
                null,
                completions[index]))
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
        var completion = new VisitCompletion(
            visit.Id,
            companyId,
            currentUser.UserId,
            serviceIds,
            materials,
            customerNotes,
            internalNotes,
            completedUtc);

        var completedVisit = visit with
        {
            Status = VisitStatus.Completed,
            CompletedUtc = completedUtc
        };

        await store.UpsertVisitAsync(completedVisit, cancellationToken);
        await store.UpsertVisitCompletionAsync(completion, cancellationToken);

        var client = await store.GetClientAsync(companyId, visit.CompanyClientId, cancellationToken)
            ?? throw new InvalidOperationException("Client was not found.");
        var user = await store.GetUserAsync(currentUser.UserId, cancellationToken);

        await notificationQueue.QueueVisitCompletedEmailAsync(
            new ServiceHistoryItem(completedVisit, client, user, completion),
            cancellationToken);
        ServiceBusinessTelemetry.VisitCompletions.Add(1);
    }
}

public sealed class ClientPortalService(IServiceBusinessStore store, TenantAuthorizationService authorization)
{
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
        var completions = await Task.WhenAll(visits.Select(v => store.GetVisitCompletionAsync(companyId, v.Id, cancellationToken)));

        return visits
            .Where(v => v.Status == VisitStatus.Completed)
            .OrderByDescending(v => v.CompletedUtc)
            .Select((visit, index) => new ServiceHistoryItem(
                visit,
                client,
                users.FirstOrDefault(u => u.Id == visit.AssignedUserId),
                completions[index]))
            .ToList();
    }
}

public sealed class OnboardingService(IServiceBusinessStore store)
{
    public async Task<IReadOnlyList<Company>> GetAvailableCompaniesAsync(CancellationToken cancellationToken = default)
    {
        var companies = await store.GetCompaniesAsync(cancellationToken);
        return companies
            .Where(c => c.Status == CompanyStatus.Active)
            .OrderBy(c => c.Name)
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

    public async Task<UserAccessOverview?> SignInAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var user = await store.GetUserByEmailAsync(email, cancellationToken);
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
            var updated = existing with
            {
                DisplayName = submission.DisplayName.Trim(),
                Phone = string.IsNullOrWhiteSpace(submission.Phone) ? existing.Phone : submission.Phone.Trim()
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
            false,
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
        var companyTypes = await store.GetCompanyTypesAsync(cancellationToken);
        var company = new Company(
            companyId,
            companyTypes.First(t => t.IsActive).Id,
            submission.BusinessName.Trim(),
            string.IsNullOrWhiteSpace(submission.BusinessEmail) ? submission.Email : submission.BusinessEmail.Trim(),
            string.IsNullOrWhiteSpace(submission.BusinessPhone) ? submission.Phone : submission.BusinessPhone.Trim(),
            "America/Los_Angeles",
            CompanyStatus.Active);

        await store.UpsertCompanyAsync(company, cancellationToken);

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

        var membership = new CompanyMembership(
            company.Id,
            user.Id,
            role,
            MembershipStatus.Pending,
            DateTimeOffset.UtcNow,
            null,
            null);
        await store.UpsertMembershipAsync(membership, cancellationToken);

        return new RegistrationResult(user, company, membership, RequiresApproval: true, Message: message);
    }

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
