using ServiceBusiness.Application;
using ServiceBusiness.Domain;
using ServiceBusiness.Infrastructure.AzureStorage;
using Xunit;

namespace ServiceBusiness.Tests;

public sealed class OnboardingTests
{
    [Fact]
    public async Task Business_owner_registration_creates_active_company_admin_access()
    {
        var store = new InMemoryServiceBusinessStore();
        var service = new OnboardingService(store);

        var result = await service.RegisterAsync(new RegistrationSubmission(
            RegistrationAccountType.BusinessOwner,
            "owner@gmail.com",
            "Taylor Owner",
            "555-0199",
            null,
            "Pristine Pool Service",
            "555-0188",
            "office@pristine.example",
            "West valley",
            ["Weekly Cleaning"]));

        Assert.False(result.RequiresApproval);
        Assert.NotNull(result.Company);
        Assert.Equal(MembershipStatus.Active, result.Membership!.Status);
        Assert.Equal(CompanyRole.CompanyAdmin, result.Membership.Role);
    }

    [Fact]
    public async Task Business_user_registration_creates_pending_company_membership()
    {
        var store = new InMemoryServiceBusinessStore();
        var service = new OnboardingService(store);

        var result = await service.RegisterAsync(new RegistrationSubmission(
            RegistrationAccountType.BusinessUser,
            "routeuser@gmail.com",
            "Riley Route",
            "555-0177",
            "clearwater",
            null,
            null,
            null,
            null,
            [],
            "500 Scenario Way, Phoenix, AZ",
            "Side gate"));

        Assert.True(result.RequiresApproval);
        Assert.Equal("clearwater", result.Membership!.CompanyId);
        Assert.Equal(CompanyRole.CompanyUser, result.Membership.Role);
        Assert.Equal(MembershipStatus.Pending, result.Membership.Status);
    }

    [Fact]
    public async Task Independent_homeowner_registration_creates_active_owner_workspace_without_company_membership()
    {
        var store = new InMemoryServiceBusinessStore();
        var service = new OnboardingService(store);

        var result = await service.RegisterAsync(new RegistrationSubmission(
            RegistrationAccountType.IndependentHomeOwner,
            "homeowner.new@gmail.com",
            "Harper Homeowner",
            "555-0166",
            null,
            null,
            null,
            null,
            null,
            [],
            HomeAddress: "500 Scenario Way, Phoenix, AZ",
            HomeAccessNotes: "Side gate"));

        Assert.False(result.RequiresApproval);
        Assert.Null(result.Company);
        Assert.Null(result.Membership);

        var memberships = await store.GetMembershipsForUserAsync(result.User.Id);
        Assert.Empty(memberships);

        var categories = await store.GetPoolEquipmentCategoriesAsync(EquipmentScope.HomeOwner, result.User.Id);
        var items = await store.GetPoolEquipmentItemsAsync(EquipmentScope.HomeOwner, result.User.Id);
        var profile = await store.GetIndependentHomeOwnerProfileAsync(result.User.Id);
        Assert.Equal("500 Scenario Way, Phoenix, AZ", profile!.HomeAddress);
        Assert.Equal("Side gate", profile.AccessNotes);
        Assert.Contains(categories, c => c.Id == "my-pool-equipment");
        Assert.Contains(items, i => i.Id == "primary-pool-equipment");
    }

    [Fact]
    public async Task Business_owner_can_approve_pending_access_request()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("demo-owner-1");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var notificationQueue = new TestNotificationQueue();
        var service = new CompanyAdminService(store, authorization, currentUser, notificationQueue);

        var pending = await service.GetPendingAccessRequestsAsync("clearwater");
        var request = Assert.Single(pending);

        await service.DecideAccessRequestAsync(
            "clearwater",
            request.User.Id,
            request.Membership.Role,
            MembershipStatus.Active);

        var memberships = await store.GetMembershipsForUserAsync(request.User.Id);
        var approved = Assert.Single(memberships, m => m.CompanyId == "clearwater");

        Assert.Equal(MembershipStatus.Active, approved.Status);
        Assert.Equal("demo-owner-1", approved.DecidedByUserId);
        Assert.Single(notificationQueue.Decisions);
    }

    [Fact]
    public async Task Seeded_test_users_can_skip_gmail_authentication()
    {
        var store = new InMemoryServiceBusinessStore();
        var service = new OnboardingService(store);

        var result = await service.SignInAsync("owner-1@demo.example");

        Assert.NotNull(result);
        Assert.True(result.AuthenticationSkipped);
    }

    [Fact]
    public async Task Current_user_can_update_profile_contact_details()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("demo-user-1");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new UserProfileService(store, authorization);

        var updated = await service.UpdateCurrentProfileAsync(
            "Morgan Route",
            "route-notify@example.com",
            "555-0200",
            false);

        Assert.Equal("Morgan Route", updated.DisplayName);
        Assert.Equal("route-notify@example.com", updated.NotificationEmail);
        Assert.Equal("555-0200", updated.Phone);
        Assert.False(updated.EmailNotificationsEnabled);

        var persisted = await store.GetUserAsync("demo-user-1");
        Assert.Equal("Morgan Route", persisted!.DisplayName);
        Assert.False(persisted.EmailNotificationsEnabled);
    }

    private sealed class TestCurrentUser(string userId) : ICurrentUserContext
    {
        public string UserId { get; } = userId;
    }

    private sealed class TestNotificationQueue : INotificationQueue
    {
        public List<(AccessRequest Request, MembershipStatus Decision)> Decisions { get; } = [];

        public Task QueueVisitCompletedEmailAsync(ServiceHistoryItem item, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task QueueAccountApprovalDecisionEmailAsync(AccessRequest request, MembershipStatus decision, CancellationToken cancellationToken = default)
        {
            Decisions.Add((request, decision));
            return Task.CompletedTask;
        }
    }
}
