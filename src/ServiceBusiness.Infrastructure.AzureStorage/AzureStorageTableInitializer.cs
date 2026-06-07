using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ServiceBusiness.Infrastructure.AzureStorage;

public sealed class AzureStorageTableInitializer(IConfiguration configuration) : IHostedService
{
    public static readonly string[] TableNames =
    [
        "CompanyTypes",
        "Companies",
        "Users",
        "UserByGoogleSubject",
        "UserByEmail",
        "RoleDefinitions",
        "CompanyMemberships",
        "UserCompanyMemberships",
        "CompanyClients",
        "ClientTypes",
        "Services",
        "Materials",
        "ServiceVisits",
        "VisitCompletions",
        "EmailLogs"
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!bool.TryParse(configuration["AzureStorage:UseAzureStorage"], out var useAzureStorage) || !useAzureStorage)
        {
            return;
        }

        var connectionString = configuration["AzureStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("AzureStorage:UseAzureStorage is true, but AzureStorage:ConnectionString is not configured.");
        }

        var tableServiceClient = new TableServiceClient(connectionString);
        foreach (var tableName in TableNames)
        {
            await tableServiceClient.CreateTableIfNotExistsAsync(tableName, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
