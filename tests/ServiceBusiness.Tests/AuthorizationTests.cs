using ServiceBusiness.Application;
using ServiceBusiness.Domain;
using ServiceBusiness.Infrastructure.AzureStorage;
using Xunit;

namespace ServiceBusiness.Tests;

public sealed class AuthorizationTests
{
    [Fact]
    public async Task Company_user_cannot_read_admin_dashboard()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("tech-1");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new CompanyAdminService(store, authorization, currentUser, new TestNotificationQueue());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetDashboardAsync("clearwater", DateOnly.FromDateTime(DateTime.Today)));
    }

    [Fact]
    public async Task System_admin_can_read_platform_companies()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("sys-admin");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new PlatformAdminService(store, authorization);

        var companies = await service.GetCompaniesAsync();

        Assert.Contains(companies, c => c.Id == "clearwater");
    }

    [Fact]
    public async Task System_admin_can_promote_registered_user_to_system_admin()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("sys-admin");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new PlatformAdminService(store, authorization);

        await service.SetSystemAdminAsync("admin-1", true);

        var user = await store.GetUserAsync("admin-1");
        Assert.True(user!.IsSystemAdmin);
    }

    [Fact]
    public async Task System_admin_cannot_disable_self()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("sys-admin");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new PlatformAdminService(store, authorization);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetUserStatusAsync("sys-admin", UserStatus.Disabled));
    }

    [Fact]
    public async Task System_admin_can_update_role_definition_permissions()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("sys-admin");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new PlatformAdminService(store, authorization);

        await service.UpdateRoleDefinitionAsync(new RoleDefinition(
            CompanyRole.CompanyUser,
            "Route Technician",
            "Completes assigned route work.",
            true,
            ["visits.complete", "visits.assigned.view", "visits.complete"]));

        var role = (await store.GetRoleDefinitionsAsync()).Single(r => r.Role == CompanyRole.CompanyUser);
        Assert.Equal("Route Technician", role.DisplayName);
        Assert.Equal(["visits.assigned.view", "visits.complete"], role.Permissions);
    }

    [Fact]
    public async Task Role_definition_requires_at_least_one_permission()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("sys-admin");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new PlatformAdminService(store, authorization);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateRoleDefinitionAsync(new RoleDefinition(
                CompanyRole.CompanyUser,
                "Route Technician",
                "Completes assigned route work.",
                true,
                [])));
    }

    [Fact]
    public async Task Legacy_role_definition_without_permissions_gets_default_permissions()
    {
        var store = new InMemoryServiceBusinessStore();
        await store.UpsertRoleDefinitionAsync(new RoleDefinition(
            CompanyRole.CompanyUser,
            "Business User",
            "Legacy row without permissions.",
            true,
            null!));
        var currentUser = new TestCurrentUser("sys-admin");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new PlatformAdminService(store, authorization);

        var role = (await service.GetRoleDefinitionsAsync()).Single(r => r.Role == CompanyRole.CompanyUser);

        Assert.Equal(["materials.record", "visits.assigned.view", "visits.complete", "visits.start"], role.Permissions);
    }

    private sealed class TestCurrentUser(string userId) : ICurrentUserContext
    {
        public string UserId { get; } = userId;
    }

    private sealed class TestNotificationQueue : INotificationQueue
    {
        public Task QueueVisitCompletedEmailAsync(ServiceHistoryItem item, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task QueueAccountApprovalDecisionEmailAsync(AccessRequest request, MembershipStatus decision, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
