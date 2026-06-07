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
    private readonly List<ServiceOffering> services = [];
    private readonly List<Material> materials = [];
    private readonly List<ServiceVisit> visits = [];
    private readonly List<VisitCompletion> completions = [];
    private readonly List<EmailLogEntry> emailLogs = [];

    public InMemoryServiceBusinessStore()
    {
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

    public Task<IReadOnlyList<ServiceOffering>> GetServicesAsync(string companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ServiceOffering>>(services.Where(s => s.CompanyId == companyId).ToList());

    public Task<IReadOnlyList<Material>> GetMaterialsAsync(string companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Material>>(materials.Where(m => m.CompanyId == companyId).ToList());

    public Task<IReadOnlyList<ServiceVisit>> GetVisitsByDateAsync(string companyId, DateOnly date, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ServiceVisit>>(visits.Where(v => v.CompanyId == companyId && v.ScheduledDate == date).OrderBy(v => v.ServiceWindowStart).ToList());

    public Task<IReadOnlyList<ServiceVisit>> GetVisitsForUserByDateAsync(string companyId, string userId, DateOnly date, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ServiceVisit>>(visits.Where(v => v.CompanyId == companyId && v.AssignedUserId == userId && v.ScheduledDate == date).OrderBy(v => v.RouteOrder).ToList());

    public Task<IReadOnlyList<ServiceVisit>> GetVisitsForClientAsync(string companyId, string clientId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ServiceVisit>>(visits.Where(v => v.CompanyId == companyId && v.CompanyClientId == clientId).ToList());

    public Task<ServiceVisit?> GetVisitAsync(string companyId, string visitId, CancellationToken cancellationToken = default) =>
        Task.FromResult(visits.FirstOrDefault(v => v.CompanyId == companyId && v.Id == visitId));

    public Task<VisitCompletion?> GetVisitCompletionAsync(string companyId, string visitId, CancellationToken cancellationToken = default) =>
        Task.FromResult(completions.FirstOrDefault(c => c.CompanyId == companyId && c.VisitId == visitId));

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

    public Task UpsertClientAsync(CompanyClient client, CancellationToken cancellationToken = default)
    {
        Upsert(clients, client, existing => existing.CompanyId == client.CompanyId && existing.Id == client.Id);
        return Task.CompletedTask;
    }

    public Task UpsertServiceAsync(ServiceOffering service, CancellationToken cancellationToken = default)
    {
        Upsert(services, service, existing => existing.CompanyId == service.CompanyId && existing.Id == service.Id);
        return Task.CompletedTask;
    }

    public Task UpsertMaterialAsync(Material material, CancellationToken cancellationToken = default)
    {
        Upsert(materials, material, existing => existing.CompanyId == material.CompanyId && existing.Id == material.Id);
        return Task.CompletedTask;
    }

    public Task UpsertVisitAsync(ServiceVisit visit, CancellationToken cancellationToken = default)
    {
        Upsert(visits, visit, existing => existing.CompanyId == visit.CompanyId && existing.Id == visit.Id);
        return Task.CompletedTask;
    }

    public Task UpsertVisitCompletionAsync(VisitCompletion completion, CancellationToken cancellationToken = default)
    {
        Upsert(completions, completion, existing => existing.CompanyId == completion.CompanyId && existing.VisitId == completion.VisitId);
        return Task.CompletedTask;
    }

    public Task UpsertEmailLogAsync(EmailLogEntry emailLog, CancellationToken cancellationToken = default)
    {
        Upsert(emailLogs, emailLog, existing => existing.Id == emailLog.Id);
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

    private void Seed()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        users.AddRange([
            new("sys-admin", null, "system@example.com", "system.test@example.com", "Sam System", "555-0101", null, true, true, UserStatus.Active),
            new("admin-1", null, "owner@clearwater.example", "owner.test@example.com", "Avery Owner", "555-0102", null, false, true, UserStatus.Active),
            new("tech-1", null, "morgan@clearwater.example", "morgan.test@example.com", "Morgan Tech", "555-0103", null, false, true, UserStatus.Active),
            new("client-user-1", null, "homeowner@example.com", "homeowner.test@example.com", "Jordan Homeowner", "555-0104", null, false, true, UserStatus.Active),
            new("new-tech", null, "pending.tech@gmail.com", "pending.tech.test@example.com", "Parker Pending", "555-0105", null, false, true, UserStatus.Active)
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
            new("clearwater", "admin-1", CompanyRole.CompanyAdmin, MembershipStatus.Active, DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow.AddDays(-30), "sys-admin"),
            new("clearwater", "tech-1", CompanyRole.CompanyUser, MembershipStatus.Active, DateTimeOffset.UtcNow.AddDays(-25), DateTimeOffset.UtcNow.AddDays(-25), "admin-1"),
            new("clearwater", "client-user-1", CompanyRole.CompanyClientUser, MembershipStatus.Active, DateTimeOffset.UtcNow.AddDays(-12), DateTimeOffset.UtcNow.AddDays(-12), "admin-1"),
            new("clearwater", "new-tech", CompanyRole.CompanyUser, MembershipStatus.Pending, DateTimeOffset.UtcNow.AddHours(-8), null, null)
        ]);

        clientTypes.AddRange([
            new("weekly", "clearwater", "Weekly Maintenance", BillingFrequency.Weekly, 145m, true),
            new("ffs", "clearwater", "Fee For Service", BillingFrequency.FeeForService, 0m, true)
        ]);

        clients.AddRange([
            new("client-1", "clearwater", "Diaz Residence", "Elena Diaz", "elena@example.com", "555-0111", "1142 Palm View Dr, Phoenix, AZ", "Gate code 2468. Equipment is on left side yard.", "weekly", null, true),
            new("client-2", "clearwater", "Nguyen Residence", "Ben Nguyen", "ben@example.com", "555-0112", "89 Desert Bloom Ln, Phoenix, AZ", "Use side entrance. Dogs are inside during service window.", "weekly", 165m, true),
            new("client-3", "clearwater", "Patel Residence", "Mina Patel", "mina@example.com", "555-0113", "720 Citrus Way, Phoenix, AZ", "Text before arrival.", "ffs", null, true)
        ]);

        services.AddRange([
            new("svc-basic", "clearwater", "Standard Pool Cleaning", "Skim, brush, vacuum, and basket check.", 45, 95m, true, true),
            new("svc-chem", "clearwater", "Chemical Balance", "Test and balance pool chemistry.", 15, 35m, true, true),
            new("svc-filter", "clearwater", "Filter Cleaning", "Clean filter cartridge or backwash as needed.", 30, 65m, true, true)
        ]);

        materials.AddRange([
            new("mat-chlorine", "clearwater", "Chlorine", "lb", 3.50m, 6.00m, true, true),
            new("mat-acid", "clearwater", "Muriatic Acid", "gal", 7.00m, 12.00m, true, true),
            new("mat-tabs", "clearwater", "Tabs", "each", 1.25m, 2.50m, true, true)
        ]);

        visits.AddRange([
            new("visit-1", "clearwater", "client-1", "tech-1", today, new TimeOnly(8, 0), new TimeOnly(10, 0), VisitStatus.Assigned, ["svc-basic", "svc-chem"], 1, "Check chlorine levels closely.", null, null),
            new("visit-2", "clearwater", "client-2", "tech-1", today, new TimeOnly(10, 0), new TimeOnly(12, 0), VisitStatus.Assigned, ["svc-basic"], 2, "Customer requested photo after service in future phase.", null, null),
            new("visit-3", "clearwater", "client-3", null, today, new TimeOnly(13, 0), new TimeOnly(15, 0), VisitStatus.Scheduled, ["svc-filter"], 0, "Needs assignment.", null, null),
            new("visit-4", "clearwater", "client-1", "tech-1", today.AddDays(-7), new TimeOnly(8, 0), new TimeOnly(10, 0), VisitStatus.Completed, ["svc-basic", "svc-chem"], 1, "", new DateTimeOffset(today.AddDays(-7).ToDateTime(new TimeOnly(8, 15), DateTimeKind.Local)), new DateTimeOffset(today.AddDays(-7).ToDateTime(new TimeOnly(9, 0), DateTimeKind.Local)))
        ]);

        completions.Add(new(
            "visit-4",
            "clearwater",
            "tech-1",
            ["svc-basic", "svc-chem"],
            [new("mat-chlorine", 2)],
            "Pool cleaned and chemicals balanced.",
            "Slight algae starting near steps.",
            DateTimeOffset.Now.AddDays(-7)));
    }
}
