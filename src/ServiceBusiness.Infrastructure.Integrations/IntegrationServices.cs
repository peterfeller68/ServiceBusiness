using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
        {item.Visit.NotesToBusinessClient}
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

public sealed class AzureCommunicationEmailSender(IConfiguration configuration) : IEmailSender
{
    private readonly string? connectionString = configuration["Email:AzureCommunicationServices:ConnectionString"];
    private readonly string? senderAddress = configuration["Email:AzureCommunicationServices:SenderAddress"];

    public async Task<ServiceBusiness.Application.EmailSendResult> SendAsync(EmailLogEntry email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(senderAddress))
        {
            return ServiceBusiness.Application.EmailSendResult.Failed("Azure Communication Services email settings are not configured.");
        }

        try
        {
            var emailClient = new EmailClient(connectionString);
            var message = new EmailMessage(
                senderAddress,
                email.RecipientEmail,
                new EmailContent(email.Subject)
                {
                    PlainText = email.Body,
                    Html = email.Body
                });

            var operation = await emailClient.SendAsync(WaitUntil.Completed, message, cancellationToken);
            return ServiceBusiness.Application.EmailSendResult.Sent(operation.Id);
        }
        catch (Exception ex) when (ex is RequestFailedException or InvalidOperationException)
        {
            return ServiceBusiness.Application.EmailSendResult.Failed(ex.Message);
        }
    }
}

public sealed class StripePaymentProviderGateway(HttpClient httpClient, IConfiguration configuration) : IPaymentProviderGateway
{
    private const string ProviderName = "Stripe";
    private readonly string? secretKey = configuration["Payment:Stripe:SecretKey"] ?? configuration["Stripe:SecretKey"];
    private readonly string? webhookSecret = configuration["Payment:Stripe:WebhookSecret"] ?? configuration["Stripe:WebhookSecret"];
    private readonly string providerMode = configuration["Payment:Stripe:Mode"] ?? configuration["Stripe:Mode"] ?? "test";
    private readonly string? monthlyPriceId = configuration["Payment:Stripe:HomeOwnerMonthlyPriceId"] ?? configuration["Stripe:HomeOwnerMonthlyPriceId"];
    private readonly string? annualPriceId = configuration["Payment:Stripe:HomeOwnerAnnualPriceId"] ?? configuration["Stripe:HomeOwnerAnnualPriceId"];

    public async Task<PaymentCheckoutSession> CreateSubscriptionCheckoutSessionAsync(
        AppUser user,
        HomeOwnerSubscription subscription,
        SubscriptionPlan plan,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default)
    {
        var priceId = ResolvePriceId(plan);
        var values = new List<KeyValuePair<string, string>>
        {
            new("mode", "subscription"),
            new("success_url", successUrl),
            new("cancel_url", cancelUrl),
            new("client_reference_id", subscription.OwnerUserId),
            new("line_items[0][price]", priceId),
            new("line_items[0][quantity]", "1"),
            new("metadata[owner_user_id]", subscription.OwnerUserId),
            new("metadata[subscription_id]", subscription.Id),
            new("subscription_data[metadata][owner_user_id]", subscription.OwnerUserId),
            new("subscription_data[metadata][subscription_id]", subscription.Id)
        };

        if (string.IsNullOrWhiteSpace(subscription.ProviderCustomerId))
        {
            values.Add(new("customer_email", user.Email));
        }
        else
        {
            values.Add(new("customer", subscription.ProviderCustomerId));
        }

        if (subscription.TrialEndsAt is { } trialEndsAt && trialEndsAt > DateTimeOffset.UtcNow)
        {
            values.Add(new("subscription_data[trial_end]", trialEndsAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));
        }

        using var response = await PostStripeFormAsync("checkout/sessions", values, cancellationToken);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var root = document.RootElement;
        var url = root.GetProperty("url").GetString()
            ?? throw new InvalidOperationException("Stripe checkout session did not include a URL.");

        return new PaymentCheckoutSession(
            ProviderName,
            providerMode,
            root.GetProperty("id").GetString() ?? "",
            url,
            GetNullableString(root, "customer"),
            GetNullableString(root, "subscription"));
    }

    public async Task<PaymentPortalSession> CreateCustomerPortalSessionAsync(
        string providerCustomerId,
        string returnUrl,
        CancellationToken cancellationToken = default)
    {
        using var response = await PostStripeFormAsync(
            "billing_portal/sessions",
            [
                new("customer", providerCustomerId),
                new("return_url", returnUrl)
            ],
            cancellationToken);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var root = document.RootElement;
        var url = root.GetProperty("url").GetString()
            ?? throw new InvalidOperationException("Stripe portal session did not include a URL.");

        return new PaymentPortalSession(
            ProviderName,
            providerMode,
            root.GetProperty("id").GetString() ?? "",
            url);
    }

    public PaymentProviderWebhookEvent ParseWebhookEvent(string payload, string signatureHeader)
    {
        VerifySignature(payload, signatureHeader);
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var eventId = root.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Stripe webhook event id is missing.");
        var eventType = root.GetProperty("type").GetString()
            ?? throw new InvalidOperationException("Stripe webhook event type is missing.");
        var dataObject = root.GetProperty("data").GetProperty("object");

        return eventType switch
        {
            "checkout.session.completed" => ParseCheckoutSessionCompleted(eventId, eventType, dataObject),
            "customer.subscription.created" or "customer.subscription.updated" or "customer.subscription.deleted" =>
                ParseSubscriptionEvent(eventId, eventType, dataObject),
            "invoice.payment_failed" => ParseInvoiceEvent(eventId, eventType, dataObject, SubscriptionStatus.PastDue),
            "invoice.paid" => ParseInvoiceEvent(eventId, eventType, dataObject, SubscriptionStatus.Active),
            _ => new PaymentProviderWebhookEvent(
                ProviderName,
                providerMode,
                eventId,
                eventType,
                null,
                GetNullableString(dataObject, "customer"),
                GetNullableString(dataObject, "subscription"),
                null,
                null,
                null,
                null,
                null,
                $"Stripe event {eventType}")
        };
    }

