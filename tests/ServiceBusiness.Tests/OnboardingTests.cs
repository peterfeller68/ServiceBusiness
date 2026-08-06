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
    public async Task Business_client_registration_requires_and_stores_selected_client_address()
    {
        var store = new InMemoryServiceBusinessStore();
        var service = new OnboardingService(store);
        var availableClients = await service.GetAvailableBusinessClientsAsync("clearwater");
        var selectedClient = Assert.Single(availableClients, client => client.Id == "client-1");

        var result = await service.RegisterAsync(new RegistrationSubmission(
            RegistrationAccountType.BusinessClient,
            "client-access@gmail.com",
            "Casey Client",
            "555-0178",
            "clearwater",
            null,
            null,
            null,
            null,
            [],
            BusinessClientId: selectedClient.Id));

        Assert.True(result.RequiresApproval);
        Assert.Equal("clearwater", result.Membership!.CompanyId);
        Assert.Equal(CompanyRole.CompanyClientUser, result.Membership.Role);
        Assert.Equal(MembershipStatus.Pending, result.Membership.Status);
        Assert.Equal("client-1", result.Membership.CompanyClientId);
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
        var subscription = await store.GetHomeOwnerSubscriptionAsync(result.User.Id);
        Assert.Equal("500 Scenario Way, Phoenix, AZ", profile!.HomeAddress);
        Assert.Equal("Side gate", profile.AccessNotes);
        Assert.NotNull(result.Subscription);
        Assert.NotNull(subscription);
        Assert.Equal(SubscriptionReferenceData.HomeOwnerMonthlyId, subscription!.PlanId);
        Assert.Equal(SubscriptionStatus.Trialing, subscription.Status);
        Assert.True(subscription.TrialEndsAt > DateTimeOffset.UtcNow);
        Assert.Contains(categories, c => c.Id == "my-pool-equipment");
        Assert.Contains(items, i => i.Id == "primary-pool-equipment");
    }

    [Fact]
    public async Task Independent_homeowner_registration_uses_selected_subscription_plan_and_trial_days()
    {
        var store = new InMemoryServiceBusinessStore(new SystemSettings(SystemMode.Pool, DevTest: true, HomeOwnerTrialDays: 30));
        var service = new OnboardingService(store);

        var result = await service.RegisterAsync(new RegistrationSubmission(
            RegistrationAccountType.IndependentHomeOwner,
            "annual.homeowner@gmail.com",
            "Annual Homeowner",
            "555-0167",
            null,
            null,
            null,
            null,
            null,
            [],
            HomeAddress: "600 Scenario Way, Phoenix, AZ",
            SubscriptionPlanId: SubscriptionReferenceData.HomeOwnerAnnualId));

        var subscription = await store.GetHomeOwnerSubscriptionAsync(result.User.Id);

        Assert.Equal(SubscriptionReferenceData.HomeOwnerAnnualId, subscription!.PlanId);
        Assert.Equal(SubscriptionStatus.Trialing, subscription.Status);
        Assert.InRange(
            subscription.TrialEndsAt!.Value,
            DateTimeOffset.UtcNow.AddDays(29),
            DateTimeOffset.UtcNow.AddDays(31));
    }

    [Fact]
    public async Task System_admin_can_update_persisted_homeowner_trial_days()
    {
        var store = new InMemoryServiceBusinessStore(new SystemSettings(SystemMode.Pool, DevTest: true, HomeOwnerTrialDays: 14));
        var currentUser = new TestCurrentUser("sys-admin");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var adminService = new PlatformAdminService(store, authorization);

        var saved = await adminService.UpdateSystemSettingsAsync(new SystemSettings(SystemMode.Pool, DevTest: true, HomeOwnerTrialDays: 45));

        Assert.Equal(45, saved.HomeOwnerTrialDays);
        Assert.Equal(45, (await store.GetSystemSettingsAsync()).HomeOwnerTrialDays);
    }

    [Fact]
    public async Task Updated_system_settings_drive_new_homeowner_subscription_trial()
    {
        var store = new InMemoryServiceBusinessStore(new SystemSettings(SystemMode.Pool, DevTest: true, HomeOwnerTrialDays: 14));
        var currentUser = new TestCurrentUser("sys-admin");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var adminService = new PlatformAdminService(store, authorization);
        await adminService.UpdateSystemSettingsAsync(new SystemSettings(SystemMode.Pool, DevTest: true, HomeOwnerTrialDays: 7));
        var onboarding = new OnboardingService(store);

        var result = await onboarding.RegisterAsync(new RegistrationSubmission(
            RegistrationAccountType.IndependentHomeOwner,
            "seven.day.trial@gmail.com",
            "Seven Day Trial",
            "555-0171",
            null,
            null,
            null,
            null,
            null,
            [],
            HomeAddress: "700 Scenario Way, Phoenix, AZ",
            SubscriptionPlanId: SubscriptionReferenceData.HomeOwnerMonthlyId));
        var subscription = await store.GetHomeOwnerSubscriptionAsync(result.User.Id);

        Assert.InRange(
            subscription!.TrialEndsAt!.Value,
            DateTimeOffset.UtcNow.AddDays(6),
            DateTimeOffset.UtcNow.AddDays(8));
    }

    [Fact]
    public async Task System_admin_can_update_homeowner_subscription_trial_end()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("sys-admin");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new PlatformAdminService(store, authorization);
        var newTrialEnd = DateTimeOffset.UtcNow.AddDays(45);

        var updated = await service.UpdateHomeOwnerSubscriptionTrialEndAsync("independent-homeowner-1", newTrialEnd);

        Assert.Equal(newTrialEnd, updated.TrialEndsAt);
        var persisted = await store.GetHomeOwnerSubscriptionAsync("independent-homeowner-1");
        Assert.Equal(newTrialEnd, persisted!.TrialEndsAt);
    }

    [Fact]
    public async Task System_admin_can_update_subscription_plan_price_and_provider_price_id()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("sys-admin");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new PlatformAdminService(store, authorization);

        var saved = await service.UpsertSubscriptionPlanAsync(new SubscriptionPlan(
            SubscriptionReferenceData.HomeOwnerMonthlyId,
            "Home Owner Monthly Plus",
            "Updated homeowner monthly plan.",
            SubscriptionBillingInterval.Monthly,
            29m,
            true,
            5,
            "price_test_monthly"));

        var plans = await service.GetSubscriptionPlansAsync();
        var persisted = Assert.Single(plans, plan => plan.Id == SubscriptionReferenceData.HomeOwnerMonthlyId);

        Assert.Equal("Home Owner Monthly Plus", saved.Name);
        Assert.Equal(29m, persisted.Price);
        Assert.Equal("price_test_monthly", persisted.ProviderPriceId);
        Assert.Equal(5, persisted.SortOrder);
    }

    [Fact]
    public async Task System_admin_can_deactivate_subscription_plan_without_deleting_it()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("sys-admin");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new PlatformAdminService(store, authorization);

        await service.SetSubscriptionPlanActiveAsync(SubscriptionReferenceData.HomeOwnerAnnualId, false);

        var plan = await store.GetSubscriptionPlanAsync(SubscriptionReferenceData.HomeOwnerAnnualId);
        Assert.NotNull(plan);
        Assert.False(plan!.IsActive);
    }

    [Fact]
    public async Task Subscription_plan_management_requires_system_admin()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("demo-owner-1");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new PlatformAdminService(store, authorization);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpsertSubscriptionPlanAsync(new SubscriptionPlan(
                "homeowner-premium",
                "Home Owner Premium",
                "",
                SubscriptionBillingInterval.Monthly,
                39m,
                true,
                30,
                null)));
    }

    [Fact]
    public async Task Subscription_plan_price_cannot_be_negative()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("sys-admin");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new PlatformAdminService(store, authorization);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpsertSubscriptionPlanAsync(new SubscriptionPlan(
                "bad-plan",
                "Bad Plan",
                "",
                SubscriptionBillingInterval.Monthly,
                -1m,
                true,
                10,
                null)));
    }

    [Fact]
    public void Subscription_entitlement_requires_active_or_current_trial_status()
    {
        var now = DateTimeOffset.UtcNow;
        var trialing = new HomeOwnerSubscription(
            "sub-1",
            "owner-1",
            SubscriptionReferenceData.HomeOwnerMonthlyId,
            SubscriptionStatus.Trialing,
            now.AddDays(1),
            now,
            now.AddDays(1),
            false,
            now,
            now);
        var pastDue = trialing with { Status = SubscriptionStatus.PastDue };
        var expiredTrial = trialing with { TrialEndsAt = now.AddDays(-1) };

        Assert.True(SubscriptionService.HasActiveEntitlement(trialing, now));
        Assert.False(SubscriptionService.HasActiveEntitlement(pastDue, now));
        Assert.False(SubscriptionService.HasActiveEntitlement(expiredTrial, now));
    }

    [Fact]
    public async Task Payment_provider_events_are_idempotent_and_drive_subscription_status()
    {
        var store = new InMemoryServiceBusinessStore();
        var service = new PaymentIntegrationService(store);

        var updated = await service.ApplySubscriptionStatusFromTrustedEventAsync(
            "independent-homeowner-1",
            SubscriptionStatus.Active,
            "Stripe",
            "evt_123",
            "test",
            "customer.subscription.updated",
            "Subscription active");
        var duplicate = await service.ProcessProviderEventAsync(
            "Stripe",
            "evt_123",
            "test",
            "customer.subscription.updated",
            "independent-homeowner-1",
            "Subscription active");
        var paymentEvents = await store.GetPaymentProviderEventsAsync();

        Assert.Equal(SubscriptionStatus.Active, updated.Status);
        Assert.Equal(PaymentEventProcessingStatus.Duplicate, duplicate.Status);
        Assert.Single(paymentEvents);
    }

    [Fact]
    public async Task Homeowner_checkout_session_updates_subscription_provider_references()
    {
        var store = new InMemoryServiceBusinessStore();
        var gateway = new TestPaymentProviderGateway();
        var service = new PaymentIntegrationService(store, gateway);

        var session = await service.CreateHomeOwnerCheckoutSessionAsync(
            "independent-homeowner-1",
            "https://example.test/success",
            "https://example.test/cancel");

        var subscription = await store.GetHomeOwnerSubscriptionAsync("independent-homeowner-1");

        Assert.Equal("cs_test_123", session.CheckoutSessionId);
        Assert.Equal("https://stripe.example/checkout", session.Url);
        Assert.Equal("cs_test_123", subscription!.ProviderCheckoutSessionId);
        Assert.Equal("cus_test_123", subscription.ProviderCustomerId);
        Assert.Equal("sub_test_123", subscription.ProviderSubscriptionId);
    }

    [Fact]
    public async Task Homeowner_checkout_session_writes_payment_api_logs()
    {
        var store = new InMemoryServiceBusinessStore();
        var gateway = new TestPaymentProviderGateway();
        var service = new PaymentIntegrationService(store, gateway);

        await service.CreateHomeOwnerCheckoutSessionAsync(
            "independent-homeowner-1",
            "https://example.test/success",
            "https://example.test/cancel");

        var logs = await store.GetPaymentOperationLogsAsync();

        Assert.Contains(logs, log =>
            log.Operation == PaymentOperationType.CheckoutSession &&
            log.Status == PaymentOperationStatus.Requested &&
            log.UserId == "independent-homeowner-1");
        Assert.Contains(logs, log =>
            log.Operation == PaymentOperationType.CheckoutSession &&
            log.Status == PaymentOperationStatus.Succeeded &&
            log.ProviderCheckoutSessionId == "cs_test_123");
    }

    [Fact]
    public async Task Homeowner_customer_portal_requires_provider_customer()
    {
        var store = new InMemoryServiceBusinessStore();
        var gateway = new TestPaymentProviderGateway();
        var service = new PaymentIntegrationService(store, gateway);
        await store.UpsertHomeOwnerSubscriptionAsync((await store.GetHomeOwnerSubscriptionAsync("independent-homeowner-1"))! with
        {
            ProviderCustomerId = "cus_test_123"
        });

        var session = await service.CreateHomeOwnerPortalSessionAsync(
            "independent-homeowner-1",
            "https://example.test/profile");

        Assert.Equal("bps_test_123", session.PortalSessionId);
        Assert.Equal("https://stripe.example/portal", session.Url);
    }

    [Fact]
    public async Task Webhook_processing_updates_subscription_once_for_duplicate_provider_event()
    {
        var store = new InMemoryServiceBusinessStore();
        var gateway = new TestPaymentProviderGateway
        {
            WebhookEvent = new PaymentProviderWebhookEvent(
                "Stripe",
                "test",
                "evt_checkout_completed",
                "checkout.session.completed",
                "independent-homeowner-1",
                "cus_test_456",
                "sub_test_456",
                "cs_test_456",
                SubscriptionStatus.Active,
                null,
                null,
                null,
                "Checkout completed")
        };
        var service = new PaymentIntegrationService(store, gateway);

        var first = await service.ProcessWebhookAsync("{}", "test-signature");
        var duplicate = await service.ProcessWebhookAsync("{}", "test-signature");
        var subscription = await store.GetHomeOwnerSubscriptionAsync("independent-homeowner-1");
        var events = await store.GetPaymentProviderEventsAsync();

        Assert.Equal(PaymentEventProcessingStatus.Processed, first.Status);
        Assert.Equal(PaymentEventProcessingStatus.Duplicate, duplicate.Status);
        Assert.Single(events);
        Assert.Equal(SubscriptionStatus.Active, subscription!.Status);
        Assert.Equal("cus_test_456", subscription.ProviderCustomerId);
        Assert.Equal("sub_test_456", subscription.ProviderSubscriptionId);
        Assert.Equal("cs_test_456", subscription.ProviderCheckoutSessionId);
    }

    [Fact]
    public async Task Webhook_processing_writes_provider_event_and_payment_api_log()
    {
        var store = new InMemoryServiceBusinessStore();
        var gateway = new TestPaymentProviderGateway
        {
            WebhookEvent = new PaymentProviderWebhookEvent(
                "Stripe",
                "test",
                "evt_payment_log",
                "checkout.session.completed",
                "independent-homeowner-1",
                "cus_test_log",
                "sub_test_log",
                "cs_test_log",
                SubscriptionStatus.Active,
                null,
                null,
                null,
                "Checkout completed")
        };
        var service = new PaymentIntegrationService(store, gateway);

        await service.ProcessWebhookAsync("{}", "test-signature");

        var providerEvent = Assert.Single(await store.GetPaymentProviderEventsAsync(), paymentEvent => paymentEvent.Id == "stripe:evt_payment_log");
        var apiLog = Assert.Single(await store.GetPaymentOperationLogsAsync(), log =>
            log.Operation == PaymentOperationType.Webhook &&
            log.Status == PaymentOperationStatus.Succeeded &&
            log.ProviderEventId == "evt_payment_log");

        Assert.Equal(PaymentEventProcessingStatus.Processed, providerEvent.Status);
        Assert.Equal("cs_test_log", apiLog.ProviderCheckoutSessionId);
    }

    [Fact]
    public async Task Payment_logs_require_system_admin()
    {
        var store = new InMemoryServiceBusinessStore();
        await store.UpsertPaymentOperationLogAsync(new PaymentOperationLog(
            "payment-operation-test",
            PaymentOperationType.CheckoutReturn,
            PaymentOperationStatus.Succeeded,
            "Stripe",
            "test",
            null,
            null,
            null,
            null,
            null,
            "cs_test_123",
            null,
            "Checkout browser returned success.",
            null,
            DateTimeOffset.UtcNow));
        var sysAdmin = new PaymentLogService(store, new TenantAuthorizationService(store, new TestCurrentUser("sys-admin")));
        var businessOwner = new PaymentLogService(store, new TenantAuthorizationService(store, new TestCurrentUser("demo-owner-1")));

        Assert.Single(await sysAdmin.GetPaymentOperationLogsAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => businessOwner.GetPaymentOperationLogsAsync());
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
    public async Task Seeded_test_users_can_skip_gmail_authentication_by_user_id()
    {
        var store = new InMemoryServiceBusinessStore();
        var service = new OnboardingService(store);

        var result = await service.SignInAsync("demo-owner-1");

        Assert.NotNull(result);
        Assert.Equal("demo-owner-1", result.User.Id);
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

    private sealed class TestPaymentProviderGateway : IPaymentProviderGateway
    {
        public PaymentProviderWebhookEvent WebhookEvent { get; set; } = new(
            "Stripe",
            "test",
            "evt_test",
            "customer.subscription.updated",
            "independent-homeowner-1",
            "cus_test_123",
            "sub_test_123",
            null,
            SubscriptionStatus.Active,
            null,
            null,
            null,
            "Subscription updated");

        public Task<PaymentCheckoutSession> CreateSubscriptionCheckoutSessionAsync(
            AppUser user,
            HomeOwnerSubscription subscription,
            SubscriptionPlan plan,
            string successUrl,
            string cancelUrl,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PaymentCheckoutSession(
                "Stripe",
                "test",
                "cs_test_123",
                "https://stripe.example/checkout",
                "cus_test_123",
                "sub_test_123"));

        public Task<PaymentPortalSession> CreateCustomerPortalSessionAsync(
            string providerCustomerId,
            string returnUrl,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PaymentPortalSession(
                "Stripe",
                "test",
                "bps_test_123",
                "https://stripe.example/portal"));

        public PaymentProviderWebhookEvent ParseWebhookEvent(string payload, string signatureHeader) => WebhookEvent;
    }
}
