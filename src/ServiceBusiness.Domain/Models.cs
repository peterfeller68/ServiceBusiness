namespace ServiceBusiness.Domain;

public enum CompanyRole
{
    CompanyAdmin,
    CompanyUser,
    CompanyClientUser
}

public enum RegistrationAccountType
{
    BusinessOwner,
    BusinessUser,
    BusinessClient,
    IndependentHomeOwner
}

public enum MembershipStatus
{
    Pending,
    Active,
    Rejected,
    Inactive,
    Removed
}

public enum CompanyStatus
{
    Active,
    Suspended,
    Inactive
}

public enum BillingFrequency
{
    FeeForService,
    Weekly,
    BiWeekly,
    Monthly
}

public enum VisitStatus
{
    New,
    Unscheduled,
    Scheduled,
    Assigned,
    InProgress,
    Completed,
    Closed,
    Canceled,
    Skipped
}

public enum VisitType
{
    ServicePackageVisit,
    AdHocVisit
}

public enum EmailDeliveryStatus
{
    New,
    Queued,
    Sent,
    Failed,
    TestRerouted,
    Suppressed
}

public enum InvoiceStatus
{
    New,
    Invoiced,
    Paid
}

public enum UserStatus
{
    Active,
    Disabled
}

public enum EquipmentScope
{
    Global,
    Company,
    HomeOwner
}

public enum SystemMode
{
    Pool,
    Landscape
}

public sealed record SystemSettings(SystemMode SystemMode, bool DevTest = false);

public static class GlobalCatalogScope
{
    public const string Pool = "Pool_Global";
    public const string Landscape = "LandScape_Global";
    public const string Legacy = "global";

    public static string For(SystemMode mode) =>
        mode == SystemMode.Landscape ? Landscape : Pool;

