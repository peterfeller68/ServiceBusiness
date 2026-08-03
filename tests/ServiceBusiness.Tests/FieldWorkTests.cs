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

        Assert.Equal(VisitStatus.Completed, visit!.Status);
        Assert.Null(visit.InvoiceId);
        Assert.Equal("demo-user-1", visit.CompletedByUserId);
        Assert.Equal(["svc-basic"], visit.CompletedServiceIds);
        Assert.Equal([new MaterialUsage("mat-chlorine", 2)], visit.MaterialsUsed);
        Assert.Equal("Finished service.", visit.NotesToBusinessClient);
        Assert.Equal("No internal issues.", visit.InternalNotes);
        Assert.Single(notificationQueue.Items);
    }

    [Fact]
    public async Task Company_admin_visit_save_normalizes_section_15_status_rules()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("demo-owner-1");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new CompanyAdminService(store, authorization, currentUser, new TestNotificationQueue());

        await service.UpsertVisitAsync(new ServiceVisit(
            "section-15-visit",
            "clearwater",
            "client-1",
            null,
            default,
            new TimeOnly(8, 0),
            new TimeOnly(10, 0),
            VisitStatus.Scheduled,
            ["pool-cleaning-standard-service"],
            0,
            "",
            null,
            null));

        var unscheduled = await store.GetVisitAsync("clearwater", "section-15-visit");
        Assert.Equal(VisitStatus.New, unscheduled!.Status);

        await service.UpsertVisitAsync(unscheduled with
        {
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today)
        });

        var scheduled = await store.GetVisitAsync("clearwater", "section-15-visit");
        Assert.Equal(VisitStatus.New, scheduled!.Status);

        await service.UpsertVisitAsync(scheduled! with
        {
            AssignedUserId = "demo-user-1"
        });

        var assigned = await store.GetVisitAsync("clearwater", "section-15-visit");
        Assert.Equal(VisitStatus.Assigned, assigned!.Status);
    }

    [Fact]
    public async Task Company_admin_can_assign_visit_to_business_owner()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("demo-owner-1");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new CompanyAdminService(store, authorization, currentUser, new TestNotificationQueue());

        await service.AssignVisitAsync("clearwater", "visit-3", "demo-owner-1");

        var visit = await store.GetVisitAsync("clearwater", "visit-3");
        Assert.Equal("demo-owner-1", visit!.AssignedUserId);
        Assert.Equal(VisitStatus.Assigned, visit.Status);
    }

    [Fact]
    public async Task Employee_dashboard_queries_split_today_upcoming_and_completed_visits()
    {
        var store = new InMemoryServiceBusinessStore();
        var tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        await store.UpsertVisitAsync(new ServiceVisit(
            "tomorrow-assigned-visit",
            "clearwater",
            "client-1",
            "demo-user-1",
            tomorrow,
            new TimeOnly(8, 0),
            new TimeOnly(10, 0),
            VisitStatus.Assigned,
            ["svc-basic"],
            0,
            "",
            null,
            null));
        var currentUser = new TestCurrentUser("demo-user-1");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new FieldWorkService(store, authorization, currentUser, new TestNotificationQueue());

        var todayVisits = await service.GetTodayAssignedVisitsAsync("clearwater");
        var upcomingVisits = await service.GetUpcomingAssignedVisitsAsync("clearwater");
        var completedVisits = await service.GetRecentlyCompletedAssignedVisitsAsync("clearwater");

        Assert.Contains(todayVisits, item => item.Visit.Id == "visit-1");
        Assert.DoesNotContain(upcomingVisits, item => item.Visit.Id == "visit-1");
        Assert.Contains(upcomingVisits, item => item.Visit.Id == "tomorrow-assigned-visit");
        Assert.Contains(completedVisits, item => item.Visit.Id == "visit-4");

        await service.MarkVisitInProgressAsync("clearwater", "visit-4");

        var reopenedVisit = await store.GetVisitAsync("clearwater", "visit-4");
        Assert.Equal(VisitStatus.InProgress, reopenedVisit!.Status);
    }

    [Fact]
    public async Task Employee_can_update_allowed_visit_details()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("demo-user-1");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var service = new FieldWorkService(store, authorization, currentUser, new TestNotificationQueue());

        await service.UpdateAssignedVisitDetailsAsync(
            "clearwater",
            "visit-1",
            "Customer-facing notes.",
            "Owner-facing notes.",
            "Internal crew notes.",
            ["svc-basic"],
            ["svc-filter"]);

        var visit = await store.GetVisitAsync("clearwater", "visit-1");

        Assert.Equal("Customer-facing notes.", visit!.NotesToBusinessClient);
        Assert.Equal("Owner-facing notes.", visit.NotesToServiceClient);
        Assert.Equal("Internal crew notes.", visit.InternalNotes);
        Assert.Equal(["svc-basic"], visit.CompletedServiceIds);
        Assert.Equal(["svc-filter"], visit.OutOfScopeServiceIds);
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
