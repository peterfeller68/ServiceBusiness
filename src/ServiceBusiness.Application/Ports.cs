using ServiceBusiness.Domain;

namespace ServiceBusiness.Application;

public interface IServiceBusinessStore
{
    Task<AppUser?> GetUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<AppUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<AppUser?> GetUserByGoogleSubjectAsync(string googleSubjectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppUser>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<SystemSettings> GetSystemSettingsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubscriptionPlan>> GetSubscriptionPlansAsync(CancellationToken cancellationToken = default);
    Task<SubscriptionPlan?> GetSubscriptionPlanAsync(string planId, CancellationToken cancellationToken = default);
    Task<HomeOwnerSubscription?> GetHomeOwnerSubscriptionAsync(string ownerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HomeOwnerSubscription>> GetHomeOwnerSubscriptionsAsync(CancellationToken cancellationToken = default);
    Task<PaymentProviderEvent?> GetPaymentProviderEventAsync(string provider, string providerEventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentProviderEvent>> GetPaymentProviderEventsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentOperationLog>> GetPaymentOperationLogsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleDefinition>> GetRoleDefinitionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CompanyType>> GetCompanyTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Company>> GetCompaniesAsync(CancellationToken cancellationToken = default);
    Task<Company?> GetCompanyAsync(string companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CompanyMembership>> GetMembershipsForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CompanyMembership>> GetMembershipsForCompanyAsync(string companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClientType>> GetClientTypesAsync(string companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CompanyClient>> GetClientsAsync(string companyId, CancellationToken cancellationToken = default);
    Task<CompanyClient?> GetClientAsync(string companyId, string clientId, CancellationToken cancellationToken = default);
    Task<IndependentHomeOwnerProfile?> GetIndependentHomeOwnerProfileAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IndependentHomeOwnerProfile>> GetIndependentHomeOwnerProfilesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HomeOwnerPoolEquipmentPhoto>> GetHomeOwnerPoolEquipmentPhotosAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IndependentHomeOwnerServiceHistoryItem>> GetIndependentHomeOwnerServiceHistoryAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceCategory>> GetServiceCategoriesAsync(string companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaterialCategory>> GetMaterialCategoriesAsync(string companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PoolEquipmentCategory>> GetPoolEquipmentCategoriesAsync(EquipmentScope scope, string scopeOwnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceOffering>> GetServicesAsync(string companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServicePackage>> GetServicePackagesAsync(string companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Material>> GetMaterialsAsync(string companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PoolEquipmentItem>> GetPoolEquipmentItemsAsync(EquipmentScope scope, string scopeOwnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceVisit>> GetVisitsByDateAsync(string companyId, DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceVisit>> GetVisitsAsync(string companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceVisit>> GetVisitsForUserByDateAsync(string companyId, string userId, DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceVisit>> GetVisitsForClientAsync(string companyId, string clientId, CancellationToken cancellationToken = default);
    Task<ServiceVisit?> GetVisitAsync(string companyId, string visitId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invoice>> GetInvoicesAsync(string companyId, CancellationToken cancellationToken = default);
    Task<Invoice?> GetInvoiceAsync(string companyId, string invoiceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailLogEntry>> GetEmailLogsAsync(CancellationToken cancellationToken = default);
    Task UpsertUserAsync(AppUser user, CancellationToken cancellationToken = default);
    Task UpsertSubscriptionPlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default);
    Task UpsertHomeOwnerSubscriptionAsync(HomeOwnerSubscription subscription, CancellationToken cancellationToken = default);
    Task UpsertPaymentProviderEventAsync(PaymentProviderEvent paymentEvent, CancellationToken cancellationToken = default);
    Task UpsertPaymentOperationLogAsync(PaymentOperationLog paymentOperationLog, CancellationToken cancellationToken = default);
    Task UpsertRoleDefinitionAsync(RoleDefinition roleDefinition, CancellationToken cancellationToken = default);
    Task UpsertCompanyAsync(Company company, CancellationToken cancellationToken = default);
    Task UpsertMembershipAsync(CompanyMembership membership, CancellationToken cancellationToken = default);
    Task UpsertClientTypeAsync(ClientType clientType, CancellationToken cancellationToken = default);
    Task UpsertClientAsync(CompanyClient client, CancellationToken cancellationToken = default);
    Task UpsertIndependentHomeOwnerProfileAsync(IndependentHomeOwnerProfile profile, CancellationToken cancellationToken = default);
    Task UpsertHomeOwnerPoolEquipmentPhotoAsync(string userId, HomeOwnerPoolEquipmentPhoto photo, CancellationToken cancellationToken = default);
    Task DeleteHomeOwnerPoolEquipmentPhotoAsync(string userId, string photoId, CancellationToken cancellationToken = default);
    Task<UserDeletionResult> DeleteUserAsync(string userId, CancellationToken cancellationToken = default);
    Task UpsertIndependentHomeOwnerServiceHistoryItemAsync(IndependentHomeOwnerServiceHistoryItem item, CancellationToken cancellationToken = default);
    Task UpsertServiceCategoryAsync(ServiceCategory category, CancellationToken cancellationToken = default);
    Task UpsertMaterialCategoryAsync(MaterialCategory category, CancellationToken cancellationToken = default);
    Task UpsertPoolEquipmentCategoryAsync(PoolEquipmentCategory category, CancellationToken cancellationToken = default);
    Task UpsertServiceAsync(ServiceOffering service, CancellationToken cancellationToken = default);
    Task UpsertServicePackageAsync(ServicePackage servicePackage, CancellationToken cancellationToken = default);
    Task UpsertMaterialAsync(Material material, CancellationToken cancellationToken = default);
    Task UpsertPoolEquipmentItemAsync(PoolEquipmentItem item, CancellationToken cancellationToken = default);
    Task DeleteServiceAsync(string companyId, string serviceId, CancellationToken cancellationToken = default);
    Task DeleteServicePackageAsync(string companyId, string packageId, CancellationToken cancellationToken = default);
    Task DeleteVisitAsync(string companyId, string visitId, CancellationToken cancellationToken = default);
    Task DeleteInvoiceAsync(string companyId, string invoiceId, CancellationToken cancellationToken = default);
    Task DeletePoolEquipmentItemAsync(EquipmentScope scope, string scopeOwnerId, string itemId, CancellationToken cancellationToken = default);
    Task UpsertVisitAsync(ServiceVisit visit, CancellationToken cancellationToken = default);
    Task UpsertInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task UpsertEmailLogAsync(EmailLogEntry emailLog, CancellationToken cancellationToken = default);
    Task UpsertSystemSettingsAsync(SystemSettings settings, CancellationToken cancellationToken = default);
}

public interface ICurrentUserContext
{
    string UserId { get; }
}

public interface INotificationQueue
{
    Task QueueVisitCompletedEmailAsync(ServiceHistoryItem item, CancellationToken cancellationToken = default);
    Task QueueAccountApprovalDecisionEmailAsync(AccessRequest request, MembershipStatus decision, CancellationToken cancellationToken = default);
}

public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(EmailLogEntry email, CancellationToken cancellationToken = default);
}

public interface IPaymentProviderGateway
{
    Task<PaymentCheckoutSession> CreateSubscriptionCheckoutSessionAsync(
        AppUser user,
        HomeOwnerSubscription subscription,
        SubscriptionPlan plan,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default);

    Task<PaymentPortalSession> CreateCustomerPortalSessionAsync(
        string providerCustomerId,
        string returnUrl,
        CancellationToken cancellationToken = default);

    PaymentProviderWebhookEvent ParseWebhookEvent(string payload, string signatureHeader);
}

public sealed record EmailSendResult(
    bool Succeeded,
    string? ProviderMessageId = null,
    string? FailureMessage = null)
{
    public static EmailSendResult Sent(string? providerMessageId = null) => new(true, providerMessageId);

    public static EmailSendResult Failed(string failureMessage) => new(false, null, failureMessage);
}

public sealed record PaymentCheckoutSession(
    string Provider,
    string ProviderMode,
    string CheckoutSessionId,
    string Url,
    string? CustomerId,
    string? SubscriptionId);

public sealed record PaymentPortalSession(
    string Provider,
    string ProviderMode,
    string PortalSessionId,
    string Url);

public sealed record PaymentProviderWebhookEvent(
    string Provider,
    string ProviderMode,
    string ProviderEventId,
    string EventType,
    string? OwnerUserId,
    string? ProviderCustomerId,
    string? ProviderSubscriptionId,
    string? ProviderCheckoutSessionId,
    SubscriptionStatus? SubscriptionStatus,
    DateTimeOffset? CurrentPeriodStartsAt,
    DateTimeOffset? CurrentPeriodEndsAt,
    bool? CancelAtPeriodEnd,
    string Summary);
