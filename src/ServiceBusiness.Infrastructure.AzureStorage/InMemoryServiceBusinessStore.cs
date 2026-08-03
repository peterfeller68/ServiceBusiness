using ServiceBusiness.Application;
using ServiceBusiness.Domain;

namespace ServiceBusiness.Infrastructure.AzureStorage;

public sealed class InMemoryServiceBusinessStore : IServiceBusinessStore
{
    private readonly object sync = new();
    private readonly List<AppUser> users = [];
    private readonly List<RoleDefinition> roles = [];
    private readonly List<CompanyType> companyTypes = [];
    private readonly List<Company> companies = [];
    private readonly List<CompanyMembership> memberships = [];
    private readonly List<ClientType> clientTypes = [];
    private readonly List<CompanyClient> clients = [];
    private readonly List<IndependentHomeOwnerProfile> independentHomeOwnerProfiles = [];
    private readonly List<HomeOwnerPhotoRecord> homeOwnerPoolEquipmentPhotos = [];
    private readonly List<IndependentHomeOwnerServiceHistoryItem> independentHomeOwnerServiceHistory = [];
    private readonly List<ServiceCategory> serviceCategories = [];
    private readonly List<MaterialCategory> materialCategories = [];
    private readonly List<PoolEquipmentCategory> poolEquipmentCategories = [];
    private readonly List<ServiceOffering> services = [];
    private readonly List<ServicePackage> servicePackages = [];
    private readonly List<Material> materials = [];
    private readonly List<PoolEquipmentItem> poolEquipmentItems = [];
    private readonly List<ServiceVisit> visits = [];
    private readonly List<Invoice> invoices = [];
    private readonly List<EmailLogEntry> emailLogs = [];
    private SystemSettings systemSettings;

    public InMemoryServiceBusinessStore(SystemSettings? defaultSystemSettings = null)
    {
        systemSettings = defaultSystemSettings ?? new SystemSettings(SystemMode.Pool);
        Seed();
    }

