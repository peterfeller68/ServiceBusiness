# Observability Setup

The application uses the Azure Monitor OpenTelemetry distro for Application Insights telemetry.

## Configuration

Set the Application Insights connection string with user secrets, environment variables, Azure App Configuration, or Key Vault-backed configuration:

```powershell
dotnet user-secrets set "ApplicationInsights:ConnectionString" "<application-insights-connection-string>" --project src\ServiceBusiness.Web\ServiceBusiness.Web.csproj
```

The checked-in `appsettings.json` contains an empty placeholder. When the connection string is empty, Azure Monitor export is not registered and local development continues without telemetry export.

## Collected Telemetry

The Azure Monitor OpenTelemetry distro collects ASP.NET Core request telemetry and dependency telemetry.

The app also registers a custom `ServiceBusiness` activity source and meter for business workflow telemetry:

- `DecideAccessRequest` activity
- `CompleteVisit` activity
- `SendEmailNotification` activity
- `servicebusiness.account_approval_decisions` counter
- `servicebusiness.completed_visits` counter
- `servicebusiness.email_notifications` counter

Business telemetry tags include company ID, visit ID, role, decision, email type, recipient test-user flag, and email status where applicable.
