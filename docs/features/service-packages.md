# Service Packages

Status: Implemented
Owner: Product
Last reviewed: 2026-08-05

## Problem

Service businesses need reusable service packages that define the recurring service bundle a service client or business client receives, including package cost and the services included on every visit or every N visits.

## Personas

- System Administrator
- Business Owner
- Business Client

## Requirements

- System Administrators manage current app-mode global service packages at `/admin/catalog/servicepackages`.
- Business Owners manage company-scoped service packages at `/catalog/servicepackages`.
- System Administrator packages can use services from the current global service catalog.
- Business Owner packages can use active services from their company catalog and the current app-mode global service catalog.
- A service package has name, recurrence, description, cost, active status, and chosen services.
- Package recurrence supports Weekly, Bi-Weekly, Monthly, Bi-Monthly, Half-Yearly, and Yearly.
- Each chosen service supports Every Visit or Every X Visits recurrence.
- Service packages can be assigned to service clients and business clients by other management pages.
- Business Clients can view their assigned service package on the dashboard.

## User Flows

### System Admin Manages Global Packages

1. A System Administrator opens `/admin/catalog/servicepackages`.
2. The page resolves the current app-mode global catalog scope.
3. The user creates, edits, filters, sorts, activates, deactivates, or deletes global packages.
4. The package service chooser lists active global services.

### Business Owner Manages Company Packages

1. A Business Owner opens `/catalog/servicepackages`.
2. The page resolves the active `CompanyAdmin` service client.
3. The user creates, edits, filters, sorts, activates, deactivates, or deletes company packages.
4. The package service chooser lists active company services and active global starter services.

### Choose Services For A Package

1. The user opens the package editor.
2. The user filters available services by name, category, scope, or description.
3. The user sets a service recurrence and chooses the service.
4. The chosen service appears in Chosen Services, where its recurrence can be edited or the service can be removed.
5. The user saves the package.

### Business Client Views Package

1. A Business Client opens the dashboard.
2. The dashboard loads the client-level service package, falling back to the service client's default package.
3. The dashboard shows package name, recurrence, cost, description, and included services.

## UI Expectations

- The page heading is Service Packages.
- The page eyebrow is System Administrator on `/admin/catalog/servicepackages` and Company Admin on `/catalog/servicepackages`.
- The page uses one Manage Service Packages panel with Create and collapse controls.
- The package table shows Active, Name, Recurrence, Description, Cost, Services, and Action.
- Package rows support filtering and sortable columns for active state, name, recurrence, description, and cost.
- The inline editor shows Name, Recurrence, Cost, Active, Description, Chosen Services, Save/Cancel, and Choose Services.
- Chosen Services and Choose Services tables show Service, Category, Scope, Recurrence, and Action.
- Row actions are icon-only edit, choose, remove, and delete controls.

## Data Model Impact

- `ServicePackage` stores id, company id, name, recurrence, description, cost, active flag, and package services.
- `ServicePackageService` stores service id and service recurrence.
- Global starter packages use app-mode global catalog company ids such as `Pool_Global` and `LandScape_Global`.
- Company packages use the owning service client company id.
- `Company.ServicePackageId` stores the service client's default package.
- `CompanyClient.ServicePackageId` stores a business-client package override.

## Authorization Rules

- System Administrator access is required to manage global packages.
- Active `CompanyAdmin` membership is required to manage company packages.
- Package reads use catalog read authorization for the package company id.
- Package writes use catalog management authorization for the package company id.
- The service layer rejects missing package ids, missing names, missing package recurrence, invalid recurrence, negative cost, invalid service recurrence, and chosen services outside the accessible service scopes.
- Business Client dashboard package reads are scoped to the signed-in user's linked business client.

## Acceptance Criteria

- [x] `/admin/catalog/servicepackages` provides a focused global package editor for System Administrators.
- [x] `/catalog/servicepackages` provides a focused company package editor for Business Owners.
- [x] System Administrator package choices come from the current app-mode global service catalog.
- [x] Business Owner package choices come from active company services plus active current app-mode global services.
- [x] Packages can be created, edited, activated, deactivated, filtered, sorted, and deleted.
- [x] Package recurrence is limited to Weekly, Bi-Weekly, Monthly, Bi-Monthly, Half-Yearly, and Yearly.
- [x] Chosen services can be set to Every Visit or Every X Visits.
- [x] Package services are normalized and duplicate chosen service ids collapse to one entry.
- [x] Service package assignment is supported from service-client and business-client management.
- [x] Business Clients can view the assigned package from the dashboard.
- [ ] Package delete removes the package row rather than archiving it.
- [ ] The UI does not currently show whether package services point to a global or company service after saving beyond the chosen service display text.

## Tests

- `AuthorizationTests.Company_admin_can_manage_company_service_packages`
- `AuthorizationTests.Global_service_packages_are_service_specific`
- `AuthorizationTests.System_admin_can_assign_service_package_to_service_client`
- `AuthorizationTests.Company_admin_can_assign_service_package_to_business_client`
- `AuthorizationTests.Business_client_dashboard_can_read_assigned_service_package`
- `AzureTableKeyTests.Global_service_package_partition_uses_service_catalog_scope`
- `AzureTableKeyTests.Company_service_package_partition_uses_company_scope`

## User Documentation Impact

- User-facing behavior is documented in [Service Packages](../user-guide/service-packages.md).
- Service-client and business-client guides already describe package assignment touchpoints.

## Current Implementation

- Implemented by `ServicePackagesPage.razor` and `CompanyAdminService`.
- `/admin/catalog/servicepackages` sets the demo current user to `sys-admin` and uses `ApplicationModeSnapshot.GlobalCatalogCompanyId` as the package company id.
- `/catalog/servicepackages` resolves the active company through `CurrentCompany.GetRequiredCompanyIdAsync([CompanyRole.CompanyAdmin])`.
- System Administrator pages load only the global catalog for available services.
- Business Owner pages load both the company catalog and global catalog, de-duplicating available service choices by service id.
- New package ids are generated from package name in the UI and normalized by the service layer.
- Package services are stored as service id plus recurrence.
- `UpsertServicePackageAsync` validates package fields and ensures all chosen service ids exist in the supplied accessible service company ids.
- `SetServicePackageActiveAsync` toggles active status.
- `DeleteServicePackageAsync` physically deletes package rows.
- Service client and business client assignment flows validate active packages against the service client's company scope or current app-mode global scope.
- Business Client dashboard reads the business-client package, falling back to the service-client default package.

## Outstanding Tasks

- Add browser/component tests for the package editor, service chooser, recurrence controls, filters, and sorting.
- Decide whether package deletion should be changed to archive-only behavior for consistency with other management pages.
- Add explicit validation or user messaging for packages with no chosen services if the product requires at least one service.
- Consider displaying package service scope after save in a more durable way if duplicate global/company service ids become common.

## Change Log

- 2026-08-05: Created the implemented service-packages feature spec from code, tests, storage models, and source-of-truth docs.
