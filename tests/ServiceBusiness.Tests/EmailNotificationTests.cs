using Microsoft.Extensions.Configuration;
using ServiceBusiness.Application;
using ServiceBusiness.Domain;
using ServiceBusiness.Infrastructure.AzureStorage;
using ServiceBusiness.Infrastructure.Integrations;
using Xunit;

namespace ServiceBusiness.Tests;

public sealed class EmailNotificationTests
{
    [Fact]
    public async Task Approval_email_for_test_user_is_rerouted_and_logged()
    {
        var store = new InMemoryServiceBusinessStore();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:TestRecipientEmail"] = "test-inbox@example.com"
            })
            .Build();
        var queue = new AzureCommunicationEmailNotificationQueue(store, configuration);
        var user = (await store.GetUserAsync("demo-pending-user-1"))!;
        var company = (await store.GetCompanyAsync("clearwater"))!;
        var role = (await store.GetRoleDefinitionsAsync()).First(r => r.Role == CompanyRole.CompanyUser);
        var membership = (await store.GetMembershipsForUserAsync(user.Id)).Single();

        await queue.QueueAccountApprovalDecisionEmailAsync(
            new AccessRequest(membership, user, company, role),
            MembershipStatus.Active);

        var email = Assert.Single(await store.GetEmailLogsAsync());
        Assert.Equal(EmailDeliveryStatus.TestRerouted, email.Status);
        Assert.Equal("pending-user-1.notify@demo.example", email.OriginalRecipientEmail);
        Assert.Equal("test-inbox@example.com", email.RecipientEmail);
    }

    [Fact]
    public async Task Approval_email_is_suppressed_when_user_disables_email_notifications()
    {
        var store = new InMemoryServiceBusinessStore();
        var queue = new AzureCommunicationEmailNotificationQueue(store, new ConfigurationBuilder().Build());
        var user = (await store.GetUserAsync("demo-pending-user-1"))! with { EmailNotificationsEnabled = false };
        await store.UpsertUserAsync(user);

        var company = (await store.GetCompanyAsync("clearwater"))!;
        var role = (await store.GetRoleDefinitionsAsync()).First(r => r.Role == CompanyRole.CompanyUser);
        var membership = (await store.GetMembershipsForUserAsync(user.Id)).Single();

        await queue.QueueAccountApprovalDecisionEmailAsync(
            new AccessRequest(membership, user, company, role),
            MembershipStatus.Active);

        var email = Assert.Single(await store.GetEmailLogsAsync());
        Assert.Equal(EmailDeliveryStatus.Suppressed, email.Status);
        Assert.Equal("User disabled email notifications.", email.FailureReason);
        Assert.Null(email.ProviderMessageId);
        Assert.Null(email.SentUtc);
    }

    [Fact]
    public async Task Email_log_service_filters_system_admin_logs_by_system_mode()
    {
        var store = new InMemoryServiceBusinessStore();
        await SeedEmailLogAsync(store, "pool-log", "clearwater", "pool@example.com");
        await SeedEmailLogAsync(store, "landscape-log", "landscape1", "landscape@example.com");
        var service = CreateService(store, "sys-admin");

        var poolLogs = await service.GetVisibleEmailLogsAsync(SystemMode.Pool);
        var landscapeLogs = await service.GetVisibleEmailLogsAsync(SystemMode.Landscape);

        Assert.Contains(poolLogs, log => log.Id == "pool-log");
        Assert.DoesNotContain(poolLogs, log => log.Id == "landscape-log");
        Assert.Contains(landscapeLogs, log => log.Id == "landscape-log");
        Assert.DoesNotContain(landscapeLogs, log => log.Id == "pool-log");
    }

    [Fact]
    public async Task Email_log_service_limits_business_owner_to_owned_company()
    {
        var store = new InMemoryServiceBusinessStore();
        await SeedEmailLogAsync(store, "owner-company-log", "clearwater", "pool@example.com");
        await SeedEmailLogAsync(store, "other-company-log", "landscape1", "landscape@example.com");
        var service = CreateService(store, "demo-owner-1");

        var logs = await service.GetVisibleEmailLogsAsync(SystemMode.Pool);

        Assert.Contains(logs, log => log.Id == "owner-company-log");
        Assert.DoesNotContain(logs, log => log.Id == "other-company-log");
    }

    [Fact]
    public async Task Email_log_service_blocks_business_employee_access()
    {
        var store = new InMemoryServiceBusinessStore();
        await SeedEmailLogAsync(store, "employee-log", "clearwater", "user-1@demo.example", recipientUserId: "demo-user-1");
        var service = CreateService(store, "demo-user-1");

        var logs = await service.GetVisibleEmailLogsAsync(SystemMode.Pool);

        Assert.Empty(logs);
    }

    [Fact]
    public async Task Email_log_service_limits_client_and_homeowner_to_their_messages()
    {
        var store = new InMemoryServiceBusinessStore();
        await SeedEmailLogAsync(store, "client-log", "clearwater", "client-1.notify@demo.example");
        await SeedEmailLogAsync(store, "homeowner-log", null, "homeowner-1.notify@independent.com");
        await SeedEmailLogAsync(store, "other-log", "clearwater", "other@example.com");

        var clientLogs = await CreateService(store, "demo-client-1").GetVisibleEmailLogsAsync(SystemMode.Pool);
        var homeownerLogs = await CreateService(store, "independent-homeowner-1").GetVisibleEmailLogsAsync(SystemMode.Pool);

        Assert.Contains(clientLogs, log => log.Id == "client-log");
        Assert.DoesNotContain(clientLogs, log => log.Id == "other-log");
        Assert.Contains(homeownerLogs, log => log.Id == "homeowner-log");
        Assert.DoesNotContain(homeownerLogs, log => log.Id == "other-log");
    }

    [Fact]
    public async Task Email_log_service_filters_visible_logs_by_created_date()
    {
        var store = new InMemoryServiceBusinessStore();
        await SeedEmailLogAsync(
            store,
            "selected-date-log",
            "clearwater",
            "pool@example.com",
            createdUtc: new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero));
        await SeedEmailLogAsync(
            store,
            "other-date-log",
            "clearwater",
            "pool@example.com",
            createdUtc: new DateTimeOffset(2026, 1, 16, 12, 0, 0, TimeSpan.Zero));
        var service = CreateService(store, "sys-admin");

        var logs = await service.GetVisibleEmailLogsAsync(SystemMode.Pool, new DateOnly(2026, 1, 15));

        Assert.Contains(logs, log => log.Id == "selected-date-log");
        Assert.DoesNotContain(logs, log => log.Id == "other-date-log");
    }

    [Fact]
    public async Task Email_job_marks_test_user_messages_sent_without_provider_send()
    {
        var store = new InMemoryServiceBusinessStore(new SystemSettings(SystemMode.Pool, DevTest: false));
        await SeedEmailLogAsync(
            store,
            "test-user-new-log",
            "clearwater",
            "client-1.notify@demo.example",
            "demo-client-1",
            EmailDeliveryStatus.New);
        var sender = new TestEmailSender(EmailSendResult.Sent("provider-message"));
        var job = new EmailJobService(store, sender);

        var processed = await job.ProcessNewEmailLogsAsync();

        var log = Assert.Single(await store.GetEmailLogsAsync(), log => log.Id == "test-user-new-log");
        Assert.Equal(1, processed);
        Assert.Equal(0, sender.SendCount);
        Assert.Equal(EmailDeliveryStatus.Sent, log.Status);
        Assert.Null(log.ProviderMessageId);
        Assert.NotNull(log.SentUtc);
    }

    [Fact]
    public async Task Email_job_sends_non_test_user_messages_through_provider()
    {
        var store = new InMemoryServiceBusinessStore(new SystemSettings(SystemMode.Pool, DevTest: false));
        await store.UpsertUserAsync(new AppUser(
            "real-user",
            null,
            "real@example.com",
            "real.notify@example.com",
            "Real User",
            "555-0199",
            null,
            false,
            false,
            true,
            UserStatus.Active));
        await SeedEmailLogAsync(
            store,
            "real-user-new-log",
            "clearwater",
            "real.notify@example.com",
            "real-user",
            EmailDeliveryStatus.New);
        var sender = new TestEmailSender(EmailSendResult.Sent("provider-message"));
        var job = new EmailJobService(store, sender);

        await job.ProcessNewEmailLogsAsync();

        var log = Assert.Single(await store.GetEmailLogsAsync(), log => log.Id == "real-user-new-log");
        Assert.Equal(1, sender.SendCount);
        Assert.Equal(EmailDeliveryStatus.Sent, log.Status);
        Assert.Equal("provider-message", log.ProviderMessageId);
        Assert.NotNull(log.SentUtc);
    }

    [Fact]
    public async Task Email_job_records_provider_failure_message()
    {
        var store = new InMemoryServiceBusinessStore(new SystemSettings(SystemMode.Pool, DevTest: false));
        await store.UpsertUserAsync(new AppUser(
            "real-user",
            null,
            "real@example.com",
            "real.notify@example.com",
            "Real User",
            "555-0199",
            null,
            false,
            false,
            true,
            UserStatus.Active));
        await SeedEmailLogAsync(
            store,
            "failed-provider-log",
            "clearwater",
            "real.notify@example.com",
            "real-user",
            EmailDeliveryStatus.New);
        var job = new EmailJobService(store, new TestEmailSender(EmailSendResult.Failed("Provider rejected the message.")));

        await job.ProcessNewEmailLogsAsync();

        var log = Assert.Single(await store.GetEmailLogsAsync(), log => log.Id == "failed-provider-log");
        Assert.Equal(EmailDeliveryStatus.Failed, log.Status);
        Assert.Equal("Provider rejected the message.", log.FailureReason);
        Assert.Null(log.SentUtc);
    }

    [Fact]
    public async Task Email_job_records_invalid_recipient_failure_without_provider_send()
    {
        var store = new InMemoryServiceBusinessStore(new SystemSettings(SystemMode.Pool, DevTest: false));
        await SeedEmailLogAsync(
            store,
            "invalid-recipient-log",
            "clearwater",
            "invalid-recipient",
            status: EmailDeliveryStatus.New);
        var sender = new TestEmailSender(EmailSendResult.Sent("provider-message"));
        var job = new EmailJobService(store, sender);

        await job.ProcessNewEmailLogsAsync();

        var log = Assert.Single(await store.GetEmailLogsAsync(), log => log.Id == "invalid-recipient-log");
        Assert.Equal(0, sender.SendCount);
        Assert.Equal(EmailDeliveryStatus.Failed, log.Status);
        Assert.Equal("Recipient email address is not valid.", log.FailureReason);
        Assert.Null(log.SentUtc);
    }

    private static EmailLogService CreateService(InMemoryServiceBusinessStore store, string userId)
    {
        var currentUser = new TestCurrentUser(userId);
        return new EmailLogService(store, new TenantAuthorizationService(store, currentUser));
    }

    private static Task SeedEmailLogAsync(
        InMemoryServiceBusinessStore store,
        string id,
        string? companyId,
        string recipientEmail,
        string recipientUserId = "",
        EmailDeliveryStatus status = EmailDeliveryStatus.Sent,
        DateTimeOffset? createdUtc = null) =>
        store.UpsertEmailLogAsync(new EmailLogEntry(
            id,
            companyId,
            "Test",
            recipientUserId,
            recipientEmail,
            recipientEmail,
            $"Subject {id}",
            $"Body {id}",
            status,
            null,
            null,
            createdUtc ?? DateTimeOffset.UtcNow,
            status == EmailDeliveryStatus.Sent ? DateTimeOffset.UtcNow : null,
            "from@example.com",
            ""));

    private sealed class TestCurrentUser(string userId) : ICurrentUserContext
    {
        public string UserId { get; } = userId;
    }

    private sealed class TestEmailSender(EmailSendResult result) : IEmailSender
    {
        public int SendCount { get; private set; }

        public Task<EmailSendResult> SendAsync(EmailLogEntry email, CancellationToken cancellationToken = default)
        {
            SendCount++;
            return Task.FromResult(result);
        }
    }
}
