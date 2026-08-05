# Email and Azure Storage Setup

## Azure Communication Services Email

The application uses `AzureCommunicationEmailNotificationQueue` for notification delivery.

Configure these values with user secrets, environment variables, Azure App Configuration, or Key Vault-backed configuration:

```powershell
dotnet user-secrets set "Email:AzureCommunicationServices:ConnectionString" "<acs-connection-string>" --project src\ServiceBusiness.Web\ServiceBusiness.Web.csproj
dotnet user-secrets set "Email:AzureCommunicationServices:SenderAddress" "DoNotReply@<verified-domain>" --project src\ServiceBusiness.Web\ServiceBusiness.Web.csproj
dotnet user-secrets set "Email:TestRecipientEmail" "developer-test-inbox@example.com" --project src\ServiceBusiness.Web\ServiceBusiness.Web.csproj
```

Behavior:

- If ACS settings are missing, email attempts are logged with `Queued` status and business workflows continue.
- If ACS settings are present, email is sent with `Azure.Communication.Email.EmailClient`.
- If the recipient user is marked `IsTestUser` and `Email:TestRecipientEmail` is set, the actual recipient is rerouted to the test inbox and the email log status is `TestRerouted`.

## Azure Storage

The application references `Azure.Data.Tables` and includes a startup initializer for the current logical table set.

Configure:

```powershell
dotnet user-secrets set "AzureStorage:ConnectionString" "<storage-connection-string>" --project src\ServiceBusiness.Web\ServiceBusiness.Web.csproj
dotnet user-secrets set "AzureStorage:UseAzureStorage" "true" --project src\ServiceBusiness.Web\ServiceBusiness.Web.csproj
```

When enabled, `AzureStorageTableInitializer` creates these tables if they do not exist:

```text
CompanyTypes
Companies
Users
UserByGoogleSubject
UserByEmail
RoleDefinitions
CompanyMemberships
UserCompanyMemberships
CompanyClients
ClientTypes
Services
Materials
ServiceVisits
EmailLogs
```

When `AzureStorage:UseAzureStorage` is `true`, the app uses `AzureTableServiceBusinessStore`.

On first access, if the `Users` table is empty, the store seeds the current MVP data set, including:

- seeded system admin and test users
- role definitions
- company types
- Clearwater demo company
- company memberships
- client types, clients, services, materials, visits, and completed visit history stored on visits

Google-authenticated users are persisted to:

- `Users`
- `UserByEmail`
- `UserByGoogleSubject`

When `AzureStorage:UseAzureStorage` is `false`, the app uses `InMemoryServiceBusinessStore` for local development.