    private PaymentProviderWebhookEvent ParseCheckoutSessionCompleted(string eventId, string eventType, JsonElement dataObject)
    {
        var ownerUserId = GetNullableString(dataObject, "client_reference_id") ?? GetMetadataValue(dataObject, "owner_user_id");
        return new PaymentProviderWebhookEvent(
            ProviderName,
            providerMode,
            eventId,
            eventType,
            ownerUserId,
            GetNullableString(dataObject, "customer"),
            GetNullableString(dataObject, "subscription"),
            GetNullableString(dataObject, "id"),
            SubscriptionStatus.Active,
            null,
            null,
            null,
            "Stripe checkout session completed");
    }

    private PaymentProviderWebhookEvent ParseSubscriptionEvent(string eventId, string eventType, JsonElement dataObject)
    {
        return new PaymentProviderWebhookEvent(
            ProviderName,
            providerMode,
            eventId,
            eventType,
            GetMetadataValue(dataObject, "owner_user_id"),
            GetNullableString(dataObject, "customer"),
            GetNullableString(dataObject, "id"),
            null,
            MapStripeSubscriptionStatus(GetNullableString(dataObject, "status"), eventType),
            GetUnixDateTime(dataObject, "current_period_start"),
            GetUnixDateTime(dataObject, "current_period_end"),
            GetNullableBool(dataObject, "cancel_at_period_end"),
            $"Stripe subscription event {eventType}");
    }

    private PaymentProviderWebhookEvent ParseInvoiceEvent(string eventId, string eventType, JsonElement dataObject, SubscriptionStatus status)
    {
        return new PaymentProviderWebhookEvent(
            ProviderName,
            providerMode,
            eventId,
            eventType,
            null,
            GetNullableString(dataObject, "customer"),
            GetNullableString(dataObject, "subscription"),
            null,
            status,
            null,
            null,
            null,
            $"Stripe invoice event {eventType}");
    }

    private string ResolvePriceId(SubscriptionPlan plan)
    {
        var configured = plan.BillingInterval == SubscriptionBillingInterval.Annual
            ? annualPriceId
            : monthlyPriceId;
        var priceId = string.IsNullOrWhiteSpace(plan.ProviderPriceId) ? configured : plan.ProviderPriceId;
        if (string.IsNullOrWhiteSpace(priceId))
        {
            throw new InvalidOperationException($"Stripe price id is not configured for {plan.Name}.");
        }

        return priceId;
    }

    private async Task<HttpResponseMessage> PostStripeFormAsync(
        string relativePath,
        IReadOnlyList<KeyValuePair<string, string>> values,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("Stripe secret key is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.stripe.com/v1/{relativePath}")
        {
            Content = new FormUrlEncodedContent(values)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{secretKey}:")));

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Stripe request failed: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
        }

        return response;
    }

    private void VerifySignature(string payload, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            throw new InvalidOperationException("Stripe webhook secret is not configured.");
        }

        var values = signatureHeader
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .GroupBy(parts => parts[0], parts => parts[1])
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        if (!values.TryGetValue("t", out var timestamps) ||
            !long.TryParse(timestamps.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var timestamp) ||
            !values.TryGetValue("v1", out var signatures))
        {
            throw new InvalidOperationException("Stripe signature header is invalid.");
        }

        var eventTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        if (DateTimeOffset.UtcNow - eventTime > TimeSpan.FromMinutes(5))
        {
            throw new InvalidOperationException("Stripe signature timestamp is outside the allowed tolerance.");
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
        var signedPayload = $"{timestamp}.{payload}";
        var expectedBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var expected = Convert.ToHexString(expectedBytes).ToLowerInvariant();
        if (!signatures.Any(signature => CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signature),
                Encoding.UTF8.GetBytes(expected))))
        {
            throw new InvalidOperationException("Stripe signature verification failed.");
        }
    }

    private static SubscriptionStatus? MapStripeSubscriptionStatus(string? status, string eventType) =>
        eventType == "customer.subscription.deleted"
            ? SubscriptionStatus.Canceled
            : status switch
            {
                "trialing" => SubscriptionStatus.Trialing,
                "active" => SubscriptionStatus.Active,
                "past_due" => SubscriptionStatus.PastDue,
                "canceled" => SubscriptionStatus.Canceled,
                "unpaid" => SubscriptionStatus.PaymentFailed,
                "incomplete" or "incomplete_expired" => SubscriptionStatus.PendingCheckout,
                _ => null
            };

    private static string? GetNullableString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static bool? GetNullableBool(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static DateTimeOffset? GetUnixDateTime(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return property.TryGetInt64(out var seconds) ? DateTimeOffset.FromUnixTimeSeconds(seconds) : null;
    }

    private static string? GetMetadataValue(JsonElement element, string key)
    {
        if (!element.TryGetProperty("metadata", out var metadata) || metadata.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return GetNullableString(metadata, key);
    }
}
