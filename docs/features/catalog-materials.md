# Catalog Materials

Status: Implemented
Owner: Product
Last reviewed: 2026-08-05

## Problem

Service businesses need a focused material catalog so business owners can define billable materials used during visits, while system administrators can maintain starter material records for the current service type.

## Personas

- System Administrator
- Business Owner
- Business Employee

## Requirements

- System Administrators manage the current app-mode starter material catalog at `/admin/catalog/materials`.
- Business Owners manage their company material items at `/catalog/materials`.
- Business Owners can add active global starter materials into their company catalog.
- Material rows support manufacturer, name, category, model number, unit, unit cost, billable price, taxable status, active status, and description.
- Material categories group material rows and inactive or legacy rows remain visible.
- CSV seeding is available only to System Administrators.

## User Flows

### System Admin Seeds Starter Materials

1. A System Administrator opens `/admin/catalog/materials`.
2. The page loads the current app-mode global catalog scope.
3. The user uploads a CSV with `Brand`, `Category`, `Name`, and `Model No` columns.
4. The system creates or updates material categories and material rows.

### Business Owner Adds A Starter Material

1. A Business Owner opens `/catalog/materials`.
2. The page loads company materials and available global materials.
3. The user searches Add Materials and chooses a starter row.
4. The system copies the starter material and any missing category into the company scope.

### Business Owner Manages A Material

1. The user chooses Create or edit.
2. The inline Material editor opens.
3. The user updates details and saves.
4. The table reloads with the saved material.

## UI Expectations

- The page heading is Materials.
- The page uses focused collapsible panels and does not show service, equipment, company, user, role, email-log, or dashboard controls.
- System Administrator view shows Seed Materials, Material Categories, and Materials panels.
- Business Owner view shows Materials and Add Materials panels.
- Tables support filtering and sortable column headers.
- Material actions are icon-only edit and archive controls.
- Category and material editors open inline within their management panels.

## Data Model Impact

- `MaterialCategory` stores id, company id, name, description, system-managed flag, and active flag.
- `Material` stores id, company id, optional category id, name, unit of measure, unit cost, billable price, taxable flag, active flag, manufacturer, model number, and description.
- Current app-mode starter records use `GlobalCatalogScope.Pool` or `GlobalCatalogScope.Landscape` as the catalog company id.
- Company material records remain scoped to their service client company id.

## Authorization Rules

- System Administrator access is required to manage the global starter catalog.
- Active `CompanyAdmin` membership is required to open `/catalog/materials` and manage company materials.
- `CompanyUser` users can read materials through visit workflows but cannot manage catalog definitions.
- Service-layer validation rejects invalid category references, negative material prices, missing material ids, missing names, and missing units of measure.

## Acceptance Criteria

- [x] `/admin/catalog/materials` is a focused material catalog page for System Administrators.
- [x] `/catalog/materials` is a focused material catalog page for Business Owners.
- [x] System Administrators can seed starter material categories and rows from CSV.
- [x] System Administrators can create, edit, archive, and reactivate global material categories and material items.
- [x] Business Owners can create, edit, archive, and reactivate company material items.
- [x] Business Owners can add global starter materials to the company scope without changing the global source row.
- [x] Materials are grouped by category with an Uncategorized fallback.
- [x] Material rows can be filtered and sorted.
- [ ] Business Owners cannot currently create or edit material categories directly from the UI; categories are added implicitly when selecting a global starter material.
- [ ] The UI does not expose explicit copy-as-custom buttons for material rows; it exposes an Add Materials chooser that inserts starter rows into the company scope.

## Tests

- `AuthorizationTests.Company_admin_can_create_and_archive_catalog_items`
- `AuthorizationTests.Company_admin_can_seed_materials_from_catalog_rows`
- `AuthorizationTests.System_admin_can_seed_global_materials_from_catalog_rows`
- `AuthorizationTests.Global_material_and_service_seed_catalogs_are_service_specific`
- `AuthorizationTests.Company_admin_can_copy_starter_catalog_items_to_custom_records`
- `FieldWorkTests.Catalog_overview_groups_services_and_materials_by_category`
- `AzureTableKeyTests.Global_material_partition_uses_material_catalog_scope`
- `AzureTableKeyTests.Landscape_global_material_partition_uses_material_catalog_scope`
- `AzureTableKeyTests.Company_material_partition_keeps_company_scope`

## User Documentation Impact

- User-facing behavior is documented in [Catalog Materials](../user-guide/catalog-materials.md).

## Current Implementation

- Implemented by `CatalogMaterialsPage.razor` and `CompanyAdminService`.
- The route `/admin/catalog/materials` sets the demo current user to `sys-admin` and uses the current app-mode global catalog company id.
- The route `/catalog/materials` resolves the current company from active `CompanyAdmin` membership.
- System Administrator view can seed CSV rows, manage material categories, and manage materials.
- Business Owner view can manage material rows and choose global starter materials from a searchable Add Materials panel.
- Saving a company material with a global category copies that category into the company scope first.
- Material ids are generated from manufacturer, name, and model number for new UI rows and normalized by the service layer.
- Deleting a material or category from the focused UI archives it by setting `IsActive = false`.

## Outstanding Tasks

- Decide whether Business Owners should have first-class category create/edit controls or only receive categories from starter material selection.
- Decide whether the Add Materials chooser is the desired final copy workflow or whether explicit copy-as-custom row actions should be added.
- Add browser/component coverage for the Blazor page interactions.

## Change Log

- 2026-08-05: Documented the implemented material catalog behavior, source scopes, tests, and remaining UI gaps.