    public Task<AppUser?> GetUserAsync(string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(users.FirstOrDefault(u => u.Id == userId));

    public Task<AppUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(users.FirstOrDefault(u => string.Equals(u.Email, email.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task<AppUser?> GetUserByGoogleSubjectAsync(string googleSubjectId, CancellationToken cancellationToken = default) =>
        Task.FromResult(users.FirstOrDefault(u => string.Equals(u.GoogleSubjectId, googleSubjectId.Trim(), StringComparison.Ordinal)));

    public Task<IReadOnlyList<AppUser>> GetUsersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AppUser>>(users.ToList());

    public Task<SystemSettings> GetSystemSettingsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(systemSettings);

    public Task<IReadOnlyList<RoleDefinition>> GetRoleDefinitionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RoleDefinition>>(roles.ToList());

    public Task<IReadOnlyList<CompanyType>> GetCompanyTypesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CompanyType>>(companyTypes.ToList());

    public Task<IReadOnlyList<Company>> GetCompaniesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Company>>(companies.ToList());

    public Task<Company?> GetCompanyAsync(string companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult(companies.FirstOrDefault(c => c.Id == companyId));

    public Task<IReadOnlyList<CompanyMembership>> GetMembershipsForUserAsync(string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CompanyMembership>>(memberships.Where(m => m.UserId == userId).ToList());

    public Task<IReadOnlyList<CompanyMembership>> GetMembershipsForCompanyAsync(string companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CompanyMembership>>(memberships.Where(m => m.CompanyId == companyId).ToList());

    public Task<IReadOnlyList<ClientType>> GetClientTypesAsync(string companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ClientType>>(clientTypes.Where(c => c.CompanyId == companyId).ToList());

    public Task<IReadOnlyList<CompanyClient>> GetClientsAsync(string companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CompanyClient>>(clients.Where(c => c.CompanyId == companyId).ToList());

    public Task<CompanyClient?> GetClientAsync(string companyId, string clientId, CancellationToken cancellationToken = default) =>
        Task.FromResult(clients.FirstOrDefault(c => c.CompanyId == companyId && c.Id == clientId));

    public Task<IndependentHomeOwnerProfile?> GetIndependentHomeOwnerProfileAsync(string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(independentHomeOwnerProfiles.FirstOrDefault(p => p.UserId == userId));

    public Task<IReadOnlyList<IndependentHomeOwnerProfile>> GetIndependentHomeOwnerProfilesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IndependentHomeOwnerProfile>>(independentHomeOwnerProfiles
            .OrderBy(p => p.HomeAddress)
            .ThenBy(p => p.UserId)
            .ToList());

    public Task<IReadOnlyList<HomeOwnerPoolEquipmentPhoto>> GetHomeOwnerPoolEquipmentPhotosAsync(string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<HomeOwnerPoolEquipmentPhoto>>(homeOwnerPoolEquipmentPhotos
            .Where(p => p.UserId == userId)
            .Select(p => p.Photo)
            .OrderByDescending(p => p.UploadedUtc)
            .ToList());

    public Task<IReadOnlyList<IndependentHomeOwnerServiceHistoryItem>> GetIndependentHomeOwnerServiceHistoryAsync(string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IndependentHomeOwnerServiceHistoryItem>>(independentHomeOwnerServiceHistory.Where(h => h.UserId == userId).OrderByDescending(h => h.ServiceDateTime).ToList());

    public Task<IReadOnlyList<ServiceCategory>> GetServiceCategoriesAsync(string companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ServiceCategory>>(serviceCategories.Where(c => c.CompanyId == companyId).ToList());

    public Task<IReadOnlyList<MaterialCategory>> GetMaterialCategoriesAsync(string companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MaterialCategory>>(materialCategories.Where(c => c.CompanyId == companyId).ToList());

    public Task<IReadOnlyList<PoolEquipmentCategory>> GetPoolEquipmentCategoriesAsync(EquipmentScope scope, string scopeOwnerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PoolEquipmentCategory>>(poolEquipmentCategories.Where(c => c.Scope == scope && c.ScopeOwnerId == scopeOwnerId).ToList());

    public Task<IReadOnlyList<ServiceOffering>> GetServicesAsync(string companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ServiceOffering>>(services.Where(s => s.CompanyId == companyId).ToList());

    public Task<IReadOnlyList<ServicePackage>> GetServicePackagesAsync(string companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ServicePackage>>(servicePackages.Where(p => p.CompanyId == companyId).ToList());

    public Task<IReadOnlyList<Material>> GetMaterialsAsync(string companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Material>>(materials.Where(m => m.CompanyId == companyId).ToList());

    public Task<IReadOnlyList<PoolEquipmentItem>> GetPoolEquipmentItemsAsync(EquipmentScope scope, string scopeOwnerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PoolEquipmentItem>>(poolEquipmentItems.Where(i => i.Scope == scope && i.ScopeOwnerId == scopeOwnerId).ToList());

    public Task<IReadOnlyList<ServiceVisit>> GetVisitsByDateAsync(string companyId, DateOnly date, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ServiceVisit>>(visits.Where(v => v.CompanyId == companyId && v.ScheduledDate == date).OrderBy(v => v.ServiceWindowStart).ToList());

    public Task<IReadOnlyList<ServiceVisit>> GetVisitsAsync(string companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ServiceVisit>>(visits.Where(v => v.CompanyId == companyId).OrderBy(v => v.ScheduledDate).ThenBy(v => v.ServiceWindowStart).ToList());

    public Task<IReadOnlyList<ServiceVisit>> GetVisitsForUserByDateAsync(string companyId, string userId, DateOnly date, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ServiceVisit>>(visits.Where(v => v.CompanyId == companyId && v.AssignedUserId == userId && v.ScheduledDate == date).OrderBy(v => v.RouteOrder).ToList());

    public Task<IReadOnlyList<ServiceVisit>> GetVisitsForClientAsync(string companyId, string clientId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ServiceVisit>>(visits.Where(v => v.CompanyId == companyId && v.CompanyClientId == clientId).ToList());

    public Task<ServiceVisit?> GetVisitAsync(string companyId, string visitId, CancellationToken cancellationToken = default) =>
        Task.FromResult(visits.FirstOrDefault(v => v.CompanyId == companyId && v.Id == visitId));

    public Task<IReadOnlyList<Invoice>> GetInvoicesAsync(string companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Invoice>>(invoices.Where(i => i.CompanyId == companyId).OrderByDescending(i => i.CreatedUtc).ToList());

    public Task<Invoice?> GetInvoiceAsync(string companyId, string invoiceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(invoices.FirstOrDefault(i => i.CompanyId == companyId && i.InvoiceId == invoiceId));

    public Task<IReadOnlyList<EmailLogEntry>> GetEmailLogsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<EmailLogEntry>>(emailLogs.OrderByDescending(e => e.CreatedUtc).ToList());

    public Task UpsertUserAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        Upsert(users, user, existing => existing.Id == user.Id);
        return Task.CompletedTask;
    }

    public Task UpsertRoleDefinitionAsync(RoleDefinition roleDefinition, CancellationToken cancellationToken = default)
    {
        Upsert(roles, roleDefinition, existing => existing.Role == roleDefinition.Role);
        return Task.CompletedTask;
    }

    public Task UpsertCompanyAsync(Company company, CancellationToken cancellationToken = default)
    {
        Upsert(companies, company, existing => existing.Id == company.Id);
        return Task.CompletedTask;
    }

    public Task UpsertMembershipAsync(CompanyMembership membership, CancellationToken cancellationToken = default)
    {
        Upsert(
            memberships,
            membership,
            existing => existing.CompanyId == membership.CompanyId &&
                existing.UserId == membership.UserId &&
                existing.Role == membership.Role);
        return Task.CompletedTask;
    }

    public Task UpsertClientTypeAsync(ClientType clientType, CancellationToken cancellationToken = default)
    {
        Upsert(clientTypes, clientType, existing => existing.CompanyId == clientType.CompanyId && existing.Id == clientType.Id);
        return Task.CompletedTask;
    }

    public Task UpsertClientAsync(CompanyClient client, CancellationToken cancellationToken = default)
    {
        Upsert(clients, client, existing => existing.CompanyId == client.CompanyId && existing.Id == client.Id);
        return Task.CompletedTask;
    }

    public Task UpsertIndependentHomeOwnerProfileAsync(IndependentHomeOwnerProfile profile, CancellationToken cancellationToken = default)
    {
        Upsert(independentHomeOwnerProfiles, profile with { PoolEquipmentPhotos = null }, existing => existing.UserId == profile.UserId);
        foreach (var photo in profile.PoolEquipmentPhotos ?? [])
        {
            Upsert(homeOwnerPoolEquipmentPhotos, new HomeOwnerPhotoRecord(profile.UserId, photo), existing => existing.UserId == profile.UserId && existing.Photo.Id == photo.Id);
        }
        return Task.CompletedTask;
    }

    public Task UpsertHomeOwnerPoolEquipmentPhotoAsync(string userId, HomeOwnerPoolEquipmentPhoto photo, CancellationToken cancellationToken = default)
    {
        Upsert(homeOwnerPoolEquipmentPhotos, new HomeOwnerPhotoRecord(userId, photo), existing => existing.UserId == userId && existing.Photo.Id == photo.Id);
        return Task.CompletedTask;
    }

    public Task DeleteHomeOwnerPoolEquipmentPhotoAsync(string userId, string photoId, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            homeOwnerPoolEquipmentPhotos.RemoveAll(existing => existing.UserId == userId && existing.Photo.Id == photoId);
        }

        return Task.CompletedTask;
    }

    public Task<UserDeletionResult> DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var rowsDeleted = 0;

        lock (sync)
        {
            rowsDeleted += users.RemoveAll(existing => existing.Id == userId);
            rowsDeleted += memberships.RemoveAll(existing => existing.UserId == userId);
            rowsDeleted += independentHomeOwnerProfiles.RemoveAll(existing => existing.UserId == userId);
            rowsDeleted += homeOwnerPoolEquipmentPhotos.RemoveAll(existing => existing.UserId == userId);
            rowsDeleted += independentHomeOwnerServiceHistory.RemoveAll(existing => existing.UserId == userId);
            rowsDeleted += serviceCategories.RemoveAll(existing => existing.CompanyId == userId);
            rowsDeleted += services.RemoveAll(existing => existing.CompanyId == userId);
            rowsDeleted += materialCategories.RemoveAll(existing => existing.CompanyId == userId);
            rowsDeleted += materials.RemoveAll(existing => existing.CompanyId == userId);
            rowsDeleted += poolEquipmentCategories.RemoveAll(existing => existing.Scope == EquipmentScope.HomeOwner && existing.ScopeOwnerId == userId);
            rowsDeleted += poolEquipmentItems.RemoveAll(existing => existing.Scope == EquipmentScope.HomeOwner && existing.ScopeOwnerId == userId);
            rowsDeleted += visits.RemoveAll(existing => string.Equals(existing.AssignedUserId, userId, StringComparison.OrdinalIgnoreCase));
            rowsDeleted += invoices.RemoveAll(existing => existing.CompanyClientId == userId);
            rowsDeleted += emailLogs.RemoveAll(existing => string.Equals(existing.RecipientUserId, userId, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(new UserDeletionResult(userId, rowsDeleted));
    }

    public Task UpsertIndependentHomeOwnerServiceHistoryItemAsync(IndependentHomeOwnerServiceHistoryItem item, CancellationToken cancellationToken = default)
    {
        Upsert(independentHomeOwnerServiceHistory, item, existing => existing.UserId == item.UserId && existing.Id == item.Id);
        return Task.CompletedTask;
    }

    public Task UpsertServiceCategoryAsync(ServiceCategory category, CancellationToken cancellationToken = default)
    {
        Upsert(serviceCategories, category, existing => existing.CompanyId == category.CompanyId && existing.Id == category.Id);
        return Task.CompletedTask;
    }

    public Task UpsertMaterialCategoryAsync(MaterialCategory category, CancellationToken cancellationToken = default)
    {
        Upsert(materialCategories, category, existing => existing.CompanyId == category.CompanyId && existing.Id == category.Id);
        return Task.CompletedTask;
    }

    public Task UpsertPoolEquipmentCategoryAsync(PoolEquipmentCategory category, CancellationToken cancellationToken = default)
    {
        Upsert(poolEquipmentCategories, category, existing => existing.Scope == category.Scope && existing.ScopeOwnerId == category.ScopeOwnerId && existing.Id == category.Id);
        return Task.CompletedTask;
    }

    public Task UpsertServiceAsync(ServiceOffering service, CancellationToken cancellationToken = default)
    {
        Upsert(services, service, existing => existing.CompanyId == service.CompanyId && existing.Id == service.Id);
        return Task.CompletedTask;
    }

    public Task UpsertServicePackageAsync(ServicePackage servicePackage, CancellationToken cancellationToken = default)
    {
        Upsert(servicePackages, servicePackage, existing => existing.CompanyId == servicePackage.CompanyId && existing.Id == servicePackage.Id);
        return Task.CompletedTask;
    }

    public Task UpsertMaterialAsync(Material material, CancellationToken cancellationToken = default)
    {
        Upsert(materials, material, existing => existing.CompanyId == material.CompanyId && existing.Id == material.Id);
        return Task.CompletedTask;
    }

    public Task UpsertPoolEquipmentItemAsync(PoolEquipmentItem item, CancellationToken cancellationToken = default)
    {
        Upsert(poolEquipmentItems, item, existing => existing.Scope == item.Scope && existing.ScopeOwnerId == item.ScopeOwnerId && existing.Id == item.Id);
        return Task.CompletedTask;
    }

    public Task DeleteServiceAsync(string companyId, string serviceId, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            services.RemoveAll(existing => existing.CompanyId == companyId && existing.Id == serviceId);
        }

        return Task.CompletedTask;
    }

    public Task DeleteServicePackageAsync(string companyId, string packageId, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            servicePackages.RemoveAll(existing => existing.CompanyId == companyId && existing.Id == packageId);
        }

        return Task.CompletedTask;
    }

    public Task DeleteVisitAsync(string companyId, string visitId, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            visits.RemoveAll(existing => existing.CompanyId == companyId && existing.Id == visitId);
        }

        return Task.CompletedTask;
    }

    public Task DeletePoolEquipmentItemAsync(EquipmentScope scope, string scopeOwnerId, string itemId, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            poolEquipmentItems.RemoveAll(existing => existing.Scope == scope && existing.ScopeOwnerId == scopeOwnerId && existing.Id == itemId);
        }

        return Task.CompletedTask;
    }

    public Task UpsertVisitAsync(ServiceVisit visit, CancellationToken cancellationToken = default)
    {
        Upsert(visits, visit, existing => existing.CompanyId == visit.CompanyId && existing.Id == visit.Id);
        return Task.CompletedTask;
    }

    public Task UpsertInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        Upsert(invoices, invoice, existing => existing.CompanyId == invoice.CompanyId && existing.InvoiceId == invoice.InvoiceId);
        return Task.CompletedTask;
    }

    public Task UpsertEmailLogAsync(EmailLogEntry emailLog, CancellationToken cancellationToken = default)
    {
        Upsert(emailLogs, emailLog, existing => existing.Id == emailLog.Id);
        return Task.CompletedTask;
    }

    public Task UpsertSystemSettingsAsync(SystemSettings settings, CancellationToken cancellationToken = default)
    {
        systemSettings = settings;
        return Task.CompletedTask;
    }

    private void Upsert<T>(List<T> list, T item, Func<T, bool> match)
    {
        lock (sync)
        {
            var index = list.FindIndex(existing => match(existing));
            if (index >= 0)
            {
                list[index] = item;
            }
            else
            {
                list.Add(item);
            }
        }
    }

    private sealed record HomeOwnerPhotoRecord(string UserId, HomeOwnerPoolEquipmentPhoto Photo);

    private void Seed()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        users.AddRange([
            new("sys-admin", null, "system@example.com", "system.test@example.com", "Sam System", "555-0101", null, true, true, true, UserStatus.Active),
            new("demo-owner-1", null, "owner-1@demo.example", "owner-1.notify@demo.example", "Avery Owner", "555-0102", null, false, true, true, UserStatus.Active),
            new("demo-user-1", null, "user-1@demo.example", "user-1.notify@demo.example", "Morgan Tech", "555-0103", null, false, true, true, UserStatus.Active),
            new("demo-client-1", null, "client-1@demo.example", "client-1.notify@demo.example", "Jordan Client", "555-0104", null, false, true, true, UserStatus.Active),
            new("demo-pending-user-1", null, "pending-user-1@demo.example", "pending-user-1.notify@demo.example", "Parker Pending", "555-0105", null, false, true, true, UserStatus.Active),
            new("independent-homeowner-1", null, "homeowner-1@independent.com", "homeowner-1.notify@independent.com", "Jordan Homeowner", "555-0106", null, false, true, true, UserStatus.Active),
            new("independent-homeowner-2", null, "homeowner-2@independent.com", "homeowner-2.notify@independent.com", "Harper Homeowner", "555-0107", null, false, true, true, UserStatus.Active),
            new("independent-homeowner-3", null, "homeowner-3@independent.com", "homeowner-3.notify@independent.com", "Riley Homeowner", "555-0108", null, false, true, true, UserStatus.Active),
            new("other-1", null, "other-1@gmail.com", "other-1.notify@gmail.com", "Other Test User 1", "555-0301", null, false, true, true, UserStatus.Active),
            new("other-2", null, "other-2@gmail.com", "other-2.notify@gmail.com", "Other Test User 2", "555-0302", null, false, true, true, UserStatus.Active),
            new("other-3", null, "other-3@gmail.com", "other-3.notify@gmail.com", "Other Test User 3", "555-0303", null, false, true, true, UserStatus.Active),
            new("other-4", null, "other-4@gmail.com", "other-4.notify@gmail.com", "Other Test User 4", "555-0304", null, false, true, true, UserStatus.Active),
            new("other-5", null, "other-5@gmail.com", "other-5.notify@gmail.com", "Other Test User 5", "555-0305", null, false, true, true, UserStatus.Active),
            new("other-6", null, "other-6@gmail.com", "other-6.notify@gmail.com", "Other Test User 6", "555-0306", null, false, true, true, UserStatus.Active),
            new("other-7", null, "other-7@gmail.com", "other-7.notify@gmail.com", "Other Test User 7", "555-0307", null, false, true, true, UserStatus.Active),
            new("other-8", null, "other-8@gmail.com", "other-8.notify@gmail.com", "Other Test User 8", "555-0308", null, false, true, true, UserStatus.Active),
            new("other-9", null, "other-9@gmail.com", "other-9.notify@gmail.com", "Other Test User 9", "555-0309", null, false, true, true, UserStatus.Active),
            new("other-10", null, "other-10@gmail.com", "other-10.notify@gmail.com", "Other Test User 10", "555-0310", null, false, true, true, UserStatus.Active)
        ]);

        roles.AddRange([
            new(CompanyRole.CompanyAdmin, "Business Owner", "Owns company setup, approvals, scheduling, clients, and reporting.", false,
                ["company.configure", "users.approve", "clients.manage", "schedule.manage", "catalog.manage", "reports.view"]),
            new(CompanyRole.CompanyUser, "Business User", "Works assigned field visits and records service completion.", true,
                ["visits.assigned.view", "visits.start", "visits.complete", "materials.record"]),
            new(CompanyRole.CompanyClientUser, "Business Client", "Views service history and client-facing account details.", true,
                ["client.history.view", "client.messages.create", "client.billing.view"])
        ]);

        companyTypes.AddRange([
            new("pool", "Pool Cleaning Service", "Pool maintenance, chemicals, and cleaning routes.", true),
            new("landscaping", "Landscaping Service", "Lawn care, planting, cleanup, and recurring yard service.", true)
        ]);

        companies.Add(new(
            "clearwater",
            "pool",
            "Clearwater Pool Care",
            "hello@clearwater.example",
            "555-0100",
            "America/Los_Angeles",
            CompanyStatus.Active));

        memberships.AddRange([
            new("clearwater", "demo-owner-1", CompanyRole.CompanyAdmin, MembershipStatus.Active, DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow.AddDays(-30), "sys-admin"),
            new("clearwater", "demo-user-1", CompanyRole.CompanyUser, MembershipStatus.Active, DateTimeOffset.UtcNow.AddDays(-25), DateTimeOffset.UtcNow.AddDays(-25), "demo-owner-1"),
            new("clearwater", "demo-client-1", CompanyRole.CompanyClientUser, MembershipStatus.Active, DateTimeOffset.UtcNow.AddDays(-12), DateTimeOffset.UtcNow.AddDays(-12), "demo-owner-1"),
            new("clearwater", "demo-pending-user-1", CompanyRole.CompanyUser, MembershipStatus.Pending, DateTimeOffset.UtcNow.AddHours(-8), null, null)
        ]);

        clientTypes.AddRange([
            BusinessClientTypeReferenceData.HomeOwner("clearwater")
        ]);

        clients.AddRange([
            new("client-1", "clearwater", "Diaz Residence", "Elena Diaz", "elena@example.com", "555-0111", "1142 Palm View Dr, Phoenix, AZ", "Gate code 2468. Equipment is on left side yard.", BusinessClientTypeReferenceData.HomeOwnerId, null, true),
            new("client-2", "clearwater", "Nguyen Residence", "Ben Nguyen", "ben@example.com", "555-0112", "89 Desert Bloom Ln, Phoenix, AZ", "Use side entrance. Dogs are inside during service window.", BusinessClientTypeReferenceData.HomeOwnerId, 165m, true),
            new("client-3", "clearwater", "Patel Residence", "Mina Patel", "mina@example.com", "555-0113", "720 Citrus Way, Phoenix, AZ", "Text before arrival.", BusinessClientTypeReferenceData.HomeOwnerId, null, true)
        ]);

        AddIndependentHomeOwnerSeed(
            "independent-homeowner-1",
            "1142 Palm View Dr, Phoenix, AZ",
            "Gate code 2468. Equipment is on left side yard.",
            "Backyard Pump",
            "Primary circulation pump for the backyard pool.");

        AddIndependentHomeOwnerSeed(
            "independent-homeowner-2",
            "500 Scenario Way, Phoenix, AZ",
            "Side gate. Equipment pad behind the spa.",
            "Spa Booster Pump",
            "Booster pump and spa equipment maintained by homeowner.");

        AddIndependentHomeOwnerSeed(
            "independent-homeowner-3",
            "89 Desert Bloom Ln, Phoenix, AZ",
            "Text before arrival. Equipment is behind the block wall.",
            "Cartridge Filter",
            "Owner-maintained cartridge filter record.");

        serviceCategories.AddRange([
            new("svc-cat-maintenance", "clearwater", "Pool Maintenance", "Recurring pool cleaning and chemistry services.", true, true),
            new("svc-cat-equipment", "clearwater", "Equipment Care", "Filter and equipment service work.", true, true)
        ]);

        materialCategories.AddRange([
            new("mat-cat-chemicals", "clearwater", "Chemicals", "Water treatment and balancing supplies.", true, true),
            new("mat-cat-parts", "clearwater", "Parts", "Replacement parts and physical supplies.", true, true)
        ]);

        services.AddRange([
            new("svc-basic", "clearwater", "svc-cat-maintenance", "Standard Pool Cleaning", "Skim, brush, vacuum, and basket check.", 45, 95m, true, true),
            new("svc-chem", "clearwater", "svc-cat-maintenance", "Chemical Balance", "Test and balance pool chemistry.", 15, 35m, true, true),
            new("svc-filter", "clearwater", "svc-cat-equipment", "Filter Cleaning", "Clean filter cartridge or backwash as needed.", 30, 65m, true, true)
        ]);

        materials.AddRange([
            new("mat-chlorine", "clearwater", "mat-cat-chemicals", "Chlorine", "lb", 3.50m, 6.00m, true, true),
            new("mat-acid", "clearwater", "mat-cat-chemicals", "Muriatic Acid", "gal", 7.00m, 12.00m, true, true),
            new("mat-tabs", "clearwater", "mat-cat-chemicals", "Tabs", "each", 1.25m, 2.50m, true, true)
        ]);

        serviceCategories.AddRange([
            new("pool-cleaning", GlobalCatalogScope.Pool, "Pool Cleaning", "Global pool cleaning service templates.", true, true),
            new("landscape-maintenance", GlobalCatalogScope.Landscape, "Landscape Maintenance", "Global landscape maintenance service templates.", true, true)
        ]);

        materialCategories.AddRange([
            new("chlorine", GlobalCatalogScope.Pool, "Chlorine", "Global pool chemical material catalog.", true, true),
            new("plant-care", GlobalCatalogScope.Landscape, "Plant Care", "Global landscape material catalog.", true, true)
        ]);

        services.AddRange([
            new("pool-cleaning-standard-service", GlobalCatalogScope.Pool, "pool-cleaning", "Standard Service", "Skim pool surface, empty baskets, brush walls, test water, and inspect equipment.", 45, 0m, true, true),
            new("pool-cleaning-chemical-only-service", GlobalCatalogScope.Pool, "pool-cleaning", "Chemical Only Service", "Test and balance water chemistry without physical cleaning.", 45, 0m, true, true),
            new("landscape-maintenance-standard-yard-service", GlobalCatalogScope.Landscape, "landscape-maintenance", "Standard Yard Service", "Mow, edge, blow hardscapes, and inspect irrigation zones.", 45, 0m, true, true),
            new("landscape-maintenance-seasonal-cleanup", GlobalCatalogScope.Landscape, "landscape-maintenance", "Seasonal Cleanup", "Trim shrubs, clear debris, and refresh visible planting areas.", 45, 0m, true, true)
        ]);

        servicePackages.AddRange([
            new(
                "pool-service-level-1",
                GlobalCatalogScope.Pool,
                "Pool Service Level 1",
                "Weekly",
                "Starter global pool care package.",
                129m,
                true,
                [
                    new("pool-cleaning-standard-service", "Every Visit"),
                    new("pool-cleaning-chemical-only-service", "Every 4 Visits")
                ]),
            new(
                "landscape-service-level-1",
                GlobalCatalogScope.Landscape,
                "Landscape Service Level 1",
                "Weekly",
                "Starter global landscape care package.",
                149m,
                true,
                [
                    new("landscape-maintenance-standard-yard-service", "Every Visit"),
                    new("landscape-maintenance-seasonal-cleanup", "Every 12 Visits")
                ])
        ]);

        materials.AddRange([
            new("bioguard-3-in-trichlor-tablets-50-lb-tab-50", GlobalCatalogScope.Pool, "chlorine", "3-in Trichlor Tablets 50 lb", "Each", 0m, 0m, true, true, "BioGuard", "TAB-50"),
            new("hth-1-in-chlorine-tablets-25-lb-cl-125", GlobalCatalogScope.Pool, "chlorine", "1-in Chlorine Tablets 25 lb", "Each", 0m, 0m, true, true, "HTH", "CL-125"),
            new("scotts-turf-builder-lawn-food-32-lb-tb-32", GlobalCatalogScope.Landscape, "plant-care", "Turf Builder Lawn Food 32 lb", "Each", 0m, 0m, true, true, "Scotts", "TB-32"),
            new("miracle-gro-all-purpose-plant-food-mg-5", GlobalCatalogScope.Landscape, "plant-care", "All Purpose Plant Food", "Each", 0m, 0m, true, true, "Miracle-Gro", "MG-5")
        ]);

        AddEquipmentSeed(
            EquipmentScope.Global,
            "global",
            [
                ("global-pumps", "Pentair", "Pool Pumps", "Circulation pumps and pump assemblies."),
                ("global-filters", "Hayward", "Pool Filters", "Cartridge, sand, and DE filtration equipment."),
                ("global-heaters", "Jandy", "Pool Heaters", "Gas and electric pool heating equipment.")
            ],
            [
                ("global-vs-pump", "global-pumps", "Variable Speed Pump", "Energy-efficient variable speed pump.", "/images/pool-waterfall-hero.png"),
                ("global-cartridge-filter", "global-filters", "Cartridge Filter", "High-capacity cartridge filter body.", "/images/pool-waterfall-hero.png"),
                ("global-gas-heater", "global-heaters", "Gas Heater", "Natural gas pool heater.", "/images/pool-waterfall-hero.png")
            ],
            systemManaged: true);

        AddEquipmentSeed(
            EquipmentScope.Company,
            "clearwater",
            [
                ("clearwater-pumps", "Pentair", "Installed Pumps", "Pumps commonly serviced by Clearwater."),
                ("clearwater-filters", "Hayward", "Installed Filters", "Filter equipment for managed customer pools.")
            ],
            [
                ("clearwater-intelliflo", "clearwater-pumps", "IntelliFlo VSF", "Customer pump model with weekly basket inspection.", "/images/pool-waterfall-hero.png"),
                ("clearwater-clean-clear", "clearwater-filters", "Clean & Clear Plus", "Cartridge filter model used across routes.", "/images/pool-waterfall-hero.png")
            ],
            systemManaged: true);

        AddSeedCompany(
            "pool1clean1",
            "pool",
            "Pool1Clean1",
            "Pool service company covering weekly cleaning, repair, and chemistry.",
            [
                ("weekly-cleaning", "Weekly Pool Cleaning", "Skim, brush, vacuum, and basket service.", 45, 110m),
                ("chemical-balance", "Chemical Balance", "Water test and chemical balancing.", 20, 45m),
                ("filter-service", "Filter Service", "Clean or backwash pool filters.", 35, 75m),
                ("pool-repair", "Pool Repair Visit", "Troubleshoot pump, leak, or equipment issues.", 75, 165m),
                ("green-pool-recovery", "Green Pool Recovery", "Algae treatment and recovery service.", 90, 225m)
            ],
            [
                ("chlorine-tabs", "Chlorine Tabs", "bucket", 42m, 68m),
                ("muriatic-acid", "Muriatic Acid", "gal", 7m, 13m),
                ("shock-bags", "Shock Bags", "bag", 5m, 10m),
                ("filter-cartridge", "Filter Cartridge", "each", 62m, 95m),
                ("test-strips", "Test Strips", "pack", 11m, 18m)
            ]);

        AddSeedCompany(
            "poolclean2",
            "pool",
            "PoolClean2",
            "Pool service company focused on premium maintenance and equipment care.",
            [
                ("premium-cleaning", "Premium Pool Cleaning", "Detailed pool cleaning and equipment check.", 60, 145m),
                ("salt-cell-cleaning", "Salt Cell Cleaning", "Clean and inspect salt chlorinator cell.", 35, 85m),
                ("tile-brushing", "Tile Brushing", "Brush tile line and remove buildup.", 40, 95m),
                ("pump-inspection", "Pump Inspection", "Inspect pump basket, seals, and operation.", 45, 115m),
                ("vacation-service", "Vacation Service Check", "Extra pool service while homeowner is away.", 30, 70m)
            ],
            [
                ("salt", "Pool Salt", "bag", 8m, 16m),
                ("clarifier", "Clarifier", "qt", 9m, 18m),
                ("alkalinity-up", "Alkalinity Up", "lb", 2m, 5m),
                ("phosphate-remover", "Phosphate Remover", "qt", 18m, 32m),
                ("pump-lid-o-ring", "Pump Lid O-Ring", "each", 6m, 14m)
            ]);

        AddSeedCompany(
            "landscape1",
            "landscaping",
            "Landscape1",
            "Landscape service company covering lawn mowing and landscape design.",
            [
                ("lawn-mowing", "Lawn Mowing", "Mow, edge, and blow hardscape areas.", 50, 85m),
                ("hedge-trimming", "Hedge Trimming", "Shape shrubs and hedges.", 70, 135m),
                ("landscape-design", "Landscape Design Consult", "On-site planning for landscape refresh.", 90, 250m),
                ("tree-trimming", "Tree Trimming", "Trim small ornamental trees.", 120, 320m),
                ("seasonal-cleanup", "Seasonal Cleanup", "Remove debris and refresh beds.", 100, 220m)
            ],
            [
                ("mulch", "Mulch", "bag", 4m, 9m),
                ("fertilizer", "Fertilizer", "bag", 18m, 32m),
                ("edger-blades", "Edger Blades", "each", 7m, 16m),
                ("plant-mix", "Planting Mix", "bag", 9m, 18m),
                ("weed-control", "Weed Control", "gal", 21m, 38m)
            ]);

        AddSeedCompany(
            "landscape2",
            "landscaping",
            "Landscape2",
            "Landscape service company covering recurring yard service and planting.",
            [
                ("yard-service", "Recurring Yard Service", "Mow, edge, trim, and cleanup.", 60, 105m),
                ("irrigation-check", "Irrigation Check", "Inspect sprinkler heads and timer schedule.", 45, 95m),
                ("flower-bed-refresh", "Flower Bed Refresh", "Weed, cultivate, and refresh flower beds.", 80, 180m),
                ("tree-pruning", "Tree Pruning", "Prune small trees and remove low limbs.", 110, 285m),
                ("sod-patching", "Sod Patching", "Patch damaged lawn areas.", 95, 240m)
            ],
            [
                ("sod-roll", "Sod Roll", "roll", 6m, 14m),
                ("sprinkler-head", "Sprinkler Head", "each", 5m, 15m),
                ("drip-line", "Drip Line", "ft", 1m, 3m),
                ("flower-flat", "Flower Flat", "flat", 19m, 36m),
                ("topsoil", "Topsoil", "bag", 5m, 11m)
            ]);

        visits.AddRange([
            new("visit-1", "clearwater", "client-1", "demo-user-1", today, new TimeOnly(8, 0), new TimeOnly(10, 0), VisitStatus.Assigned, ["svc-basic", "svc-chem"], 1, "Check chlorine levels closely.", null, null),
            new("visit-2", "clearwater", "client-2", "demo-user-1", today, new TimeOnly(10, 0), new TimeOnly(12, 0), VisitStatus.Assigned, ["svc-basic"], 2, "Customer requested photo after service in future phase.", null, null),
            new("visit-3", "clearwater", "client-3", null, today, new TimeOnly(13, 0), new TimeOnly(15, 0), VisitStatus.New, ["svc-filter"], 0, "Needs assignment.", null, null),
            new("visit-4", "clearwater", "client-1", "demo-user-1", today.AddDays(-7), new TimeOnly(8, 0), new TimeOnly(10, 0), VisitStatus.Completed, ["svc-basic", "svc-chem"], 1, "", new DateTimeOffset(today.AddDays(-7).ToDateTime(new TimeOnly(8, 15), DateTimeKind.Local)), new DateTimeOffset(today.AddDays(-7).ToDateTime(new TimeOnly(9, 0), DateTimeKind.Local)), CompletedByUserId: "demo-user-1", CompletedServiceIds: ["svc-basic", "svc-chem"], MaterialsUsed: [new("mat-chlorine", 2)], NotesToBusinessClient: "Pool cleaned and chemicals balanced.", InternalNotes: "Slight algae starting near steps.")
        ]);
    }

    private void AddSeedCompany(
        string companyId,
        string companyTypeId,
        string companyName,
        string description,
        IReadOnlyList<(string Id, string Name, string Description, int DurationMinutes, decimal Price)> serviceSeed,
        IReadOnlyList<(string Id, string Name, string Unit, decimal Cost, decimal Price)> materialSeed)
    {
        companies.Add(new(
            companyId,
            companyTypeId,
            companyName,
            $"hello@{companyId}.com",
            "555-0200",
            "America/Los_Angeles",
            CompanyStatus.Active));

        var ownerId = $"{companyId}-owner-1";
        var user1Id = $"{companyId}-user-1";
        var user2Id = $"{companyId}-user-2";
        var client1Id = $"{companyId}-client-1";
        var client2Id = $"{companyId}-client-2";

        users.AddRange([
            new(ownerId, null, $"owner-1@{companyId}.com", $"owner-1.notify@{companyId}.com", $"{companyName} Owner", "555-0211", null, false, true, true, UserStatus.Active),
            new(user1Id, null, $"user-1@{companyId}.com", $"user-1.notify@{companyId}.com", $"{companyName} Route User 1", "555-0212", null, false, true, true, UserStatus.Active),
            new(user2Id, null, $"user-2@{companyId}.com", $"user-2.notify@{companyId}.com", $"{companyName} Route User 2", "555-0213", null, false, true, true, UserStatus.Active),
            new(client1Id, null, $"client-1@{companyId}.com", $"client-1.notify@{companyId}.com", $"{companyName} Client User 1", "555-0214", null, false, true, true, UserStatus.Active),
            new(client2Id, null, $"client-2@{companyId}.com", $"client-2.notify@{companyId}.com", $"{companyName} Client User 2", "555-0215", null, false, true, true, UserStatus.Active)
        ]);

        memberships.AddRange([
            new(companyId, ownerId, CompanyRole.CompanyAdmin, MembershipStatus.Active, DateTimeOffset.UtcNow.AddDays(-20), DateTimeOffset.UtcNow.AddDays(-20), "sys-admin"),
            new(companyId, user1Id, CompanyRole.CompanyUser, MembershipStatus.Active, DateTimeOffset.UtcNow.AddDays(-18), DateTimeOffset.UtcNow.AddDays(-18), ownerId),
            new(companyId, user2Id, CompanyRole.CompanyUser, MembershipStatus.Pending, DateTimeOffset.UtcNow.AddDays(-2), null, null),
            new(companyId, client1Id, CompanyRole.CompanyClientUser, MembershipStatus.Active, DateTimeOffset.UtcNow.AddDays(-15), DateTimeOffset.UtcNow.AddDays(-15), ownerId),
            new(companyId, client2Id, CompanyRole.CompanyClientUser, MembershipStatus.Pending, DateTimeOffset.UtcNow.AddDays(-1), null, null)
        ]);

        clientTypes.AddRange([
            BusinessClientTypeReferenceData.HomeOwner(companyId)
        ]);

        clients.AddRange([
            new($"{companyId}-home-1", companyId, $"{companyName} Test Home 1", "Client Contact 1", $"client-home-1@{companyId}.com", "555-0221", "101 Test Loop", "Use side gate.", BusinessClientTypeReferenceData.HomeOwnerId, null, true),
            new($"{companyId}-home-2", companyId, $"{companyName} Test Home 2", "Client Contact 2", $"client-home-2@{companyId}.com", "555-0222", "202 Sample Way", "Text before arrival.", BusinessClientTypeReferenceData.HomeOwnerId, 210m, true)
        ]);

        var serviceCategoryId = $"{companyId}-service-core";
        var specialtyServiceCategoryId = $"{companyId}-service-specialty";
        var materialCategoryId = $"{companyId}-materials-primary";
        var partsCategoryId = $"{companyId}-materials-parts";

        serviceCategories.AddRange([
            new(serviceCategoryId, companyId, "Core Services", description, true, true),
            new(specialtyServiceCategoryId, companyId, "Specialty Services", "Higher-touch one-off or seasonal services.", true, true)
        ]);

        materialCategories.AddRange([
            new(materialCategoryId, companyId, "Primary Materials", "Common supplies used during routine service.", true, true),
            new(partsCategoryId, companyId, "Parts and Supplies", "Replacement parts and specialty supplies.", true, true)
        ]);

        foreach (var (item, index) in serviceSeed.Select((item, index) => (item, index)))
        {
            services.Add(new(
                $"{companyId}-{item.Id}",
                companyId,
                index < 3 ? serviceCategoryId : specialtyServiceCategoryId,
                item.Name,
                item.Description,
                item.DurationMinutes,
                item.Price,
                true,
                true));
        }

        servicePackages.Add(new(
            $"{companyId}-standard-package",
            companyId,
            $"{companyName} Standard Package",
            "Weekly",
            $"Default service package for {companyName}.",
            serviceSeed.FirstOrDefault().Price,
            true,
            serviceSeed.Take(3)
                .Select(item => new ServicePackageService($"{companyId}-{item.Id}", "Every Visit"))
                .ToList()));

        foreach (var (item, index) in materialSeed.Select((item, index) => (item, index)))
        {
            materials.Add(new(
                $"{companyId}-{item.Id}",
                companyId,
                index < 3 ? materialCategoryId : partsCategoryId,
                item.Name,
                item.Unit,
                item.Cost,
                item.Price,
                true,
                true));
        }

        AddEquipmentSeed(
            EquipmentScope.Company,
            companyId,
            [
                ($"{companyId}-equipment-primary", companyTypeId == "pool" ? "Pentair" : "Rain Bird", "Primary Equipment", $"Common equipment tracked by {companyName}."),
                ($"{companyId}-equipment-parts", companyTypeId == "pool" ? "Hayward" : "Hunter", "Equipment Parts", $"Replacement equipment parts tracked by {companyName}.")
            ],
            [
                ($"{companyId}-equipment-1", $"{companyId}-equipment-primary", companyTypeId == "pool" ? "Variable Speed Pump" : "Irrigation Controller", "Primary field equipment record.", "/images/pool-waterfall-hero.png"),
                ($"{companyId}-equipment-2", $"{companyId}-equipment-primary", companyTypeId == "pool" ? "Cartridge Filter" : "Valve Box", "Routinely inspected equipment record.", "/images/pool-waterfall-hero.png"),
                ($"{companyId}-equipment-3", $"{companyId}-equipment-parts", companyTypeId == "pool" ? "Pump Lid Assembly" : "Sprinkler Head", "Common replacement equipment item.", "/images/pool-waterfall-hero.png")
            ],
            systemManaged: true);
    }

    private void AddIndependentHomeOwnerSeed(
        string userId,
        string homeAddress,
        string accessNotes,
        string equipmentItemName,
        string equipmentItemDescription)
    {
        independentHomeOwnerProfiles.Add(new(
            userId,
            homeAddress,
            accessNotes,
            DateTimeOffset.UtcNow.AddDays(-12),
            DateTimeOffset.UtcNow.AddDays(-12)));

        AddEquipmentSeed(
            EquipmentScope.HomeOwner,
            userId,
            [
                ("homeowner-equipment", "Pentair", "My Pool Equipment", "Homeowner maintained equipment records.")
            ],
            [
                ("homeowner-pump", "homeowner-equipment", equipmentItemName, equipmentItemDescription, "/images/pool-waterfall-hero.png")
            ],
            systemManaged: false);
    }

    private void AddEquipmentSeed(
        EquipmentScope scope,
        string scopeOwnerId,
        IReadOnlyList<(string Id, string Manufacturer, string Name, string Description)> categorySeed,
        IReadOnlyList<(string Id, string CategoryId, string Name, string Description, string? ImageUrl)> itemSeed,
        bool systemManaged)
    {
        foreach (var category in categorySeed)
        {
            poolEquipmentCategories.Add(new(
                category.Id,
                scope,
                scopeOwnerId,
                category.Manufacturer,
                category.Name,
                category.Description,
                systemManaged,
                true));
        }

        foreach (var item in itemSeed)
        {
            poolEquipmentItems.Add(new(
                item.Id,
                scope,
                scopeOwnerId,
                item.CategoryId,
                item.Name,
                item.Description,
                item.ImageUrl,
                true));
        }
    }
}
