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
    private readonly TableServiceClient tableServiceClient;
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
        var all = await GetAllAsync<CompanyMembership>("CompanyMemberships", cancellationToken);
        return all.Where(m => m.UserId == userId).ToList();
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

    public async Task<IReadOnlyList<ServiceOffering>> GetServicesAsync(string companyId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetPartitionAsync<ServiceOffering>("Services", CompanyPartition(companyId), cancellationToken);
    }

    public async Task<IReadOnlyList<Material>> GetMaterialsAsync(string companyId, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        return await GetPartitionAsync<Material>("Materials", CompanyPartition(companyId), cancellationToken);
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
    }

    public async Task UpsertClientAsync(CompanyClient client, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await UpsertAsync("CompanyClients", CompanyPartition(client.CompanyId), client.Id, client, cancellationToken);
    }

    public async Task UpsertServiceAsync(ServiceOffering service, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await UpsertAsync("Services", CompanyPartition(service.CompanyId), service.Id, service, cancellationToken);
    }

    public async Task UpsertMaterialAsync(Material material, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await UpsertAsync("Materials", CompanyPartition(material.CompanyId), material.Id, material, cancellationToken);
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
            }

            foreach (var clientType in await seed.GetClientTypesAsync(company.Id, cancellationToken))
            {
                await UpsertWithoutInitializationAsync("ClientTypes", CompanyPartition(clientType.CompanyId), clientType.Id, clientType, cancellationToken);
            }

            foreach (var client in await seed.GetClientsAsync(company.Id, cancellationToken))
            {
                await UpsertWithoutInitializationAsync("CompanyClients", CompanyPartition(client.CompanyId), client.Id, client, cancellationToken);
            }

            foreach (var service in await seed.GetServicesAsync(company.Id, cancellationToken))
            {
                await UpsertWithoutInitializationAsync("Services", CompanyPartition(service.CompanyId), service.Id, service, cancellationToken);
            }

            foreach (var material in await seed.GetMaterialsAsync(company.Id, cancellationToken))
            {
                await UpsertWithoutInitializationAsync("Materials", CompanyPartition(material.CompanyId), material.Id, material, cancellationToken);
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
    }

    private async Task<T?> GetAsync<T>(string tableName, string partitionKey, string rowKey, CancellationToken cancellationToken)
    {
        var table = tableServiceClient.GetTableClient(tableName);
        try
        {
            var response = await table.GetEntityAsync<TableEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);
            return Deserialize<T>(response.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return default;
        }
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
        await foreach (var entity in table.QueryAsync<TableEntity>(e => e.PartitionKey == partitionKey, cancellationToken: cancellationToken))
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
        await table.UpsertEntityAsync(ToEntity(partitionKey, rowKey, item), TableUpdateMode.Replace, cancellationToken);
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

    private static string MembershipRow(string userId, CompanyRole role) => $"USER_{userId}_ROLE_{role}";

    private sealed record UserLookup(string UserId, string Email);
}
