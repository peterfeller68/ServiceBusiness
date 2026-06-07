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
}

public sealed class PlatformAdminService(IServiceBusinessStore store, TenantAuthorizationService authorization)
{
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

    private async Task EnsureAnotherActiveSystemAdminExistsAsync(string excludedUserId, CancellationToken cancellationToken)
    {
        var users = await store.GetUsersAsync(cancellationToken);
        if (users.Count(u => u.IsSystemAdmin && u.Status == UserStatus.Active && u.Id != excludedUserId) == 0)
        {
            throw new InvalidOperationException("At least one active system admin is required.");
        }
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
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim()
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
        var visits = await store.GetVisitsByDateAsync(companyId, date, cancellationToken);
        var memberships = await store.GetMembershipsForCompanyAsync(companyId, cancellationToken);
        var clients = await store.GetClientsAsync(companyId, cancellationToken);

        return new CompanyDashboard(
            company,
            TodayScheduled: visits.Count,
            TodayCompleted: visits.Count(v => v.Status == VisitStatus.Completed),
            UnassignedVisits: visits.Count(v => string.IsNullOrWhiteSpace(v.AssignedUserId)),
            PendingEmployeeRequests: memberships.Count(m => m.Status == MembershipStatus.Pending),
            ActiveClients: clients.Count(c => c.IsActive));
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
        var roles = await store.GetRoleDefinitionsAsync(cancellationToken);
        var memberships = await store.GetMembershipsForCompanyAsync(companyId, cancellationToken);

        return memberships
            .Where(m => m.Status == MembershipStatus.Pending)
            .OrderBy(m => m.RequestedUtc)
            .Select(m => new AccessRequest(
                m,
                users.First(u => u.Id == m.UserId),
                company,
                roles.First(r => r.Role == m.Role)))
            .ToList();
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
        var roleDefinition = (await store.GetRoleDefinitionsAsync(cancellationToken)).FirstOrDefault(r => r.Role == role);
        var membership = (await store.GetMembershipsForCompanyAsync(companyId, cancellationToken))
            .FirstOrDefault(m => m.UserId == userId && m.Role == role);

        return company is null || user is null || roleDefinition is null || membership is null
            ? null
            : new AccessRequest(membership, user, company, roleDefinition);
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

    private static string CreateSlug(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        var slug = string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(slug) ? $"item-{Guid.NewGuid():N}" : slug;
    }
}
