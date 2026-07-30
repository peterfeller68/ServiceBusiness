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
    Scheduled,
    Assigned,
    InProgress,
    Completed,
    Canceled,
    Skipped
}

public enum EmailDeliveryStatus
{
    Queued,
    Sent,
    Failed,
    TestRerouted,
    Suppressed
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

public sealed record SystemSettings(SystemMode SystemMode);

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
    CompanyStatus Status);

public sealed record CompanyMembership(
    string CompanyId,
    string UserId,
    CompanyRole Role,
    MembershipStatus Status,
    DateTimeOffset RequestedUtc,
    DateTimeOffset? DecidedUtc,
    string? DecidedByUserId);

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
    bool IsActive);

public sealed record IndependentHomeOwnerProfile(
    string UserId,
    string HomeAddress,
    string AccessNotes,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);

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

public sealed record Material(
    string Id,
    string CompanyId,
    string? CategoryId,
    string Name,
    string UnitOfMeasure,
    decimal DefaultUnitCost,
    decimal DefaultBillableUnitPrice,
    bool IsTaxable,
    bool IsActive);

public sealed record PoolEquipmentItem(
    string Id,
    EquipmentScope Scope,
    string ScopeOwnerId,
    string CategoryId,
    string Name,
    string Description,
    string? ImageUrl,
    bool IsActive);

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
    DateTimeOffset? CompletedUtc);

public sealed record VisitCompletion(
    string VisitId,
    string CompanyId,
    string CompletedByUserId,
    IReadOnlyList<string> ServiceIds,
    IReadOnlyList<MaterialUsage> Materials,
    string CustomerNotes,
    string InternalNotes,
    DateTimeOffset CompletedUtc);

public sealed record MaterialUsage(
    string MaterialId,
    decimal Quantity);

public sealed record ServiceHistoryItem(
    ServiceVisit Visit,
    CompanyClient Client,
    AppUser? AssignedUser,
    VisitCompletion? Completion);

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
    string? HomeAccessNotes = null);

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
    DateTimeOffset? SentUtc);

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
