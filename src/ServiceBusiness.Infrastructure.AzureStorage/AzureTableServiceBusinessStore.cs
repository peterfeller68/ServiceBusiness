using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using ServiceBusiness.Application;
using ServiceBusiness.Domain;

namespace ServiceBusiness.Infrastructure.AzureStorage;

public sealed class AzureTableServiceBusinessStore : IServiceBusinessStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int PhotoChunkSize = 32_000;
    private readonly TableServiceClient tableServiceClient;
    private readonly SystemSettings configuredDefaultSystemSettings;
    private readonly SemaphoreSlim initializationLock = new(1, 1);
    private bool initialized;

    public AzureTableServiceBusinessStore(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("AzureStorage:ConnectionString is required when AzureStorage:UseAzureStorage is true.");
        }

        tableServiceClient = new TableServiceClient(connectionString);
        configuredDefaultSystemSettings = GetConfiguredDefaultSystemSettings(configuration);
    }

    public async Task<AppUser?> GetUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetAsync<AppUser>("Users", "USER", userId, cancellationToken);
    }

    public async Task<AppUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var normalized = NormalizeEmail(email);
        var lookup = await GetAsync<UserLookup>("UserByEmail", "EMAIL", normalized, cancellationToken);
        return lookup is null ? null : await GetUserAsync(lookup.UserId, cancellationToken);
    }

    public async Task<AppUser?> GetUserByGoogleSubjectAsync(string googleSubjectId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var lookup = await GetAsync<UserLookup>("UserByGoogleSubject", "GOOGLE_SUBJECT", googleSubjectId, cancellationToken);
        return lookup is null ? null : await GetUserAsync(lookup.UserId, cancellationToken);
    }

    public async Task<IReadOnlyList<AppUser>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetPartitionAsync<AppUser>("Users", "USER", cancellationToken);
    }

    public async Task<SystemSettings> GetSystemSettingsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return configuredDefaultSystemSettings;
    }

    public async Task<IReadOnlyList<RoleDefinition>> GetRoleDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetPartitionAsync<RoleDefinition>("RoleDefinitions", "ROLE", cancellationToken);
    }

    public async Task<IReadOnlyList<CompanyType>> GetCompanyTypesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetPartitionAsync<CompanyType>("CompanyTypes", "COMPANY_TYPE", cancellationToken);
    }

    public async Task<IReadOnlyList<Company>> GetCompaniesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetPartitionAsync<Company>("Companies", "COMPANY", cancellationToken);
    }

    public async Task<Company?> GetCompanyAsync(string companyId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetAsync<Company>("Companies", "COMPANY", companyId, cancellationToken);
    }

    public async Task<IReadOnlyList<CompanyMembership>> GetMembershipsForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetPartitionAsync<CompanyMembership>("UserCompanyMemberships", UserPartition(userId), cancellationToken);
    }

    public async Task<IReadOnlyList<CompanyMembership>> GetMembershipsForCompanyAsync(string companyId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetPartitionAsync<CompanyMembership>("CompanyMemberships", CompanyPartition(companyId), cancellationToken);
    }

    public async Task<IReadOnlyList<ClientType>> GetClientTypesAsync(string companyId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetPartitionAsync<ClientType>("ClientTypes", CompanyPartition(companyId), cancellationToken);
    }

    public async Task<IReadOnlyList<CompanyClient>> GetClientsAsync(string companyId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetPartitionAsync<CompanyClient>("CompanyClients", CompanyPartition(companyId), cancellationToken);
    }

    public async Task<CompanyClient?> GetClientAsync(string companyId, string clientId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetAsync<CompanyClient>("CompanyClients", CompanyPartition(companyId), clientId, cancellationToken);
    }

    public async Task<IndependentHomeOwnerProfile?> GetIndependentHomeOwnerProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var profile = await GetAsync<IndependentHomeOwnerProfile>("IndependentHomeOwnerProfiles", "HOMEOWNER_PROFILE", userId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        var photos = await GetHomeOwnerPoolEquipmentPhotosAsync(userId, cancellationToken);
        return profile with { PoolEquipmentPhotos = photos };
    }

    public async Task<IReadOnlyList<HomeOwnerPoolEquipmentPhoto>> GetHomeOwnerPoolEquipmentPhotosAsync(string userId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var table = tableServiceClient.GetTableClient("HomeOwnerPoolEquipmentPhotos");
        var partitionKey = AzureTableKey.ToStorageKey(HomeOwnerPhotoPartition(userId));
        var entities = new List<TableEntity>();

        await foreach (var entity in table.QueryAsync<TableEntity>(e => e.PartitionKey == partitionKey, cancellationToken: cancellationToken))
        {
            entities.Add(entity);
        }

        var photos = new List<HomeOwnerPoolEquipmentPhoto>();
        foreach (var metadata in entities.Where(entity => entity.RowKey.EndsWith("_meta", StringComparison.Ordinal)))
        {
            var photoId = metadata.RowKey[..^5];
            var chunkCount = metadata.GetInt32("ChunkCount") ?? 0;
            var chunks = entities
                .Where(entity => entity.RowKey.StartsWith($"{photoId}_chunk_", StringComparison.Ordinal))
                .OrderBy(entity => entity.RowKey, StringComparer.Ordinal)
                .Select(entity => entity.GetString("Data") ?? "")
                .ToList();

            if (chunkCount != chunks.Count)
            {
                continue;
            }

            photos.Add(new HomeOwnerPoolEquipmentPhoto(
                photoId,
                metadata.GetString("FileName") ?? "",
                metadata.GetString("ContentType") ?? "application/octet-stream",
                string.Concat(chunks),
                metadata.GetDateTimeOffset("UploadedUtc") ?? DateTimeOffset.UtcNow));
        }

        return photos.OrderByDescending(photo => photo.UploadedUtc).ToList();
    }

    public async Task<IReadOnlyList<IndependentHomeOwnerServiceHistoryItem>> GetIndependentHomeOwnerServiceHistoryAsync(string userId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var history = await GetPartitionAsync<IndependentHomeOwnerServiceHistoryItem>("IndependentHomeOwnerServiceHistory", UserPartition(userId), cancellationToken);
        return history.OrderByDescending(h => h.ServiceDateTime).ToList();
    }

    public async Task<IReadOnlyList<ServiceCategory>> GetServiceCategoriesAsync(string companyId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetPartitionAsync<ServiceCategory>("ServiceCategories", ServicePartition(companyId), cancellationToken);
    }

    public async Task<IReadOnlyList<MaterialCategory>> GetMaterialCategoriesAsync(string companyId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetPartitionAsync<MaterialCategory>("MaterialCategories", MaterialPartition(companyId), cancellationToken);
    }

    public async Task<IReadOnlyList<PoolEquipmentCategory>> GetPoolEquipmentCategoriesAsync(EquipmentScope scope, string scopeOwnerId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetPartitionAsync<PoolEquipmentCategory>("PoolEquipmentCategories", EquipmentPartition(scope, scopeOwnerId), cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceOffering>> GetServicesAsync(string companyId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetPartitionAsync<ServiceOffering>("Services", ServicePartition(companyId), cancellationToken);
    }

    public async Task<IReadOnlyList<Material>> GetMaterialsAsync(string companyId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetPartitionAsync<Material>("Materials", MaterialPartition(companyId), cancellationToken);
    }

    public async Task<IReadOnlyList<PoolEquipmentItem>> GetPoolEquipmentItemsAsync(EquipmentScope scope, string scopeOwnerId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetPartitionAsync<PoolEquipmentItem>("PoolEquipmentItems", EquipmentPartition(scope, scopeOwnerId), cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceVisit>> GetVisitsByDateAsync(string companyId, DateOnly date, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var visits = await GetPartitionAsync<ServiceVisit>("ServiceVisits", CompanyPartition(companyId), cancellationToken);
        return visits.Where(v => v.ScheduledDate == date).OrderBy(v => v.ServiceWindowStart).ToList();
    }

    public async Task<IReadOnlyList<ServiceVisit>> GetVisitsForUserByDateAsync(string companyId, string userId, DateOnly date, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var visits = await GetPartitionAsync<ServiceVisit>("ServiceVisits", CompanyPartition(companyId), cancellationToken);
        return visits.Where(v => v.AssignedUserId == userId && v.ScheduledDate == date).OrderBy(v => v.RouteOrder).ToList();
    }

    public async Task<IReadOnlyList<ServiceVisit>> GetVisitsForClientAsync(string companyId, string clientId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var visits = await GetPartitionAsync<ServiceVisit>("ServiceVisits", CompanyPartition(companyId), cancellationToken);
        return visits.Where(v => v.CompanyClientId == clientId).ToList();
    }

    public async Task<ServiceVisit?> GetVisitAsync(string companyId, string visitId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetAsync<ServiceVisit>("ServiceVisits", CompanyPartition(companyId), visitId, cancellationToken);
    }

    public async Task<VisitCompletion?> GetVisitCompletionAsync(string companyId, string visitId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetAsync<VisitCompletion>("VisitCompletions", CompanyPartition(companyId), visitId, cancellationToken);
    }

    public async Task<IReadOnlyList<EmailLogEntry>> GetEmailLogsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var logs = await GetAllAsync<EmailLogEntry>("EmailLogs", cancellationToken);
        return logs.OrderByDescending(e => e.CreatedUtc).ToList();
    }

    public async Task UpsertUserAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await UpsertAsync("Users", "USER", user.Id, user, cancellationToken);
        await UpsertAsync("UserByEmail", "EMAIL", NormalizeEmail(user.Email), new UserLookup(user.Id, user.Email), cancellationToken);
        if (!string.IsNullOrWhiteSpace(user.GoogleSubjectId))
        {
            await UpsertAsync("UserByGoogleSubject", "GOOGLE_SUBJECT", user.GoogleSubjectId, new UserLookup(user.Id, user.Email), cancellationToken);
        }
    }

    public async Task UpsertRoleDefinitionAsync(RoleDefinition roleDefinition, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await UpsertAsync("RoleDefinitions", "ROLE", roleDefinition.Role.ToString(), roleDefinition, cancellationToken);
    }

    public async Task UpsertCompanyAsync(Company company, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await UpsertAsync("Companies", "COMPANY", company.Id, company, cancellationToken);
    }

    public async Task UpsertMembershipAsync(CompanyMembership membership, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await UpsertAsync("CompanyMemberships", CompanyPartition(membership.CompanyId), MembershipRow(membership.UserId, membership.Role), membership, cancellationToken);
        await UpsertAsync("UserCompanyMemberships", UserPartition(membership.UserId), UserMembershipRow(membership.CompanyId, membership.Role), membership, cancellationToken);
    }

    public async Task UpsertClientAsync(CompanyClient client, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await UpsertAsync("CompanyClients", CompanyPartition(client.CompanyId), client.Id, client, cancellationToken);
    }

    public async Task UpsertIndependentHomeOwnerProfileAsync(IndependentHomeOwnerProfile profile, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await UpsertAsync("IndependentHomeOwnerProfiles", "HOMEOWNER_PROFILE", profile.UserId, profile with { PoolEquipmentPhotos = null }, cancellationToken);
        foreach (var photo in profile.PoolEquipmentPhotos ?? [])
        {
            await UpsertHomeOwnerPoolEquipmentPhotoAsync(profile.UserId, photo, cancellationToken);
        }
    }

    public async Task UpsertHomeOwnerPoolEquipmentPhotoAsync(string userId, HomeOwnerPoolEquipmentPhoto photo, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var table = tableServiceClient.GetTableClient("HomeOwnerPoolEquipmentPhotos");
        var partitionKey = AzureTableKey.ToStorageKey(HomeOwnerPhotoPartition(userId));

        await DeleteHomeOwnerPoolEquipmentPhotoRowsAsync(table, partitionKey, photo.Id, cancellationToken);

        var chunks = Chunk(photo.DataUrl, PhotoChunkSize).ToList();
        var metadata = new TableEntity(partitionKey, $"{photo.Id}_meta")
        {
            ["FileName"] = photo.FileName,
            ["ContentType"] = photo.ContentType,
            ["UploadedUtc"] = photo.UploadedUtc,
            ["ChunkCount"] = chunks.Count
        };
        await table.UpsertEntityAsync(metadata, TableUpdateMode.Replace, cancellationToken);

        for (var index = 0; index < chunks.Count; index++)
        {
            var chunk = new TableEntity(partitionKey, $"{photo.Id}_chunk_{index:D4}")
            {
                ["Data"] = chunks[index]
            };
            await table.UpsertEntityAsync(chunk, TableUpdateMode.Replace, cancellationToken);
        }
    }

    public async Task DeleteHomeOwnerPoolEquipmentPhotoAsync(string userId, string photoId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var table = tableServiceClient.GetTableClient("HomeOwnerPoolEquipmentPhotos");
        await DeleteHomeOwnerPoolEquipmentPhotoRowsAsync(table, AzureTableKey.ToStorageKey(HomeOwnerPhotoPartition(userId)), photoId, cancellationToken);
    }

    public async Task UpsertIndependentHomeOwnerServiceHistoryItemAsync(IndependentHomeOwnerServiceHistoryItem item, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await UpsertAsync("IndependentHomeOwnerServiceHistory", UserPartition(item.UserId), item.Id, item, cancellationToken);
    }

    public async Task UpsertServiceCategoryAsync(ServiceCategory category, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await UpsertAsync("ServiceCategories", ServicePartition(category.CompanyId), category.Id, category, cancellationToken);
    }

    public async Task UpsertMaterialCategoryAsync(MaterialCategory category, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await UpsertAsync("MaterialCategories", MaterialPartition(category.CompanyId), category.Id, category, cancellationToken);
    }

    public async Task UpsertPoolEquipmentCategoryAsync(PoolEquipmentCategory category, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await UpsertAsync("PoolEquipmentCategories", EquipmentPartition(category.Scope, category.ScopeOwnerId), category.Id, category, cancellationToken);
    }

    public async Task UpsertServiceAsync(ServiceOffering service, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await UpsertAsync("Services", ServicePartition(service.CompanyId), service.Id, service, cancellationToken);
    }

    public async Task UpsertMaterialAsync(Material material, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await UpsertAsync("Materials", MaterialPartition(material.CompanyId), material.Id, material, cancellationToken);
    }

    public async Task UpsertPoolEquipmentItemAsync(PoolEquipmentItem item, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await UpsertAsync("PoolEquipmentItems", EquipmentPartition(item.Scope, item.ScopeOwnerId), item.Id, item, cancellationToken);
    }

    public async Task DeleteServiceAsync(string companyId, string serviceId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var table = tableServiceClient.GetTableClient("Services");
        await table.DeleteEntityAsync(
            AzureTableKey.ToStorageKey(ServicePartition(companyId)),
            AzureTableKey.ToStorageKey(serviceId),
            ETag.All,
            cancellationToken);
    }

    public async Task DeletePoolEquipmentItemAsync(EquipmentScope scope, string scopeOwnerId, string itemId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var table = tableServiceClient.GetTableClient("PoolEquipmentItems");
        await table.DeleteEntityAsync(
            AzureTableKey.ToStorageKey(EquipmentPartition(scope, scopeOwnerId)),
            AzureTableKey.ToStorageKey(itemId),
            ETag.All,
            cancellationToken);
    }

    public async Task UpsertVisitAsync(ServiceVisit visit, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await UpsertAsync("ServiceVisits", CompanyPartition(visit.CompanyId), visit.Id, visit, cancellationToken);
    }

    public async Task UpsertVisitCompletionAsync(VisitCompletion completion, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await UpsertAsync("VisitCompletions", CompanyPartition(completion.CompanyId), completion.VisitId, completion, cancellationToken);
    }

    public async Task UpsertEmailLogAsync(EmailLogEntry emailLog, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await UpsertAsync("EmailLogs", string.IsNullOrWhiteSpace(emailLog.CompanyId) ? "PLATFORM" : CompanyPartition(emailLog.CompanyId), emailLog.Id, emailLog, cancellationToken);
    }

    public async Task UpsertSystemSettingsAsync(SystemSettings settings, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (initialized)
        {
            return;
        }

        await initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (initialized)
            {
                return;
            }

            foreach (var tableName in AzureStorageTableInitializer.TableNames)
            {
                await tableServiceClient.CreateTableIfNotExistsAsync(tableName, cancellationToken);
            }

            var users = await GetPartitionWithoutInitializationAsync<AppUser>("Users", "USER", cancellationToken);
            if (users.Count == 0)
            {
                await SeedAsync(cancellationToken);
            }
            else
            {
                await HydrateUserMembershipLookupIfMissingAsync(cancellationToken);
            }

            initialized = true;
        }
        finally
        {
            initializationLock.Release();
        }
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        var seed = new InMemoryServiceBusinessStore();

        foreach (var user in await seed.GetUsersAsync(cancellationToken))
        {
            await UpsertWithoutInitializationAsync("Users", "USER", user.Id, user, cancellationToken);
            await UpsertWithoutInitializationAsync("UserByEmail", "EMAIL", NormalizeEmail(user.Email), new UserLookup(user.Id, user.Email), cancellationToken);
        }

        foreach (var role in await seed.GetRoleDefinitionsAsync(cancellationToken))
        {
            await UpsertWithoutInitializationAsync("RoleDefinitions", "ROLE", role.Role.ToString(), role, cancellationToken);
        }

        foreach (var user in await seed.GetUsersAsync(cancellationToken))
        {
            var profile = await seed.GetIndependentHomeOwnerProfileAsync(user.Id, cancellationToken);
            if (profile is not null)
            {
                await UpsertWithoutInitializationAsync("IndependentHomeOwnerProfiles", "HOMEOWNER_PROFILE", profile.UserId, profile, cancellationToken);

                foreach (var category in await seed.GetPoolEquipmentCategoriesAsync(EquipmentScope.HomeOwner, profile.UserId, cancellationToken))
                {
                    await UpsertWithoutInitializationAsync("PoolEquipmentCategories", EquipmentPartition(category.Scope, category.ScopeOwnerId), category.Id, category, cancellationToken);
                }

                foreach (var item in await seed.GetPoolEquipmentItemsAsync(EquipmentScope.HomeOwner, profile.UserId, cancellationToken))
                {
                    await UpsertWithoutInitializationAsync("PoolEquipmentItems", EquipmentPartition(item.Scope, item.ScopeOwnerId), item.Id, item, cancellationToken);
                }
            }
        }

        foreach (var type in await seed.GetCompanyTypesAsync(cancellationToken))
        {
            await UpsertWithoutInitializationAsync("CompanyTypes", "COMPANY_TYPE", type.Id, type, cancellationToken);
        }

        foreach (var company in await seed.GetCompaniesAsync(cancellationToken))
        {
            await UpsertWithoutInitializationAsync("Companies", "COMPANY", company.Id, company, cancellationToken);

            foreach (var membership in await seed.GetMembershipsForCompanyAsync(company.Id, cancellationToken))
            {
                await UpsertWithoutInitializationAsync("CompanyMemberships", CompanyPartition(membership.CompanyId), MembershipRow(membership.UserId, membership.Role), membership, cancellationToken);
                await UpsertWithoutInitializationAsync("UserCompanyMemberships", UserPartition(membership.UserId), UserMembershipRow(membership.CompanyId, membership.Role), membership, cancellationToken);
            }

            foreach (var clientType in await seed.GetClientTypesAsync(company.Id, cancellationToken))
            {
                await UpsertWithoutInitializationAsync("ClientTypes", CompanyPartition(clientType.CompanyId), clientType.Id, clientType, cancellationToken);
            }

            foreach (var client in await seed.GetClientsAsync(company.Id, cancellationToken))
            {
                await UpsertWithoutInitializationAsync("CompanyClients", CompanyPartition(client.CompanyId), client.Id, client, cancellationToken);
            }

            foreach (var category in await seed.GetServiceCategoriesAsync(company.Id, cancellationToken))
            {
                await UpsertWithoutInitializationAsync("ServiceCategories", ServicePartition(category.CompanyId), category.Id, category, cancellationToken);
            }

            foreach (var category in await seed.GetMaterialCategoriesAsync(company.Id, cancellationToken))
            {
                await UpsertWithoutInitializationAsync("MaterialCategories", MaterialPartition(category.CompanyId), category.Id, category, cancellationToken);
            }

            foreach (var service in await seed.GetServicesAsync(company.Id, cancellationToken))
            {
                await UpsertWithoutInitializationAsync("Services", ServicePartition(service.CompanyId), service.Id, service, cancellationToken);
            }

            foreach (var material in await seed.GetMaterialsAsync(company.Id, cancellationToken))
            {
                await UpsertWithoutInitializationAsync("Materials", MaterialPartition(material.CompanyId), material.Id, material, cancellationToken);
            }

            foreach (var category in await seed.GetPoolEquipmentCategoriesAsync(EquipmentScope.Company, company.Id, cancellationToken))
            {
                await UpsertWithoutInitializationAsync("PoolEquipmentCategories", EquipmentPartition(category.Scope, category.ScopeOwnerId), category.Id, category, cancellationToken);
            }

            foreach (var item in await seed.GetPoolEquipmentItemsAsync(EquipmentScope.Company, company.Id, cancellationToken))
            {
                await UpsertWithoutInitializationAsync("PoolEquipmentItems", EquipmentPartition(item.Scope, item.ScopeOwnerId), item.Id, item, cancellationToken);
            }

            foreach (var visit in await seed.GetVisitsByDateAsync(company.Id, DateOnly.FromDateTime(DateTime.Today), cancellationToken))
            {
                await UpsertWithoutInitializationAsync("ServiceVisits", CompanyPartition(visit.CompanyId), visit.Id, visit, cancellationToken);
            }

            foreach (var visit in await seed.GetVisitsForClientAsync(company.Id, "client-1", cancellationToken))
            {
                await UpsertWithoutInitializationAsync("ServiceVisits", CompanyPartition(visit.CompanyId), visit.Id, visit, cancellationToken);
                var completion = await seed.GetVisitCompletionAsync(company.Id, visit.Id, cancellationToken);
                if (completion is not null)
                {
                    await UpsertWithoutInitializationAsync("VisitCompletions", CompanyPartition(completion.CompanyId), completion.VisitId, completion, cancellationToken);
                }
            }
        }

        foreach (var category in await seed.GetPoolEquipmentCategoriesAsync(EquipmentScope.Global, "global", cancellationToken))
        {
            await UpsertWithoutInitializationAsync("PoolEquipmentCategories", EquipmentPartition(category.Scope, category.ScopeOwnerId), category.Id, category, cancellationToken);
        }

        foreach (var item in await seed.GetPoolEquipmentItemsAsync(EquipmentScope.Global, "global", cancellationToken))
        {
            await UpsertWithoutInitializationAsync("PoolEquipmentItems", EquipmentPartition(item.Scope, item.ScopeOwnerId), item.Id, item, cancellationToken);
        }
    }

    private async Task<T?> GetAsync<T>(string tableName, string partitionKey, string rowKey, CancellationToken cancellationToken)
    {
        var table = tableServiceClient.GetTableClient(tableName);
        var response = await table.GetEntityIfExistsAsync<TableEntity>(
            AzureTableKey.ToStorageKey(partitionKey),
            AzureTableKey.ToStorageKey(rowKey),
            cancellationToken: cancellationToken);

        if (!response.HasValue)
        {
            return default;
        }

        return Deserialize<T>(response.Value!);
    }

    private async Task<IReadOnlyList<T>> GetPartitionAsync<T>(string tableName, string partitionKey, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetPartitionWithoutInitializationAsync<T>(tableName, partitionKey, cancellationToken);
    }

    private async Task<IReadOnlyList<T>> GetAllAsync<T>(string tableName, CancellationToken cancellationToken)
    {
        var table = tableServiceClient.GetTableClient(tableName);
        var items = new List<T>();
        await foreach (var entity in table.QueryAsync<TableEntity>(cancellationToken: cancellationToken))
        {
            items.Add(Deserialize<T>(entity));
        }

        return items;
    }

    private async Task<IReadOnlyList<T>> GetPartitionWithoutInitializationAsync<T>(string tableName, string partitionKey, CancellationToken cancellationToken)
    {
        var table = tableServiceClient.GetTableClient(tableName);
        var items = new List<T>();
        var storagePartitionKey = AzureTableKey.ToStorageKey(partitionKey);
        await foreach (var entity in table.QueryAsync<TableEntity>(e => e.PartitionKey == storagePartitionKey, cancellationToken: cancellationToken))
        {
            items.Add(Deserialize<T>(entity));
        }

        return items;
    }

    private async Task UpsertAsync<T>(string tableName, string partitionKey, string rowKey, T item, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken);
        await UpsertWithoutInitializationAsync(tableName, partitionKey, rowKey, item, cancellationToken);
    }

    private async Task UpsertWithoutInitializationAsync<T>(string tableName, string partitionKey, string rowKey, T item, CancellationToken cancellationToken)
    {
        var table = tableServiceClient.GetTableClient(tableName);
        await table.UpsertEntityAsync(
            ToEntity(
                AzureTableKey.ToStorageKey(partitionKey),
                AzureTableKey.ToStorageKey(rowKey),
                item),
            TableUpdateMode.Replace,
            cancellationToken);
    }

    private static async Task DeleteHomeOwnerPoolEquipmentPhotoRowsAsync(
        TableClient table,
        string partitionKey,
        string photoId,
        CancellationToken cancellationToken)
    {
        var rows = new List<string>();
        await foreach (var entity in table.QueryAsync<TableEntity>(e => e.PartitionKey == partitionKey, cancellationToken: cancellationToken))
        {
            if (entity.RowKey == $"{photoId}_meta" ||
                entity.RowKey.StartsWith($"{photoId}_chunk_", StringComparison.Ordinal))
            {
                rows.Add(entity.RowKey);
            }
        }

        foreach (var rowKey in rows)
        {
            await table.DeleteEntityAsync(partitionKey, rowKey, ETag.All, cancellationToken);
        }
    }

    private static IEnumerable<string> Chunk(string value, int size)
    {
        for (var index = 0; index < value.Length; index += size)
        {
            yield return value.Substring(index, Math.Min(size, value.Length - index));
        }
    }

    private async Task HydrateUserMembershipLookupIfMissingAsync(CancellationToken cancellationToken)
    {
        var userMemberships = await GetAllAsync<CompanyMembership>("UserCompanyMemberships", cancellationToken);
        if (userMemberships.Count > 0)
        {
            return;
        }

        var companyMemberships = await GetAllAsync<CompanyMembership>("CompanyMemberships", cancellationToken);
        foreach (var membership in companyMemberships)
        {
            await UpsertWithoutInitializationAsync("UserCompanyMemberships", UserPartition(membership.UserId), UserMembershipRow(membership.CompanyId, membership.Role), membership, cancellationToken);
        }
    }

    private static TableEntity ToEntity<T>(string partitionKey, string rowKey, T item) =>
        new(partitionKey, rowKey)
        {
            ["Json"] = JsonSerializer.Serialize(item, JsonOptions)
        };

    private static T Deserialize<T>(TableEntity entity) =>
        JsonSerializer.Deserialize<T>((string)entity["Json"], JsonOptions)
        ?? throw new InvalidOperationException($"Could not deserialize {typeof(T).Name} from Azure Table entity.");

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string CompanyPartition(string companyId) => $"COMPANY_{companyId}";

    private static string UserPartition(string userId) => $"USER#{userId}";

    private static string EquipmentPartition(EquipmentScope scope, string scopeOwnerId) => $"EQUIPMENT_{scope}_{scopeOwnerId}";

    private static string HomeOwnerPhotoPartition(string userId) => $"HOMEOWNER_PHOTOS_{userId}";

    internal static string ServicePartition(string companyId) =>
        string.Equals(companyId, "global", StringComparison.OrdinalIgnoreCase)
            ? "SERVICES_Global_global"
            : $"SERVICES_Company_{companyId}";

    internal static string MaterialPartition(string companyId) =>
        string.Equals(companyId, "global", StringComparison.OrdinalIgnoreCase)
            ? "MATERIALS_GLOBAL_global"
            : CompanyPartition(companyId);

    private static string MembershipRow(string userId, CompanyRole role) => $"USER_{userId}_ROLE_{role}";

    private static string UserMembershipRow(string companyId, CompanyRole role) => $"COMPANY#{companyId}#ROLE#{role}";

    private static SystemSettings GetConfiguredDefaultSystemSettings(IConfiguration configuration)
    {
        var configuredMode = configuration["SystemSettings:SystemMode"] ?? configuration["SystemMode"];
        var mode = Enum.TryParse<SystemMode>(configuredMode, ignoreCase: true, out var parsedMode)
            ? parsedMode
            : SystemMode.Pool;
        var devTest = bool.TryParse(configuration["SystemSettings:DevTest"], out var parsedDevTest) && parsedDevTest;
        return new SystemSettings(mode, devTest);
    }

    private sealed record UserLookup(string UserId, string Email);
}

internal static class AzureTableKey
{
    public static string ToStorageKey(string key)
    {
        var escaped = key
            .SelectMany(EscapeCharacter)
            .ToArray();

        return new string(escaped);
    }

    private static IEnumerable<char> EscapeCharacter(char character)
    {
        if (character is '/' or '\\' or '#' or '?' or '!' || char.IsControl(character))
        {
            foreach (var escaped in $"!{(int)character:X4}")
            {
                yield return escaped;
            }

            yield break;
        }

        yield return character;
    }
}
