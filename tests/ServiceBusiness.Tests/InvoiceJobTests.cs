using ServiceBusiness.Application;
using ServiceBusiness.Domain;
using ServiceBusiness.Infrastructure.AzureStorage;
using Xunit;

namespace ServiceBusiness.Tests;

public sealed class InvoiceJobTests
{
    [Fact]
    public async Task Invoicing_service_creates_invoice_for_completed_visit_without_invoice()
    {
        var store = new InMemoryServiceBusinessStore(new SystemSettings(SystemMode.Pool, DevTest: true));
        var seedCompletedVisit = await store.GetVisitAsync("clearwater", "visit-4");
        await store.UpsertVisitAsync(seedCompletedVisit! with { InvoiceId = "seeded" });
        await store.UpsertVisitAsync(new ServiceVisit(
            "invoice-job-visit",
            "clearwater",
            "client-1",
            "demo-user-1",
            DateOnly.FromDateTime(DateTime.Today),
            new TimeOnly(8, 0),
            new TimeOnly(10, 0),
            VisitStatus.Completed,
            ["svc-filter"],
            0,
            "",
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow.AddHours(-1),
            VisitType.AdHocVisit,
            "Filter Repair",
            OutOfScopeMaterials: [new MaterialUsage("mat-tabs", 2)],
            CompletedByUserId: "demo-user-1",
            CompletedServiceIds: ["svc-filter"]));
        var invoicing = new InvoicingJobService(store);
        var emailJob = new EmailJobService(store);

        var invoices = await invoicing.CreateInvoicesForCompletedVisitsAsync();

        var invoice = Assert.Single(invoices, invoice => invoice.VisitId == "invoice-job-visit");
        Assert.Equal("000001", invoice.InvoiceId);
        Assert.Equal(InvoiceStatus.New, invoice.Status);
        Assert.True(invoice.TotalCost > 0);
        Assert.Contains("Filter Repair", invoice.InvoiceHtml);

        var visit = await store.GetVisitAsync("clearwater", "invoice-job-visit");
        Assert.Equal(invoice.InvoiceId, visit!.InvoiceId);

        var queuedEmail = Assert.Single(
            await store.GetEmailLogsAsync(),
            log => log.EmailType == "Invoice" && log.Status == EmailDeliveryStatus.New);
        Assert.Equal(invoice.InvoiceHtml, queuedEmail.Body);

        var processed = await emailJob.ProcessNewEmailLogsAsync();

        Assert.Equal(1, processed);
        var sentEmail = Assert.Single(
            await store.GetEmailLogsAsync(),
            log => log.Id == queuedEmail.Id);
        Assert.Equal(EmailDeliveryStatus.Sent, sentEmail.Status);
        Assert.NotNull(sentEmail.SentUtc);
    }

    [Fact]
    public async Task Invoicing_service_does_not_reinvoice_visits()
    {
        var store = new InMemoryServiceBusinessStore();
        var invoicing = new InvoicingJobService(store);

        var firstRun = await invoicing.CreateInvoicesForCompletedVisitsAsync();
        var secondRun = await invoicing.CreateInvoicesForCompletedVisitsAsync();

        Assert.NotEmpty(firstRun);
        Assert.Empty(secondRun);
    }

    [Fact]
    public async Task Invoice_status_moves_forward_through_workflow()
    {
        var store = new InMemoryServiceBusinessStore();
        var currentUser = new TestCurrentUser("demo-owner-1");
        var authorization = new TenantAuthorizationService(store, currentUser);
        var admin = new CompanyAdminService(store, authorization, currentUser, new TestNotificationQueue());
        var invoice = new Invoice(
            Guid.NewGuid().ToString("N"),
            "clearwater",
            "009999",
            "client-1",
            "visit-4",
            null,
            [],
            [],
            0m,
            InvoiceStatus.New,
            "<html></html>",
            DateTimeOffset.UtcNow);
        await store.UpsertInvoiceAsync(invoice);

        await admin.SetInvoiceStatusAsync("clearwater", "009999", InvoiceStatus.Invoiced);
        await admin.SetInvoiceStatusAsync("clearwater", "009999", InvoiceStatus.Paid);

        var updated = await store.GetInvoiceAsync("clearwater", "009999");
        Assert.Equal(InvoiceStatus.Paid, updated!.Status);
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
