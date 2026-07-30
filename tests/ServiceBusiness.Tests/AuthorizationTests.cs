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
        var currentUser = new TestCurrentUser("demo-user-1");
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
    public async Task Only_system_admin_can_update_system_mode()
    {
        var store = new InMemoryServiceBusinessStore();
        var companyUserAuthorization = new TenantAuthorizationService(store, new TestCurrentUser("demo-owner-1"));
        var companyUserService = new PlatformAdminService(store, companyUserAuthorization);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            companyUserService.UpdateSystemModeAsync(SystemMode.Landscape));

        var systemAdminAuthorization = new TenantAuthorizationService(store, new TestCurrentUser("sys-admin"));
        var systemAdminService = new PlatformAdminService(store, systemAdminAuthorization);

        await systemAdminService.UpdateSystemModeAsync(SystemMode.Landscape);

        var settings = await store.GetSystemSettingsAsync();
        Assert.Equal(SystemMode.Landscape, settings.SystemMode);
    }

    [Fact]
    public async Task System_admin_can_promote_registered_user_to_system_admin()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("sys-admin");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new PlatformAdminService(store, authorization);

        await service.SetSystemAdminAsync("demo-owner-1", true);

        var user = await store.GetUserAsync("demo-owner-1");
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

    [Fact]
    public async Task System_admin_can_create_and_archive_company()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("sys-admin");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new PlatformAdminService(store, authorization);

        await service.UpsertCompanyAsync(new Company(
            "Blue Sky Pools",
            "pool",
            "Blue Sky Pools",
            "hello@bluesky.example",
            "555-0300",
            "America/Los_Angeles",
            CompanyStatus.Active));
        await service.SetCompanyStatusAsync("blue-sky-pools", CompanyStatus.Suspended);

        var company = await store.GetCompanyAsync("blue-sky-pools");
        Assert.Equal("Blue Sky Pools", company!.Name);
        Assert.Equal(CompanyStatus.Suspended, company.Status);
    }

    [Fact]
    public async Task System_admin_can_create_user()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("sys-admin");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new PlatformAdminService(store, authorization);

        var user = await service.CreateUserAsync(
            "Casey Dispatcher",
            "casey.dispatcher@gmail.com",
            null,
            "555-0301",
            false,
            true);

        var persisted = await store.GetUserAsync(user.Id);
        Assert.Equal("casey.dispatcher@gmail.com", persisted!.Email);
        Assert.Equal(UserStatus.Active, persisted.Status);
    }

    [Fact]
    public async Task Company_admin_can_create_and_archive_catalog_items()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("demo-owner-1");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new CompanyAdminService(store, authorization, currentUser, new TestNotificationQueue());

        await service.UpsertMaterialCategoryAsync(new MaterialCategory(
            "Test Supplies",
            "clearwater",
            "Test Supplies",
            "Supplies for tests.",
            false,
            true));
        await service.UpsertMaterialAsync(new Material(
            "Test Net",
            "clearwater",
            "test-supplies",
            "Test Net",
            "each",
            10m,
            15m,
            true,
            true));
        await service.SetMaterialActiveAsync("clearwater", "test-net", false);

        var material = (await store.GetMaterialsAsync("clearwater")).Single(m => m.Id == "test-net");
        Assert.False(material.IsActive);

        await service.UpsertServiceCategoryAsync(new ServiceCategory(
            "Test Services",
            "clearwater",
            "Test Services",
            "Services for tests.",
            false,
            true));
        await service.UpsertServiceAsync(new ServiceOffering(
            "Test Brush",
            "clearwater",
            "test-services",
            "Test Brush",
            "Brush walls.",
            30,
            20m,
            true,
            true));
        await service.SetServiceActiveAsync("clearwater", "test-brush", false);

        var serviceOffering = (await store.GetServicesAsync("clearwater")).Single(s => s.Id == "test-brush");
        Assert.False(serviceOffering.IsActive);
    }

    [Fact]
    public async Task Company_admin_can_copy_starter_catalog_items_to_custom_records()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("demo-owner-1");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new CompanyAdminService(store, authorization, currentUser, new TestNotificationQueue());

        var materialCategory = await service.CopyMaterialCategoryAsync("clearwater", "mat-cat-chemicals");
        var material = await service.CopyMaterialAsync("clearwater", "mat-chlorine");
        var serviceCategory = await service.CopyServiceCategoryAsync("clearwater", "svc-cat-maintenance");
        var serviceOffering = await service.CopyServiceAsync("clearwater", "svc-basic");

        Assert.Equal("mat-cat-chemicals-custom", materialCategory.Id);
        Assert.False(materialCategory.IsSystemManaged);
        Assert.Equal("mat-chlorine-custom", material.Id);
        Assert.Equal("svc-cat-maintenance-custom", serviceCategory.Id);
        Assert.False(serviceCategory.IsSystemManaged);
        Assert.Equal("svc-basic-custom", serviceOffering.Id);

        var secondCopy = await service.CopyServiceAsync("clearwater", "svc-basic");
        Assert.Equal("svc-basic-custom-2", secondCopy.Id);
    }

    [Fact]
    public async Task System_admin_can_create_and_archive_global_equipment()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("sys-admin");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new CompanyAdminService(store, authorization, currentUser, new TestNotificationQueue());

        await service.UpsertPoolEquipmentCategoryAsync(new PoolEquipmentCategory(
            "Test Pumps",
            EquipmentScope.Global,
            "global",
            "Pentair",
            "Test Pumps",
            "Global test pump category.",
            false,
            true));
        await service.UpsertPoolEquipmentItemAsync(new PoolEquipmentItem(
            "Test Variable Pump",
            EquipmentScope.Global,
            "global",
            "test-pumps",
            "Test Variable Pump",
            "Global test pump.",
            "/images/pool-waterfall-hero.png",
            true));
        await service.SetPoolEquipmentItemActiveAsync(EquipmentScope.Global, "global", "test-variable-pump", false);

        var item = (await store.GetPoolEquipmentItemsAsync(EquipmentScope.Global, "global")).Single(i => i.Id == "test-variable-pump");
        Assert.False(item.IsActive);
    }

    [Fact]
    public async Task Company_admin_can_create_and_archive_company_equipment()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("demo-owner-1");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new CompanyAdminService(store, authorization, currentUser, new TestNotificationQueue());

        await service.UpsertPoolEquipmentCategoryAsync(new PoolEquipmentCategory(
            "Test Filters",
            EquipmentScope.Company,
            "clearwater",
            "Hayward",
            "Test Filters",
            "Company test filter category.",
            false,
            true));
        await service.UpsertPoolEquipmentItemAsync(new PoolEquipmentItem(
            "Test Cartridge Filter",
            EquipmentScope.Company,
            "clearwater",
            "test-filters",
            "Test Cartridge Filter",
            "Company test filter.",
            null,
            true));
        await service.SetPoolEquipmentCategoryActiveAsync(EquipmentScope.Company, "clearwater", "test-filters", false);

        var category = (await store.GetPoolEquipmentCategoriesAsync(EquipmentScope.Company, "clearwater")).Single(c => c.Id == "test-filters");
        Assert.False(category.IsActive);
    }

    [Fact]
    public async Task Company_admin_can_copy_starter_equipment_to_custom_records()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("demo-owner-1");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new CompanyAdminService(store, authorization, currentUser, new TestNotificationQueue());

        var category = await service.CopyPoolEquipmentCategoryAsync(EquipmentScope.Company, "clearwater", "clearwater-pumps");
        var item = await service.CopyPoolEquipmentItemAsync(EquipmentScope.Company, "clearwater", "clearwater-intelliflo");

        Assert.Equal("clearwater-pumps-custom", category.Id);
        Assert.False(category.IsSystemManaged);
        Assert.Equal("clearwater-intelliflo-custom", item.Id);
    }

    [Fact]
    public async Task Homeowner_can_manage_only_own_equipment()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("independent-homeowner-1");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new CompanyAdminService(store, authorization, currentUser, new TestNotificationQueue());

        await service.UpsertPoolEquipmentCategoryAsync(new PoolEquipmentCategory(
            "Homeowner Pump",
            EquipmentScope.HomeOwner,
            "independent-homeowner-1",
            "Pentair",
            "Homeowner Pump",
            "Homeowner equipment.",
            false,
            true));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpsertPoolEquipmentCategoryAsync(new PoolEquipmentCategory(
                "Other Pump",
                EquipmentScope.HomeOwner,
                "demo-owner-1",
                "Pentair",
                "Other Pump",
                "Other homeowner equipment.",
                false,
                true)));
    }

    [Fact]
    public async Task Company_admin_can_manage_company_user_access_and_roles()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("demo-owner-1");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new CompanyAdminService(store, authorization, currentUser, new TestNotificationQueue());

        await service.SetCompanyUserAccessStatusAsync("clearwater", "demo-user-1", CompanyRole.CompanyUser, MembershipStatus.Inactive);
        var inactiveMembership = (await store.GetMembershipsForCompanyAsync("clearwater"))
            .Single(m => m.UserId == "demo-user-1" && m.Role == CompanyRole.CompanyUser);
        Assert.Equal(MembershipStatus.Inactive, inactiveMembership.Status);

        await service.UpdateCompanyUserRoleAsync("clearwater", "demo-user-1", CompanyRole.CompanyUser, CompanyRole.CompanyClientUser);

        var memberships = await store.GetMembershipsForCompanyAsync("clearwater");
        Assert.Contains(memberships, m => m.UserId == "demo-user-1" && m.Role == CompanyRole.CompanyUser && m.Status == MembershipStatus.Removed);
        Assert.Contains(memberships, m => m.UserId == "demo-user-1" && m.Role == CompanyRole.CompanyClientUser && m.Status == MembershipStatus.Inactive);

        await service.SetCompanyUserAccessStatusAsync("clearwater", "demo-user-1", CompanyRole.CompanyClientUser, MembershipStatus.Active);
        var activeMembership = (await store.GetMembershipsForCompanyAsync("clearwater"))
            .Single(m => m.UserId == "demo-user-1" && m.Role == CompanyRole.CompanyClientUser);
        Assert.Equal(MembershipStatus.Active, activeMembership.Status);
    }

    [Fact]
    public async Task Company_admin_cannot_remove_last_active_company_admin()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("demo-owner-1");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new CompanyAdminService(store, authorization, currentUser, new TestNotificationQueue());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetCompanyUserAccessStatusAsync("clearwater", "demo-owner-1", CompanyRole.CompanyAdmin, MembershipStatus.Inactive));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateCompanyUserRoleAsync("clearwater", "demo-owner-1", CompanyRole.CompanyAdmin, CompanyRole.CompanyUser));
    }

    [Fact]
    public async Task Seed_data_includes_requested_test_companies_catalogs_and_users()
    {
        var store = new InMemoryServiceBusinessStore();
        var expectedCompanies = new[] { "pool1clean1", "poolclean2", "landscape1", "landscape2" };

        var companies = await store.GetCompaniesAsync();
        var users = await store.GetUsersAsync();

        foreach (var companyId in expectedCompanies)
        {
            var memberships = await store.GetMembershipsForCompanyAsync(companyId);
            Assert.Contains(companies, c => c.Id == companyId);
            Assert.True((await store.GetServicesAsync(companyId)).Count >= 5);
            Assert.True((await store.GetMaterialsAsync(companyId)).Count >= 5);
            Assert.True((await store.GetPoolEquipmentItemsAsync(EquipmentScope.Company, companyId)).Count >= 3);
            Assert.True(memberships.Count >= 5);
            Assert.Contains(memberships, m => m.Status == MembershipStatus.Active);
            Assert.Contains(memberships, m => m.Status == MembershipStatus.Pending);
            Assert.Contains(users, u => u.Email == $"owner-1@{companyId}.com");
            Assert.Contains(users, u => u.Email == $"user-1@{companyId}.com");
            Assert.Contains(users, u => u.Email == $"client-1@{companyId}.com");
        }

        Assert.True((await store.GetPoolEquipmentItemsAsync(EquipmentScope.Global, "global")).Count >= 3);
        Assert.True((await store.GetPoolEquipmentItemsAsync(EquipmentScope.HomeOwner, "independent-homeowner-1")).Count >= 1);
        Assert.Contains(users, u => u.Email == "homeowner-1@independent.com");
        Assert.Contains(users, u => u.Email == "homeowner-2@independent.com");
        Assert.Contains(users, u => u.Email == "homeowner-3@independent.com");
        Assert.Empty(await store.GetMembershipsForUserAsync("independent-homeowner-1"));
        Assert.True((await store.GetPoolEquipmentItemsAsync(EquipmentScope.HomeOwner, "independent-homeowner-2")).Count >= 1);
        Assert.True((await store.GetPoolEquipmentItemsAsync(EquipmentScope.HomeOwner, "independent-homeowner-3")).Count >= 1);

        for (var i = 1; i <= 10; i++)
        {
            var user = Assert.Single(users, u => u.Email == $"other-{i}@gmail.com");
            Assert.True(user.IsTestUser);
            Assert.Empty(await store.GetMembershipsForUserAsync(user.Id));
        }
    }

    [Fact]
    public async Task Company_dashboard_includes_setup_counts_and_pending_approval_splits()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("demo-owner-1");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new CompanyAdminService(store, authorization, currentUser, new TestNotificationQueue());

        var dashboard = await service.GetDashboardAsync("clearwater", DateOnly.FromDateTime(DateTime.Today));

        Assert.True(dashboard.CustomerCount >= 3);
        Assert.True(dashboard.EmployeeCount >= 1);
        Assert.True(dashboard.EquipmentCount >= 2);
        Assert.True(dashboard.MaterialCount >= 3);
        Assert.True(dashboard.ServiceCount >= 3);
        Assert.Equal(1, dashboard.PendingEmployeeRequests);
        Assert.Equal(0, dashboard.PendingCustomerRequests);
        Assert.Contains(dashboard.PendingAccessRequests, r =>
            r.User.Id == "demo-pending-user-1" &&
            r.Membership.Role == CompanyRole.CompanyUser &&
            r.Membership.Status == MembershipStatus.Pending);
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
