using ServiceBusiness.Application;
using ServiceBusiness.Domain;
using ServiceBusiness.Infrastructure.AzureStorage;

namespace ServiceBusiness.Tests.Scenarios;

public sealed class CatalogCustomizationScenarioTests
{
    [Fact]
    public async Task Company_admin_copies_starter_service_customizes_it_and_original_stays_unchanged()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("demo-owner-1");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new CompanyAdminService(store, authorization, currentUser, new TestNotificationQueue());

        var copy = await service.CopyServiceAsync("clearwater", "svc-basic");
        await service.UpsertServiceAsync(copy with
        {
            Name = "Premium Residential Cleaning",
            Description = "Customized from the starter cleaning service.",
            DefaultDurationMinutes = 60,
            DefaultPrice = 135m
        });

        var services = await store.GetServicesAsync("clearwater");
        var original = services.Single(s => s.Id == "svc-basic");
        var customized = services.Single(s => s.Id == copy.Id);

        Assert.Equal("Standard Pool Cleaning", original.Name);
        Assert.Equal(45, original.DefaultDurationMinutes);
        Assert.Equal(95m, original.DefaultPrice);
        Assert.Equal("Premium Residential Cleaning", customized.Name);
        Assert.Equal(60, customized.DefaultDurationMinutes);
        Assert.Equal(135m, customized.DefaultPrice);
        Assert.Equal(original.CategoryId, customized.CategoryId);
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
