# Architecture Document

## 1. Architecture Goals

The application is a multi-tenant SaaS platform for pool cleaning and landscaping companies. It must be secure, mobile-friendly, tenant-isolated, Azure-hosted, and built with Blazor. Data persistence must use Azure Storage.

Primary goals:

- Support small businesses with simple operational workflows.
- Keep company data isolated by tenant.
- Use Google Authentication for identity.
- Use Azure Storage as the system of record.
- Integrate Stripe for payments.
- Support responsive desktop and mobile UI.
- Keep the MVP straightforward while leaving room for maps, photos, offline work, and richer billing.

## 2. Recommended Azure Architecture

Recommended services:

- Azure App Service: Hosts the Blazor application and backend API.
- Blazor Web App: UI built with Blazor. Use server-side rendering plus interactive components, or Blazor WebAssembly with an ASP.NET Core API depending on project preference.
- ASP.NET Core: Backend application services, authorization, API endpoints, background job entry points, and integrations.
- Azure Table Storage: Main structured entity persistence.
- Azure Blob Storage: File storage for logos, attachments, future photos, generated exports, and email assets.
- Azure Queue Storage: Background work queue for email, schedule generation, route optimization, and webhook processing.
- Azure Key Vault: Secrets for Google auth, Stripe, email provider, maps provider, and storage connection strings.
- Azure App Configuration: Non-secret runtime configuration and feature flags.
- Azure Application Insights: Telemetry, logs, traces, failures, and performance monitoring.
- Azure Communication Services: Email delivery for the current implementation.
- Azure Maps: Geocoding, maps, and route optimization.
- Stripe: Payments, invoices, customers, payment links, payment intents, and webhooks.

## 3. High-Level Components

```mermaid
flowchart LR
    User["Browser / Mobile Browser"] --> Blazor["Blazor UI"]
    Blazor --> App["ASP.NET Core App Services"]
    App --> Auth["Google Authentication"]
    App --> Tables["Azure Table Storage"]
    App --> Blobs["Azure Blob Storage"]
    App --> Queues["Azure Queue Storage"]
    App --> Stripe["Stripe API"]
    App --> Email["Email Provider"]
    App --> Maps["Azure Maps"]
    App --> Insights["Application Insights"]
    App --> KeyVault["Azure Key Vault"]
```

## 4. Application Layers

### 4.1 UI Layer

Technology:

- Blazor.
- Responsive layouts for desktop and mobile.
- Role-aware navigation.
- Form validation using shared request models or view models.

Responsibilities:

- Render persona-specific dashboards.
- Submit commands to application services.
- Display filtered tenant data.
- Support mobile field workflows.
- Avoid exposing hidden authorization assumptions in the UI only; backend must enforce authorization.

### 4.2 API and Application Service Layer

Technology:

- ASP.NET Core.
- Minimal APIs or controllers.
- Dependency-injected services.

Responsibilities:

- Authentication callback handling.
- Authorization and tenant membership validation.
- Business workflows.
- Stripe integration.
- Email job creation.
- Schedule generation.
- Report generation.
- Route optimization orchestration.

Suggested services:

- `IdentityService`
- `AuthorizationService`
- `CompanyService`
- `CompanyTypeService`
- `MembershipService`
- `ClientService`
- `ClientTypeService`
- `ServiceCatalogService`
- `MaterialCatalogService`
- `PoolEquipmentCatalogService`
- `ScheduleService`
- `VisitService`
- `RouteService`
- `NotificationService`
- `BillingService`
- `ReportService`
- `AuditService`

### 4.3 Storage Access Layer

Technology:

- Azure.Data.Tables for Table Storage.
- Azure.Storage.Blobs for Blob Storage.
- Azure.Storage.Queues for Queue Storage.

Responsibilities:

- Encapsulate partition key and row key design.
- Provide query methods that avoid full table scans.
- Normalize table entity serialization.
- Support optimistic concurrency through ETags.
- Provide idempotency helpers for external webhooks and queued jobs.

### 4.4 Background Worker Layer

Options:

- Use a hosted background service inside the App Service for MVP.
- Use Azure Functions for a more scalable worker model.

Responsibilities:

- Send emails.
- Process Stripe webhooks.
- Generate recurring visit instances.
- Generate reports and exports.
- Run route optimization jobs.
- Retry transient failures.

Current implementation:

