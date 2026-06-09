using ServiceBusiness.Domain;

namespace ServiceBusiness.Application;

public interface IServiceBusinessStore
{
    Task<AppUser?> GetUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<AppUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<AppUser?> GetUserByGoogleSubjectAsync(string googleSubjectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppUser>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<SystemSettings> GetSystemSettingsAsync(CancellationToken cancellationToken = default);
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
    Task<IReadOnlyList<ServiceCategory>> GetServiceCategoriesAsync(string companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaterialCategory>> GetMaterialCategoriesAsync(string companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PoolEquipmentCategory>> GetPoolEquipmentCategoriesAsync(EquipmentScope scope, string scopeOwnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceOffering>> GetServicesAsync(string companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Material>> GetMaterialsAsync(string companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PoolEquipmentItem>> GetPoolEquipmentItemsAsync(EquipmentScope scope, string scopeOwnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceVisit>> GetVisitsByDateAsync(string companyId, DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceVisit>> GetVisitsForUserByDateAsync(string companyId, string userId, DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceVisit>> GetVisitsForClientAsync(string companyId, string clientId, CancellationToken cancellationToken = default);
    Task<ServiceVisit?> GetVisitAsync(string companyId, string visitId, CancellationToken cancellationToken = default);
    Task<VisitCompletion?> GetVisitCompletionAsync(string companyId, string visitId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailLogEntry>> GetEmailLogsAsync(CancellationToken cancellationToken = default);
    Task UpsertUserAsync(AppUser user, CancellationToken cancellationToken = default);
    Task UpsertRoleDefinitionAsync(RoleDefinition roleDefinition, CancellationToken cancellationToken = default);
    Task UpsertCompanyAsync(Company company, CancellationToken cancellationToken = default);
    Task UpsertMembershipAsync(CompanyMembership membership, CancellationToken cancellationToken = default);
    Task UpsertClientAsync(CompanyClient client, CancellationToken cancellationToken = default);
    Task UpsertIndependentHomeOwnerProfileAsync(IndependentHomeOwnerProfile profile, CancellationToken cancellationToken = default);
    Task UpsertServiceCategoryAsync(ServiceCategory category, CancellationToken cancellationToken = default);
    Task UpsertMaterialCategoryAsync(MaterialCategory category, CancellationToken cancellationToken = default);
    Task UpsertPoolEquipmentCategoryAsync(PoolEquipmentCategory category, CancellationToken cancellationToken = default);
    Task UpsertServiceAsync(ServiceOffering service, CancellationToken cancellationToken = default);
    Task UpsertMaterialAsync(Material material, CancellationToken cancellationToken = default);
    Task UpsertPoolEquipmentItemAsync(PoolEquipmentItem item, CancellationToken cancellationToken = default);
    Task UpsertVisitAsync(ServiceVisit visit, CancellationToken cancellationToken = default);
    Task UpsertVisitCompletionAsync(VisitCompletion completion, CancellationToken cancellationToken = default);
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