    public static bool IsGlobal(string companyId) =>
        string.Equals(companyId, Pool, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(companyId, Landscape, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(companyId, Legacy, StringComparison.OrdinalIgnoreCase);

    public static string? ServiceNameFor(string companyId)
    {
        if (string.Equals(companyId, Pool, StringComparison.OrdinalIgnoreCase))
        {
            return "Pool";
        }

        if (string.Equals(companyId, Landscape, StringComparison.OrdinalIgnoreCase))
        {
            return "LandScape";
        }

        return null;
    }
}

public sealed record AppUser(
    string Id,
    string? GoogleSubjectId,
    string Email,
    string? NotificationEmail,
    string DisplayName,
    string? Phone,
    string? ProfileImageUrl,
    bool IsSystemAdmin,
    bool IsTestUser,
    bool? EmailNotificationsEnabled,
    UserStatus Status);

public sealed record RoleDefinition(
    CompanyRole Role,
    string DisplayName,
    string Description,
    bool RequiresOwnerApproval,
    IReadOnlyList<string> Permissions);

public sealed record CompanyType(
    string Id,
    string Name,
    string Description,
    bool IsActive);

public sealed record Company(
    string Id,
    string CompanyTypeId,
    string Name,
    string BusinessEmail,
    string BusinessPhone,
    string TimeZone,
    CompanyStatus Status,
    string? ServicePackageId = null);

public sealed record CompanyMembership(
    string CompanyId,
    string UserId,
    CompanyRole Role,
    MembershipStatus Status,
    DateTimeOffset RequestedUtc,
    DateTimeOffset? DecidedUtc,
    string? DecidedByUserId,
    string? CompanyClientId = null);

public sealed record ClientType(
    string Id,
    string CompanyId,
    string Name,
    BillingFrequency BillingFrequency,
    decimal DefaultRate,
    bool IsActive);

public sealed record ServiceCategory(
    string Id,
    string CompanyId,
    string Name,
    string Description,
    bool IsSystemManaged,
    bool IsActive);

public sealed record MaterialCategory(
    string Id,
    string CompanyId,
    string Name,
    string Description,
    bool IsSystemManaged,
    bool IsActive);

public sealed record PoolEquipmentCategory(
    string Id,
    EquipmentScope Scope,
    string ScopeOwnerId,
    string Manufacturer,
    string Name,
    string Description,
    bool IsSystemManaged,
    bool IsActive);

public sealed record CompanyClient(
    string Id,
    string CompanyId,
    string DisplayName,
    string PrimaryContactName,
    string Email,
    string Phone,
    string ServiceAddress,
    string AccessNotes,
    string ClientTypeId,
    decimal? RateOverride,
    bool IsActive,
    string? ServicePackageId = null);

public sealed record IndependentHomeOwnerProfile(
    string UserId,
    string HomeAddress,
    string AccessNotes,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc,
    IReadOnlyList<HomeOwnerPoolEquipmentPhoto>? PoolEquipmentPhotos = null,
    string GeneralNotes = "");

public sealed record HomeOwnerPoolEquipmentPhoto(
    string Id,
    string FileName,
    string ContentType,
    string DataUrl,
    DateTimeOffset UploadedUtc);

public sealed record ServiceOffering(
    string Id,
    string CompanyId,
    string? CategoryId,
    string Name,
    string Description,
    int DefaultDurationMinutes,
    decimal DefaultPrice,
    bool IsTaxable,
    bool IsActive);

public sealed record ServicePackage(
    string Id,
    string CompanyId,
    string Name,
    string Recurrence,
    string Description,
    decimal Cost,
    bool IsActive,
    IReadOnlyList<ServicePackageService> Services);

public sealed record ServicePackageService(
    string ServiceId,
    string Recurrence);

public sealed record ServiceSeedRow(
    string Category,
    string Name,
    string Description);

public sealed record ServiceSeedResult(
    int CategoriesSeeded,
    int ServicesSeeded);

public sealed record Material(
    string Id,
    string CompanyId,
    string? CategoryId,
    string Name,
    string UnitOfMeasure,
    decimal DefaultUnitCost,
    decimal DefaultBillableUnitPrice,
    bool IsTaxable,
    bool IsActive,
    string Brand = "",
    string ModelNo = "",
    string Description = "");

public sealed record MaterialSeedRow(
    string Brand,
    string Category,
    string Name,
    string ModelNo);

public sealed record MaterialSeedResult(
    int CategoriesSeeded,
    int MaterialsSeeded);

public sealed record PoolEquipmentItem(
    string Id,
    EquipmentScope Scope,
    string ScopeOwnerId,
    string CategoryId,
    string Name,
    string Description,
    string? ImageUrl,
    bool IsActive,
    string ModelNo = "",
    string Manufacturer = "",
    string Comment = "");

public sealed record PoolEquipmentSeedRow(
    string Manufacturer,
    string Category,
    string Name,
    string ModelNo);

public sealed record PoolEquipmentSeedResult(
    int CategoriesSeeded,
    int EquipmentSeeded);

public sealed record ServiceVisit(
    string Id,
    string CompanyId,
    string CompanyClientId,
    string? AssignedUserId,
    DateOnly ScheduledDate,
    TimeOnly ServiceWindowStart,
    TimeOnly ServiceWindowEnd,
    VisitStatus Status,
    IReadOnlyList<string> PlannedServiceIds,
    int RouteOrder,
    string Notes,
    DateTimeOffset? StartedUtc,
    DateTimeOffset? CompletedUtc,
    VisitType VisitType = VisitType.ServicePackageVisit,
    string VisitName = "",
    string NotesToBusinessClient = "",
    string NotesToServiceClient = "",
    string InternalNotes = "",
    string? InvoiceId = null,
    IReadOnlyList<string>? OutOfScopeServiceIds = null,
    IReadOnlyList<MaterialUsage>? OutOfScopeMaterials = null,
    string? CompletedByUserId = null,
    IReadOnlyList<string>? CompletedServiceIds = null,
    IReadOnlyList<MaterialUsage>? MaterialsUsed = null);

public sealed record MaterialUsage(
    string MaterialId,
    decimal Quantity);

public sealed record Invoice(
    string InvoiceGuid,
    string CompanyId,
    string InvoiceId,
    DateOnly InvoiceDate,
    DateOnly? PaidDate,
    string CompanyClientId,
    string VisitId,
    string? ServicePackageId,
    IReadOnlyList<InvoiceServiceLine> AdditionalServices,
    IReadOnlyList<InvoiceMaterialLine> Materials,
    decimal TotalCost,
    InvoiceStatus Status,
    string InvoiceHtml,
    DateTimeOffset CreatedUtc);

public sealed record InvoiceServiceLine(
    string ServiceId,
    string Name,
    decimal Amount);

public sealed record InvoiceMaterialLine(
    string MaterialId,
    string Name,
    string Unit,
    decimal Quantity,
    decimal UnitAmount,
    decimal Amount);

public sealed record ServiceHistoryItem(
    ServiceVisit Visit,
    CompanyClient Client,
    AppUser? AssignedUser);

public sealed record IndependentHomeOwnerServiceHistoryItem(
    string Id,
    string UserId,
    DateTimeOffset ServiceDateTime,
    string Notes,
    DateTimeOffset CreatedUtc,
    string? ServiceId = "",
    string ServiceName = "",
    bool IsDeleted = false);

public sealed record CatalogOverview(
    IReadOnlyList<ServiceCategoryGroup> ServiceGroups,
    IReadOnlyList<MaterialCategoryGroup> MaterialGroups);

public sealed record ServiceCategoryGroup(
    ServiceCategory Category,
    IReadOnlyList<ServiceOffering> Services);

public sealed record MaterialCategoryGroup(
    MaterialCategory Category,
    IReadOnlyList<Material> Materials);

public sealed record PoolEquipmentOverview(
    IReadOnlyList<PoolEquipmentCategoryGroup> CategoryGroups);

public sealed record PoolEquipmentCategoryGroup(
    PoolEquipmentCategory Category,
    IReadOnlyList<PoolEquipmentItem> Items);

public sealed record IndependentHomeOwnerDashboard(
    AppUser User,
    IndependentHomeOwnerProfile Profile,
    PoolEquipmentOverview PoolEquipment,
    IReadOnlyList<IndependentHomeOwnerServiceHistoryItem> ServiceHistory);

public sealed record CompanyDashboard(
    Company Company,
    int CustomerCount,
    int EmployeeCount,
    int EquipmentCount,
    int MaterialCount,
    int ServiceCount,
    int PendingEmployeeRequests,
    int PendingCustomerRequests,
    IReadOnlyList<AccessRequest> PendingAccessRequests);

public sealed record RegistrationSubmission(
    RegistrationAccountType AccountType,
    string Email,
    string DisplayName,
    string Phone,
    string? CompanyId,
    string? BusinessName,
    string? BusinessPhone,
    string? BusinessEmail,
    string? ServiceArea,
    IReadOnlyList<string> InitialServices,
    string? HomeAddress = null,
    string? HomeAccessNotes = null,
    bool AuthenticationSkipped = false,
    string? BusinessClientId = null);

public sealed record GoogleUserProfile(
    string GoogleSubjectId,
    string Email,
    string DisplayName,
    string? ProfileImageUrl);

public sealed record RegistrationResult(
    AppUser User,
    Company? Company,
    CompanyMembership? Membership,
    bool RequiresApproval,
    string Message);

public sealed record UserAccessOverview(
    AppUser User,
    bool AuthenticationSkipped,
    IReadOnlyList<CompanyAccess> Companies);

public sealed record CompanyAccess(
    Company Company,
    CompanyRole Role,
    MembershipStatus Status);

public sealed record AccessRequest(
    CompanyMembership Membership,
    AppUser User,
    Company Company,
    RoleDefinition Role);

public sealed record EmailLogEntry(
    string Id,
    string? CompanyId,
    string EmailType,
    string RecipientUserId,
    string OriginalRecipientEmail,
    string RecipientEmail,
    string Subject,
    string Body,
    EmailDeliveryStatus Status,
    string? ProviderMessageId,
    string? FailureReason,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? SentUtc,
    string FromEmail = "",
    string CcEmail = "");

public sealed record UserManagementRow(
    AppUser User,
    IReadOnlyList<CompanyAccess> CompanyAccess,
    int PendingMembershipCount,
    int ActiveMembershipCount);

public sealed record PlatformUserManagementOverview(
    int TotalUsers,
    int ActiveUsers,
    int DisabledUsers,
    int SystemAdmins,
    int PendingMemberships,
    IReadOnlyList<RoleDefinition> Roles,
    IReadOnlyList<UserManagementRow> Users);

public sealed record UserDeletionResult(
    string UserId,
    int RowsDeleted);

public sealed record BusinessClientManagementRow(
    Company Company,
    CompanyClient Client,
    ClientType? ClientType);

public sealed record PoolConfigurationClientRow(
    string ScopeOwnerId,
    string CompanyName,
    string ClientAddress,
    string ClientType);

public sealed record CompanyUserManagementRow(
    AppUser User,
    CompanyMembership Membership,
    RoleDefinition Role);

public sealed record CompanyUserManagementOverview(
    Company Company,
    int ActiveUsers,
    int PendingRequests,
    IReadOnlyList<RoleDefinition> Roles,
    IReadOnlyList<CompanyUserManagementRow> Users,
    IReadOnlyList<AccessRequest> PendingAccessRequests);
