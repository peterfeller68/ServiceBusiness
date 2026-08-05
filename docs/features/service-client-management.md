# Service Client Management

Status: Implemented
Owner: Product
Last reviewed: 2026-08-05

## Problem

System Administrators need a focused way to manage service client companies for the current Pool or Landscape service mode, including contact details, service type, status, and default service package assignment.

## Personas

- System Administrator
- Business Owner
- Business Employee
- Business Client
- Independent Home Owner

## Requirements

- Service clients are managed from a focused System Administrator page.
- The page is available at `/admin/service-clients`; legacy `/admin/companies` also routes to the same page.
- Service client rows are filtered by the current application mode, Pool or Landscape.
- Service client types are filtered to active company types that match the current application mode.
- System Administrators can create and edit service client records in the current implementation.
- System Administrators can set service client status to Active or Inactive from the grid and editor.
- System Administrators can assign an active global service package matching the service client's company type.
- Service client records must have an id, company type, name, and valid business email.
- Time zone defaults to `America/Los_Angeles` when blank.
- Service client ids are normalized to slug format when saved.

## User Flows

### View Service Clients

1. A System Administrator opens System Administrator / Service Clients.
2. The system loads company types, service client companies, and current-mode global service packages.
3. The page lists service clients whose type matches the current Pool or Landscape mode.

### Edit Service Client

1. A System Administrator selects the edit action for a service client.
2. The inline editor opens with service client type, name, business email, business phone, service package, time zone, and active status.
3. The administrator saves.
4. The system validates the company type, email, and service package, then saves the service client.

### Change Service Client Status

1. A System Administrator toggles the service client active status or selects the delete/archive action.
2. The system updates the service client status to Active or Inactive.
3. The grid reloads with the updated status.

## UI Expectations

- The page heading is Service Clients.
- The page eyebrow is System Administrator.
- The page uses a collapsible management panel.
- The table shows Status, Name, Type, Service Package, Email, Phone, and Action.
- Row actions are icon-only controls for edit and delete/archive.
- The editor is inline and uses the existing form-grid pattern.
- The editor fields are Service client type, Name, Business email, Business phone, Service package, Time zone, and Active.
- Success and validation errors are shown inline.

## Data Model Impact

- `CompanyType` stores service client type id, name, description, and active status.
- `Company` stores id, company type id, name, business email, business phone, time zone, status, and optional service package id.
- `CompanyStatus` includes Active, Suspended, and Inactive, though the current UI toggles Active/Inactive.
- `ServicePackageId` on `Company` links a service client to an active global package for the corresponding Pool or Landscape catalog scope.
- Azure Table storage persists service clients in the `Companies` table and company types in the `CompanyTypes` table.

## Authorization Rules

- Service client management requires System Administrator access.
- Business Owners, Business Employees, Business Clients, and Independent Home Owners do not receive service client management access.
- Service package assignment validates that the selected package is active in the global catalog scope matching the service client's company type.
- Current-mode filtering is enforced in the UI; service methods still require System Administrator access for reads and writes.

## Acceptance Criteria

- Implemented: System Administrators can open Service Clients at `/admin/service-clients`.
- Implemented: Legacy `/admin/companies` routes to the same Service Clients page.
- Implemented: Service clients are filtered by current app mode.
- Implemented: Service client type choices are filtered by current app mode.
- Implemented: Service client rows show status, name, type, service package, email, phone, and actions.
- Implemented: Service clients can be edited from an inline editor.
- Implemented: Service client status can be toggled Active/Inactive.
- Implemented: Service package assignment is validated against active current-service global packages.
- Implemented: Service client save validates id, company type, name, business email, and company type existence.
- Implemented: Service client save normalizes ids, email, phone, time zone, and empty service package values.
- Not implemented: Company profile page for Business Owners to maintain their own service client profile.
- Not implemented: Billing/Stripe status display and management.
- Not implemented: Logo, website, address, default notification-template, and service-area persistence in the `Company` model.

## Tests

- `AuthorizationTests.System_admin_can_create_and_archive_company`
- `AuthorizationTests.System_admin_can_assign_service_package_to_service_client`
- `AuthorizationTests.Business_owner_registration_uses_current_system_mode_company_type`
- `AuthorizationTests.Business_owner_registration_creates_home_owner_client_type`
- `AuthorizationTests.System_admin_can_read_platform_companies`

## User Documentation Impact

- Created `docs/user-guide/service-client-management.md`.
- Updated `docs/user-guide/index.md` with a Service Client Management link.

## Current Implementation

- `AdminCompaniesPage.razor` implements `/admin/service-clients` and `/admin/companies`.
- The navigation label for System Administrators is Service Clients.
- `PlatformAdminService.GetCompanyTypesAsync`, `GetCompaniesAsync`, `UpsertCompanyAsync`, and `SetCompanyStatusAsync` provide the service-client management operations.
- `AdminCompaniesPage.razor` uses `ApplicationModeService` to filter visible companies and type choices to Pool or Landscape.
- The page loads active global service packages from `CompanyAdminService.GetServicePackagesAsync(appMode.GlobalCatalogCompanyId)` for assignment.
- `PlatformAdminService.UpsertCompanyAsync` validates service package ids against the global catalog scope inferred from the selected company type.
- Business Owner registration also creates a service client company and active `CompanyAdmin` membership through `OnboardingService`.

## Outstanding Tasks

- Decide whether System Administrators should continue to create service clients directly or whether creation should be limited to Business Owner registration.
- Add browser/component coverage for `/admin/service-clients`.
- Add persisted company fields for website, physical address, logo, service area, billing status, and notification templates if those remain product requirements.
- Add a Business Owner company profile/settings page if owners should maintain their own service client details.
- Add true archive/reactivate language or behavior alignment; current UI labels delete/archive actions but stores `Inactive`.
- Add source-of-truth tests for UI mode filtering if it becomes critical.

## Change Log

- 2026-08-05: Documented implemented service client management behavior from code, tests, UI pages, storage, and configuration.
