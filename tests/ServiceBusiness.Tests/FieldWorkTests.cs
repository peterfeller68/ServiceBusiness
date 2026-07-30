using ServiceBusiness.Application;
using ServiceBusiness.Domain;
using ServiceBusiness.Infrastructure.AzureStorage;
using Xunit;

namespace ServiceBusiness.Tests;

public sealed class FieldWorkTests
{
    [Fact]
    public async Task Completing_visit_persists_completion_and_marks_visit_completed()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("demo-user-1");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var notificationQueue = new TestNotificationQueue();
        var service = new FieldWorkService(store, authorization, currentUser, notificationQueue);

        await service.CompleteVisitAsync(
            "clearwater",
            "visit-1",
            ["svc-basic"],
            [new MaterialUsage("mat-chlorine", 2)],
            "Finished service.",
            "No internal issues.");

        var visit = await store.GetVisitAsync("clearwater", "visit-1");
        var completion = await store.GetVisitCompletionAsync("clearwater", "visit-1");

        Assert.Equal(VisitStatus.Completed, visit!.Status);
        Assert.NotNull(completion);
        Assert.Single(notificationQueue.Items);
    }

    [Fact]
    public async Task Catalog_overview_groups_services_and_materials_by_category()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("demo-owner-1");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new CompanyAdminService(store, authorization, currentUser, new TestNotificationQueue());

        var catalog = await service.GetCatalogOverviewAsync("clearwater");

        var maintenance = Assert.Single(catalog.ServiceGroups, group => group.Category.Id == "svc-cat-maintenance");
        Assert.Contains(maintenance.Services, item => item.Id == "svc-basic");
        Assert.Contains(maintenance.Services, item => item.Id == "svc-chem");

        var chemicals = Assert.Single(catalog.MaterialGroups, group => group.Category.Id == "mat-cat-chemicals");
        Assert.Contains(chemicals.Materials, item => item.Id == "mat-chlorine");
    }

    private sealed class TestCurrentUser(string userId) : ICurrentUserContext
    {
        public string UserId { get; } = userId;
    }

    private sealed class TestNotificationQueue : INotificationQueue
    {
        public List<ServiceHistoryItem> Items { get; } = [];

        public Task QueueVisitCompletedEmailAsync(ServiceHistoryItem item, CancellationToken cancellationToken = default)
        {
            Items.Add(item);
            return Task.CompletedTask;
        }

        public Task QueueAccountApprovalDecisionEmailAsync(AccessRequest request, MembershipStatus decision, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