- `InvoicingJobService` is a callable application service that creates invoices for closed visits without invoice ids.
- `EmailJobService` is a callable application service that processes `EmailLogEntry` rows with `New` status.
- `ScheduledJobRunner` runs the invoicing pass followed by the email pass.
- The WebApp registers `ServiceBusinessJobScheduler` as a hosted service for automatic recurring job execution.
- `Jobs:Scheduler:Enabled`, `Jobs:Scheduler:InitialDelaySeconds`, and `Jobs:Scheduler:IntervalMinutes` configure scheduler behavior.

## 5. Authentication Flow

1. User clicks Sign In with Google.
2. Google authenticates the user and redirects to the application.
3. Application validates the Google token.
4. Application looks up user by Google subject ID.
5. If no user exists, create an application user profile.
6. Application loads platform role and company memberships.
7. UI shows the appropriate dashboard and company selector.

Current implementation details:

- ASP.NET Core cookie authentication is the application session mechanism.
- `Microsoft.AspNetCore.Authentication.Google` is registered when `Authentication:Google:ClientId` and `Authentication:Google:ClientSecret` are configured.
- `/auth/google` starts the Google challenge.
- `/auth/google-complete` creates or updates `AppUser` from Google claims and signs in with an app cookie containing the application user ID claim.
- `/auth/test-signin` is a development-only bypass for seeded users marked `IsTestUser`.
- `/auth/signout` clears the application cookie.
- The Blazor shell reads the current app user to show a profile indicator that opens `/profile`.
- `/profile` updates mutable profile fields and the email notification preference through `UserProfileService`.
- The Blazor shell uses authentication state for navigation visibility so unauthenticated users see only public links.
- Independent Homeowner registration creates an active user without company memberships, stores an owner profile with home address/access notes, and seeds owner-scoped equipment data.

## 6. Authorization Model

Every secured operation requires:

- Authenticated user.
- Platform role check for System Admin functions.
- Company membership check for company-scoped functions.
- Role check for protected company functions.
- Client access check for Company Client User functions.
- Owner-scope user check for Independent Homeowner functions.

Authorization examples:

- System Admin can manage company types and companies.
- System Admin can view all users, promote existing users to System Admin, and enable or disable users while preserving at least one active System Admin.
- System Admin can edit built-in role definitions, including display metadata, owner-approval requirements, and permission lists.
- Company Admin can manage users, clients, services, materials, schedules, billing, and reports for their company.
- Standard Company User can view assigned visits and complete those visits.
- Company Client User can view their own service history, billing history, and messages.
- Independent Home Owner can manage owner-scoped pool equipment without company membership.

Current UI enforcement:

- Navigation items are filtered by the current user's active company memberships, system-admin flag, and independent-homeowner state.
- Navigation sections can be expanded or collapsed, and each visible leaf points to an application route.
- Editor leaves use focused routes instead of routing back to a combined dashboard: `/admin/companies`, `/admin/users`, `/admin/roles`, `/admin/email-log`, `/admin/catalog/poolequipment`, `/admin/catalog/materials`, `/admin/catalog/services`, `/company/users`, `/poolequipment`, `/catalog/poolequipment`, `/catalog/materials`, and `/catalog/services`.
- Focused data-management pages render collapsible table panels with inline add/edit editor panels rather than always-visible edit forms.
- Public navigation is limited to Home and Help when no application cookie is present.
- Authenticated navigation hides Home and uses Dashboard as the post-sign-in workspace entry point.
- `ApplicationModeService` reads the persisted `SystemSettings` row and provides PoolShark/TreeShark branding, hero imagery, and Pool Equipment visibility.
- Landscape mode suppresses Pool Equipment navigation and redirects direct Pool Equipment routes back to the dashboard.
- Backend service methods still enforce authorization independently of hidden navigation.

## 7. Multi-Tenant Storage Strategy

All company-scoped entities include `CompanyId`.

Azure Table Storage partition design should optimize for common queries:

- Tenant-wide management queries by `CompanyId`.
- Date-based schedule and visit queries.
- User daily assignment queries.
- Client service history queries.
- Pending approval queues.
- Stripe webhook idempotency checks.

Use separate tables for major aggregate types rather than one universal table. This keeps partition strategies clear and reduces accidental cross-tenant queries.

## 8. Storage Services

### 8.1 Azure Table Storage

Use for:

- Users.
- Companies.
- Memberships.
- Clients.
- Services.
- Materials.
- Pool equipment.
- Schedules.
- Visits.
- Billing references.
- Messages.
- Audit events.

Current implementation:

