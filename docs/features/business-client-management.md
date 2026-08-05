# Business Client Management

Status: Implemented
Owner: Product
Last reviewed: 2026-08-05

## Problem

System Administrators and Business Owners need focused tools to create, edit, archive, and reactivate the customer/property records that service businesses visit, invoice, and expose to client users.

## Personas

- System Administrator
- Business Owner
- Business Employee
- Business Client
- Independent Home Owner

## Requirements

- Business clients are managed from a focused page at `/clients` for Business Owners and `/admin/clients` for System Administrators.
- System Administrators can view business clients across service clients matching the current Pool or Landscape app mode.
- Business Owners can view and manage business clients for their active service client.
- Business client rows show active status, name, client type, service package, email, phone, and actions.
- System Administrator rows also show the service client company.
- Business clients can be created, edited, archived, and reactivated.
- Business client records require a service client, display name, primary contact, valid email, service address, and active client type.
- The current client type list is normalized to include the active Home Owner type for the selected service client.
- Business clients can be assigned an active service package from either the service client's own package list or the current app-mode global package list.
- Business client ids created from the UI are generated from the display name plus a GUID suffix and truncated to 64 characters.
- Saved business client fields are normalized.

## User Flows

### System Admin Views Business Clients

1. A System Administrator opens `/admin/clients`.
2. The system loads service clients, company types, business-client rows, client types, and service packages.
3. The page lists business clients for service clients matching the current app mode.

### Business Owner Views Business Clients

1. A Business Owner opens `/clients`.
2. The system resolves the current company from the active `CompanyAdmin` membership.
3. The page lists business clients for that service client.

### Create Business Client

1. The user chooses Create.
2. The inline editor opens.
3. The user enters client type, optional service package, name, primary contact, email, phone, service address, rate override, access notes, and active status.
4. The system validates required fields and saves the business client.

### Archive Or Reactivate Business Client

1. The user toggles the active status or chooses the delete/archive action.
2. The system saves the business client with the updated `IsActive` value.
3. The page reloads and displays the updated status.

## UI Expectations

- The page heading is Business Clients.
- The page eyebrow is System Administrator on `/admin/clients` and Business Owner on `/clients`.
- The page uses a collapsible Business Clients management panel.
- The panel has a Create button and a collapse toggle.
- The table shows Status, Name, Client Type, Service Package, Email, Phone, and Action.
- System Administrator table rows also show Company.
- Row actions are icon-only edit and delete/archive controls.
- The editor is inline and uses the existing form-grid pattern.
- The editor fields are Service client for System Administrators, Business client type, Service package, Business client name, Primary contact, Email, Phone, Service address, Rate override, Access notes, and Active.
- Success and validation errors are shown inline.

## Data Model Impact

- `CompanyClient` stores id, company id, display name, primary contact name, email, phone, service address, access notes, client type id, optional rate override, active flag, and optional service package id.
- `ClientType` stores id, company id, name, billing frequency, default rate, and active status.
- `BusinessClientManagementRow` combines service client company, business client, and optional client type for System Administrator views.
- `CompanyMembership.CompanyClientId` links approved client-user access to a specific business client record.
- Azure Table storage persists business clients in the `CompanyClients` table.

## Authorization Rules

- `/admin/clients` operations require System Administrator access.
- `/clients` operations require active `CompanyAdmin` membership for the current company.
- Business Employees, Business Clients, and Independent Home Owners do not receive business-client management access.
- Business Owners are scoped to their own service client company.
- Business client service package assignment must reference an active package from the business client's service client or the matching global catalog scope.
- Business client registration can only select active business clients.

## Acceptance Criteria

- Implemented: System Administrators can open Business Clients at `/admin/clients`.
- Implemented: Business Owners can open Business Clients at `/clients`.
- Implemented: System Administrator rows are filtered by current app mode and show the Company column.
- Implemented: Business Owner rows are scoped to the current service client.
- Implemented: Business clients can be created and edited from an inline editor.
- Implemented: Business clients can be archived and reactivated by toggling active state.
- Implemented: The delete/archive icon sets the business client inactive.
- Implemented: Client type choices include the active Home Owner client type.
- Implemented: Service package choices combine active global and selected-service-client packages.
- Implemented: Business client save validates required fields, active client type, and service package.
- Implemented: Business client save normalizes id, email, phone, service address, access notes, client type id, and empty service package values.
- Implemented: Business Client registration requires selecting an active business client address and stores the selected business client id on the membership.
- Not implemented: Full client detail page with billing, visits, service history, messages, and linked client users.
- Not implemented: Billing address, property notes, preferred service days, taxable flag, notification preference, geocoding, and map fields in the current `CompanyClient` model.

## Tests

- `AuthorizationTests.Company_admin_can_create_customer`
- `AuthorizationTests.Company_admin_can_assign_service_package_to_business_client`
- `AuthorizationTests.Client_portal_uses_current_client_user_membership`
- `OnboardingTests.Business_client_registration_requires_and_stores_selected_client_address`
- `AuthorizationTests.Business_client_dashboard_can_read_assigned_service_package`

## User Documentation Impact

- Created `docs/user-guide/business-client-management.md`.
- Updated `docs/user-guide/index.md` with a Business Client Management link.

## Current Implementation

- `ClientsPage.razor` implements `/clients` and `/admin/clients`.
- The System Administrator route loads service clients, company types, business-client rows, client types, and service packages through `PlatformAdminService` and `CompanyAdminService`.
- The Business Owner route resolves the current company using `CurrentCompanyContext` and requires active `CompanyAdmin` membership.
- `PlatformAdminService.GetBusinessClientManagementRowsAsync`, `GetClientTypesForCompanyAsync`, and `UpsertBusinessClientAsync` support System Administrator reads and writes.
- `CompanyAdminService.GetClientsAsync`, `GetClientTypesAsync`, and `UpsertClientAsync` support Business Owner reads and writes.
- `BusinessClientTypeReferenceData.EnsureForCompanyAsync` ensures the Home Owner client type exists and is active for a service client.
- Business client access for client users is linked through `CompanyMembership.CompanyClientId` and used by client-portal services.

## Outstanding Tasks

- Add browser/component coverage for `/clients` and `/admin/clients`.
- Add direct tests for System Administrator business-client creation/edit/archive behavior.
- Add full client detail page when visits, billing, messages, and client-user links are ready to be managed from one client workspace.
- Add richer client fields if still required: billing address, property notes, preferred service days, taxable flag, notification settings, geocoding, and coordinates.
- Decide whether client type management needs its own focused feature/page beyond the seeded Home Owner type.
- Align delete/archive labels with the stored inactive behavior.

## Change Log

- 2026-08-05: Documented implemented business client management behavior from code, tests, UI pages, storage, and registration/client-portal links.
