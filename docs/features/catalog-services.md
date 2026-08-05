# Catalog Services

Status: Implemented
Owner: Product
Last reviewed: 2026-08-05

## Problem

Service businesses need a focused service catalog so owners can define visit services, prices, duration defaults, and taxable settings while system administrators maintain starter services for the current service type.

## Personas

- System Administrator
- Business Owner
- Independent Home Owner
- Business Employee

## Requirements

- System Administrators manage the current app-mode starter service catalog at `/admin/catalog/services`.
- Business Owners manage company service items at `/catalog/services`.
- Independent Home Owners manage owner-scoped services at `/settings/services`.
- Business Owners and Independent Home Owners can add global starter services into their own scope.
- Service rows support category, name, description, duration, default price, taxable status, and active status.
- CSV seeding is available only to System Administrators.

## User Flows

### System Admin Seeds Starter Services

1. A System Administrator opens `/admin/catalog/services`.
2. The page loads the current app-mode global catalog scope.
3. The user uploads a CSV with `Category`, `Name`, and `Description` columns.
4. The system creates or updates service categories and service rows.

### Business Owner Adds A Starter Service

1. A Business Owner opens `/catalog/services`.
2. The page loads company services and available global services.
3. The user searches Add Services and chooses a starter row.
4. The system copies the starter service and any missing category into the company scope.

### Independent Home Owner Manages Services

1. An Independent Home Owner opens `/settings/services`.
2. The system confirms the user has no company memberships.
3. The page displays owner-scoped services with name, category, description, and active status.
4. The homeowner can create, edit, delete, or add starter services.

## UI Expectations

- The page heading is Services.
- The page uses focused collapsible panels and does not show material, equipment, company, user, role, email-log, or dashboard controls.
- System Administrator view shows Seed Services, Service Categories, and Services panels.
- Business Owner and Independent Home Owner views show Services and Add Services panels.
- Business Owner tables show duration, price, and taxable columns; Independent Home Owner tables hide those billing columns.
- Tables support filtering and sortable column headers.
- Service actions are icon-only edit and delete controls.

## Data Model Impact

- `ServiceCategory` stores id, company id, name, description, system-managed flag, and active flag.
- `ServiceOffering` stores id, company id, optional category id, name, description, default duration minutes, default price, taxable flag, and active flag.
- Current app-mode starter records use `GlobalCatalogScope.Pool` or `GlobalCatalogScope.Landscape` as the catalog company id.
- Independent Home Owner services use the homeowner user id as the service `CompanyId`.

## Authorization Rules

- System Administrator access is required to manage the global starter catalog.
- Active `CompanyAdmin` membership is required to open `/catalog/services` and manage company services.
- `/settings/services` redirects to the dashboard unless the current user is an Independent Home Owner with no company memberships.
- `CompanyUser` users can read services through visit workflows but cannot manage catalog definitions.
- Service-layer validation rejects invalid category references, missing service ids, missing names, non-positive durations, and negative prices.

## Acceptance Criteria

- [x] `/admin/catalog/services` is a focused service catalog page for System Administrators.
- [x] `/catalog/services` is a focused service catalog page for Business Owners.
- [x] `/settings/services` is a focused owner-scoped service page for Independent Home Owners.
- [x] System Administrators can seed starter service categories and rows from CSV.
- [x] System Administrators can create, edit, archive, and reactivate global service categories and service rows.
- [x] Business Owners can create, edit, delete, and filter company service rows.
- [x] Independent Home Owners can manage owner-scoped service rows without billing columns in the table/editor.
- [x] Business Owners and Independent Home Owners can add global starter services into their own scope without changing the global source row.
- [x] Services are grouped by category with an Uncategorized fallback.
- [ ] Business Owners cannot currently create or edit service categories directly from the UI; categories are added implicitly when selecting a global starter service.
- [ ] The UI delete action removes service rows instead of archiving them, while category delete archives by setting inactive.

## Tests

- `AuthorizationTests.Company_admin_can_create_and_archive_catalog_items`
- `AuthorizationTests.Company_admin_can_seed_services_from_catalog_rows`
- `AuthorizationTests.Global_material_and_service_seed_catalogs_are_service_specific`
- `AuthorizationTests.Company_admin_can_copy_starter_catalog_items_to_custom_records`
- `CatalogCustomizationScenarioTests.Company_admin_copies_starter_service_customizes_it_and_original_stays_unchanged`
- `FieldWorkTests.Catalog_overview_groups_services_and_materials_by_category`
- `AzureTableKeyTests.Global_service_partition_uses_service_catalog_scope`
- `AzureTableKeyTests.Landscape_global_service_partition_uses_service_catalog_scope`
- `AzureTableKeyTests.Company_service_partition_uses_service_company_scope`

## User Documentation Impact

- User-facing behavior is documented in [Catalog Services](../user-guide/catalog-services.md).

## Current Implementation

- Implemented by `CatalogServicesPage.razor`, `CompanyAdminService`, and `OnboardingService`.
- The route `/admin/catalog/services` sets the demo current user to `sys-admin` and uses the current app-mode global catalog company id.
- The route `/catalog/services` resolves the current company from active `CompanyAdmin` membership.
- The route `/settings/services` uses the current user id as the owner-scoped catalog id after confirming the user is an Independent Home Owner.
- System Administrator view can seed CSV rows, manage service categories, and manage services.
- Business Owner and Independent Home Owner views can manage service rows and choose global starter services from a searchable Add Services panel.
- Saving a scoped service with a global category copies that category into the current scope first.
- Service ids are generated from category and service name for new UI rows and normalized by the service layer.

## Outstanding Tasks

- Decide whether Business Owners and Independent Home Owners should have first-class service category create/edit controls.
- Align the service row delete behavior with archive/reactivate language or update product language to call it delete.
- Add browser/component coverage for the Blazor page interactions.

## Change Log

- 2026-08-05: Documented the implemented service catalog behavior, owner-scoped services, tests, and remaining UI gaps.