- `Azure.Data.Tables` is referenced by the Azure Storage infrastructure project.
- `AzureStorageTableInitializer` creates the currently modeled tables at startup when `AzureStorage:UseAzureStorage` is enabled.
- `AzureTableServiceBusinessStore` is the active repository when `AzureStorage:UseAzureStorage` is enabled.
- `AzureTableServiceBusinessStore` stores MVP records as JSON payloads in Azure Table entities and maintains lookup tables for email and Google subject sign-in.
- Role definitions are persisted in the `RoleDefinitions` table and can be updated by system admins.
- Service, material, and pool-equipment category tables are provisioned and used to group catalog items.
- Initial seed data is defined in `InMemoryServiceBusinessStore` and is reused by the Azure Table store when hydrating an empty `Users` table; it includes Clearwater plus Pool1Clean1, PoolClean2, Landscape1, and Landscape2 test companies with service, material, and pool-equipment catalog data, three independent homeowner test users with owner-scoped pool-equipment records, and ten unassociated `other-{n}@gmail.com` test users.
- `CompanyAdminService.GetCatalogOverviewAsync` allows either system-admin access or active company admin/user access so focused system-admin catalog pages can inspect company-scoped starter catalog data.
- `PlatformAdminService` owns company CRUD, system user create/edit/status/admin actions, and role-definition editing.
- `CompanyAdminService` owns company-scoped user access decisions, company membership activation/deactivation, and company-scoped role reassignment with last-admin guardrails.
- `CompanyAdminService` owns service/material category and item create/edit/archive/reactivate actions with system-admin or company-admin authorization.
- `CompanyAdminService` also owns pool-equipment category and item create/edit/archive/reactivate actions across global, company, and homeowner scopes.
- `CompanyAdminService` supports copy-as-custom actions for starter service, material, and pool-equipment records by creating new non-system-managed records in the current scope.
- System mode is table-backed in the singleton `SystemSettings` row; `SystemMode` accepts `Pool` or `Landscape`, and app configuration only supplies the startup default when the row is missing.
- `InMemoryServiceBusinessStore` remains the default local-development repository when Azure Storage is disabled.

### 8.2 Azure Blob Storage

Use for:

- Company logos.
- Future visit photos.
- Report exports.
- Email attachments.
- Imported CSV files.

Blob containers:

- `company-assets`
- `visit-attachments`
- `report-exports`
- `imports`

Blob naming:

- Include `CompanyId` in blob paths for tenant isolation.
- Example: `company-assets/{companyId}/logo/{fileName}`.

### 8.3 Azure Queue Storage

Use for:

- `email-jobs`
- `stripe-webhook-jobs`
- `schedule-generation-jobs`
- `route-optimization-jobs`
- `report-export-jobs`

Queue messages should include:

- Job ID.
- Company ID if applicable.
- Entity ID.
- Job type.
- Requested by user ID if applicable.
- Created timestamp.
- Idempotency key.

## 9. Stripe Integration

Stripe responsibilities:

- Customer management.
- Payment links or hosted invoices.
- Payment records.
- Webhooks for payment status changes.

Application responsibilities:

- Store Stripe IDs and status snapshots.
- Enqueue webhook processing jobs.
- Process webhooks idempotently.
- Never store card data.
- Display invoice and payment history to authorized users.

Suggested webhook events:

- `customer.created`
- `customer.updated`
- `invoice.created`
- `invoice.finalized`
- `invoice.paid`
- `invoice.payment_failed`
- `payment_intent.succeeded`
- `payment_intent.payment_failed`

## 10. Email Integration

The application sends emails through Azure Communication Services when configured.

Target email sending flow:

1. Application persists the source event, such as visit completion.
2. Application creates an email job in Queue Storage.
3. Background worker processes the email job.
4. Worker sends the email through the provider.
5. Worker records delivery attempt status.

This ensures the visit completion workflow succeeds even if email delivery has a transient failure.

Current implementation details:

- `AzureCommunicationEmailNotificationQueue` implements `INotificationQueue`.
- If `Email:AzureCommunicationServices:ConnectionString` or `Email:AzureCommunicationServices:SenderAddress` is missing, the queue records an `EmailLogEntry` with `Queued` status and does not throw.
- If Azure Communication Services is configured, the queue sends email through `EmailClient` and logs `Sent` or `Failed`.
- Test-user email is rerouted to `Email:TestRecipientEmail` when configured and logged with `TestRerouted` status.
- Recipient users with `EmailNotificationsEnabled` set to `false` are logged with `Suppressed` status and are not sent to the provider.
- `EmailJobService` processes invoice email rows and other `New` email log rows through `IEmailSender`; test recipients and DevTest rows are marked `Sent` without provider delivery.
- `ServiceBusinessJobScheduler` automatically invokes `ScheduledJobRunner`, which lets invoice email rows created by `InvoicingJobService` be processed by `EmailJobService`.
- Logs / Email Log displays role-scoped email log entries for System Administrators, Business Owners, Business Clients, and Independent Home Owners.

