using System.Diagnostics;
using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using ServiceBusiness.Application;
using ServiceBusiness.Domain;

namespace ServiceBusiness.Infrastructure.Integrations;

public sealed class AzureCommunicationEmailNotificationQueue(
    IServiceBusinessStore store,
    IConfiguration configuration) : INotificationQueue
{
    private readonly string? connectionString = configuration["Email:AzureCommunicationServices:ConnectionString"];
    private readonly string? senderAddress = configuration["Email:AzureCommunicationServices:SenderAddress"];
    private readonly string? testRecipientEmail = configuration["Email:TestRecipientEmail"];

    public async Task QueueVisitCompletedEmailAsync(ServiceHistoryItem item, CancellationToken cancellationToken = default)
    {
        var subject = $"Service completed for {item.Client.DisplayName}";
        var body = $"""
        Your service visit was completed on {item.Visit.CompletedUtc?.LocalDateTime:g}.

        Notes:
        {item.Completion?.CustomerNotes}
        """;

        var recipient = item.AssignedUser ?? new AppUser(
            $"client-{item.Client.Id}",
            null,
            item.Client.Email,
            item.Client.Email,
            item.Client.PrimaryContactName,
            item.Client.Phone,
            null,
            false,
            false,
            true,
            UserStatus.Active);

        await SendAndLogAsync(
            item.Visit.CompanyId,
            "VisitCompleted",
            recipient,
            item.Client.Email,
            subject,
            body,
            cancellationToken);
    }

    public Task QueueAccountApprovalDecisionEmailAsync(
        AccessRequest request,
        MembershipStatus decision,
        CancellationToken cancellationToken = default)
    {
        var subject = $"Your {request.Company.Name} access request was {decision.ToString().ToLowerInvariant()}";
        var body = decision == MembershipStatus.Active
            ? $"Your {request.Role.DisplayName} access for {request.Company.Name} has been approved. You can now sign in and open your dashboard."
            : $"Your {request.Role.DisplayName} access request for {request.Company.Name} was rejected. Contact the business owner if you believe this was a mistake.";

        return SendAndLogAsync(
            request.Company.Id,
            "AccountApprovalDecision",
            request.User,
            request.User.NotificationEmail ?? request.User.Email,
            subject,
            body,
            cancellationToken);
    }

    private async Task SendAndLogAsync(
        string? companyId,
        string emailType,
        AppUser recipientUser,
        string originalRecipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        using var activity = ServiceBusinessTelemetry.ActivitySource.StartActivity("SendEmailNotification");
        activity?.SetTag("company.id", companyId);
        activity?.SetTag("email.type", emailType);
        activity?.SetTag("recipient.user.id", recipientUser.Id);
        activity?.SetTag("recipient.is_test_user", recipientUser.IsTestUser);

        var createdUtc = DateTimeOffset.UtcNow;
        var recipientEmail = ResolveRecipientEmail(recipientUser, originalRecipientEmail);
        var status = recipientUser.IsTestUser && !string.IsNullOrWhiteSpace(testRecipientEmail)
            ? EmailDeliveryStatus.TestRerouted
            : EmailDeliveryStatus.Queued;
        activity?.SetTag("email.status.initial", status.ToString());

        var log = new EmailLogEntry(
            Guid.NewGuid().ToString("N"),
            companyId,
            emailType,
            recipientUser.Id,
            originalRecipientEmail,
            recipientEmail,
            subject,
            body,
            status,
            null,
            null,
            createdUtc,
            null);

        if (recipientUser.EmailNotificationsEnabled == false)
        {
            var suppressedLog = log with
            {
                Status = EmailDeliveryStatus.Suppressed,
                FailureReason = "User disabled email notifications."
            };
            await store.UpsertEmailLogAsync(suppressedLog, cancellationToken);
            activity?.SetTag("email.status.initial", suppressedLog.Status.ToString());
            ServiceBusinessTelemetry.EmailNotifications.Add(1, new KeyValuePair<string, object?>("email.status", suppressedLog.Status.ToString()));
            return;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(senderAddress))
            {
                await store.UpsertEmailLogAsync(log, cancellationToken);
                ServiceBusinessTelemetry.EmailNotifications.Add(1, new KeyValuePair<string, object?>("email.status", log.Status.ToString()));
                return;
            }

            var emailClient = new EmailClient(connectionString);
            var message = new EmailMessage(
                senderAddress,
                recipientEmail,
                new EmailContent(subject)
                {
                    PlainText = body
                });

            var operation = await emailClient.SendAsync(WaitUntil.Completed, message, cancellationToken);

            await store.UpsertEmailLogAsync(log with
            {
                Status = EmailDeliveryStatus.Sent,
                ProviderMessageId = operation.Id,
                SentUtc = DateTimeOffset.UtcNow
            }, cancellationToken);
            ServiceBusinessTelemetry.EmailNotifications.Add(1, new KeyValuePair<string, object?>("email.status", EmailDeliveryStatus.Sent.ToString()));
        }
        catch (Exception ex) when (ex is RequestFailedException or InvalidOperationException)
        {
            await store.UpsertEmailLogAsync(log with
            {
                Status = EmailDeliveryStatus.Failed,
                FailureReason = ex.Message
            }, cancellationToken);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            ServiceBusinessTelemetry.EmailNotifications.Add(1, new KeyValuePair<string, object?>("email.status", EmailDeliveryStatus.Failed.ToString()));
        }
    }

    private string ResolveRecipientEmail(AppUser recipientUser, string originalRecipientEmail)
    {
        if (recipientUser.IsTestUser && !string.IsNullOrWhiteSpace(testRecipientEmail))
        {
            return testRecipientEmail;
        }

        return string.IsNullOrWhiteSpace(recipientUser.NotificationEmail)
            ? originalRecipientEmail
            : recipientUser.NotificationEmail;
    }
}