## 11. Scheduling and Recurrence

Use recurrence templates to create visit instances.

Design:

- `RecurringSchedule` stores recurrence rules.
- `ServiceVisit` stores actual scheduled work.
- Generated visits reference the recurring schedule ID.
- Each visit can be edited independently.

Generation strategy:

- Generate rolling windows, such as 30 to 60 days ahead.
- Store idempotency keys to prevent duplicates.
- Regenerate only missing future visits.

## 12. Route Optimization

MVP:

- Show assigned visits ordered by scheduled window.
- Allow manual reorder.
- Open external navigation links.

Enhanced phase:

- Geocode client service addresses using Azure Maps.
- Store latitude and longitude on client service location.
- Optimize routes by user and date.
- Persist optimized route order.

## 13. Reporting Architecture

Reports should query Table Storage by partition-friendly dimensions:

- Visits by company and date.
- Visits by assigned user and date.
- Visits by client and date.
- Billing records by company and date.

For MVP, reports can be generated on demand from operational tables.

For later scale, introduce denormalized reporting tables:

- `VisitReportDaily`
- `UserProductivityDaily`
- `ClientBillingSummary`
- `MaterialUsageDaily`

## 14. Security Architecture

Security requirements:

- Use HTTPS only.
- Store secrets in Key Vault.
- Validate tenant membership on every company-scoped request.
- Use secure cookies or token handling according to the chosen Blazor hosting model.
- Apply CSRF protection for cookie-authenticated endpoints.
- Avoid logging secrets, access notes, gate codes, payment identifiers beyond required IDs, or personal data unnecessarily.
- Encrypt data at rest using Azure defaults.
- Use Azure role-based access for managed identity access to storage and Key Vault.

## 14.1 Observability Architecture

The current implementation uses the Azure Monitor OpenTelemetry distro for Application Insights.

Collected telemetry:

- ASP.NET Core request telemetry.
- HTTP dependency telemetry included by the distro.
- Custom `ServiceBusiness` activity source spans for account approval decisions, visit completion, and email notification sending.
- Custom `ServiceBusiness` meter counters for account approval decisions, visit completions, and email notifications.

Configuration:

- Set `ApplicationInsights:ConnectionString` to enable Azure Monitor export.
- Leave the connection string empty for local development without telemetry export.
- Use `docs/observability-setup.md` for setup steps.

## 15. Blazor Hosting Recommendation

Recommended for MVP:

- Blazor Web App with server-side interactivity.
- ASP.NET Core Identity-style external Google auth integration without local passwords.
- Shared server-side application services.

Reasons:

- Faster MVP.
- Simple server-side authorization.
- Reduced complexity around tokens in browser storage.
- Works well for responsive browser-based mobile workflows.

Future option:

- Add Blazor WebAssembly or native mobile app support if offline mode or richer mobile UX becomes necessary.

## 16. Deployment Environments

Recommended environments:

- Development
- Test
- Production

Each environment should have:

- Separate Azure Storage account or separate table/blob/queue prefixes.
- Separate Stripe mode or keys.
- Separate Google OAuth client configuration.
- Separate email provider configuration.
- Separate Application Insights resource.

## 17. Implementation Project Structure

Suggested solution structure:

- `ServiceBusiness.Web`: Blazor UI and app host.
- `ServiceBusiness.Application`: Use cases, commands, validation, authorization orchestration.
- `ServiceBusiness.Domain`: Domain models, enums, value objects.
- `ServiceBusiness.Infrastructure.AzureStorage`: Table, Blob, and Queue implementations.
- `ServiceBusiness.Infrastructure.Integrations`: Google, Stripe, Email, Maps.
- `ServiceBusiness.Tests`: Unit, application scenario, and browser scenario tests.

## 18. Quality Gates

Before release:

- Unit tests for authorization checks.
- Application scenario tests for end-to-end business workflows at the service layer.
- Playwright browser scenario tests for critical persona UI workflows.
- Unit tests for schedule recurrence generation.
- Unit tests for Stripe webhook idempotency.
- Integration tests for table repositories.
- UI smoke tests for each persona dashboard.
- Manual mobile verification for field-user visit completion.
- Manual tenant isolation verification.
