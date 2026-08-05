# Pool Equipment Catalog

Status: Implemented
Owner: Product
Last reviewed: 2026-08-05

## Problem

Pool service users need a structured equipment catalog so system administrators can maintain starter equipment. Pool-specific configured equipment is documented separately in [Pool Configuration](pool-configuration.md).

## Personas

- System Administrator
- Business Owner
- Independent Home Owner
- Business Client

## Requirements

- System Administrators manage the global pool-equipment catalog at `/admin/catalog/poolequipment`.
- Pool configuration users manage selected client or homeowner equipment at `/poolequipment`; see [Pool Configuration](pool-configuration.md).
- Pool equipment routes are available only in Pool mode.
- Equipment records support manufacturer, category, name, model number, image URL, description, comment for pool configuration rows, and active status.
- System Administrators can seed global equipment from CSV.
- Pool configuration users can add global starter equipment into the selected homeowner/client scope.
- Pool configuration users can upload equipment pictures.

## User Flows

### System Admin Manages Global Equipment

1. A System Administrator opens `/admin/catalog/poolequipment`.
2. The page loads global equipment categories and items.
3. The user seeds CSV rows or creates, edits, archives, reactivates, or deletes equipment records.

### Business Owner Manages A Client Pool Configuration

1. A Business Owner opens `/poolequipment`.
2. The system loads pool configuration clients for the owner company.
3. The user chooses a client.
4. The page loads homeowner-scoped equipment, global starter equipment, and equipment pictures for the selected client.
5. The user adds starter equipment, creates custom equipment, edits comments/details, deletes rows, or uploads pictures.

### Independent Home Owner Manages Own Pool Configuration

1. An Independent Home Owner opens `/poolequipment`.
2. The system uses the current user id as the owner scope.
3. The homeowner adds starter equipment, creates custom equipment, edits comments/details, deletes rows, or uploads pictures.

## UI Expectations

- The global catalog page heading is Pool Equipment.
- The pool configuration page heading is Pool Configuration.
- The page uses focused collapsible management panels and does not show service, material, company, user, role, email-log, or dashboard controls.
- System Administrator view shows Seed Equipment, Equipment Categories, and Equipment panels.
- Pool Configuration view shows Choose Client when applicable, Configuration, Add Pool Equipment, and Equipment Pictures panels.
- Tables support filtering and sortable column headers.
- Row actions are icon-only edit, description/info, and delete/archive controls where applicable.

## Data Model Impact

- `PoolEquipmentCategory` stores id, scope, scope owner id, manufacturer, name, description, system-managed flag, and active flag.
- `PoolEquipmentItem` stores id, scope, scope owner id, category id, name, description, image URL, active flag, model number, manufacturer, and comment.
- `HomeOwnerPoolEquipmentPhoto` stores uploaded image metadata and a data URL payload.
- Equipment scopes are `Global`, `Company`, and `HomeOwner`; current UI uses `Global` for `/admin/catalog/poolequipment` and `HomeOwner` for `/poolequipment`.

## Authorization Rules

- System Administrator access is required to manage global equipment.
- Pool Equipment routes redirect to the dashboard when the app is not in Pool mode.
- `/catalog/poolequipment` currently redirects to the dashboard.
- Business Owners can manage homeowner-scoped pool configuration records for clients in their company.
- Independent Home Owners can manage only their own homeowner-scoped equipment.
- System Administrators can manage pool configurations for available clients.
- Service-layer validation rejects missing scope owner ids, missing item ids, missing item names, and invalid category references.

## Acceptance Criteria

- [x] `/admin/catalog/poolequipment` is a focused global pool-equipment catalog page for System Administrators.
- [x] `/poolequipment` is a focused Pool Configuration page for System Administrators, Business Owners, and Independent Home Owners in Pool mode.
- [x] Landscape mode hides Pool Equipment navigation and redirects direct equipment routes to the dashboard.
- [x] System Administrators can seed global equipment categories and items from CSV.
- [x] System Administrators can create, edit, archive/reactivate categories, and create, edit, delete, archive/reactivate global equipment items.
- [x] Pool configuration users can add global starter equipment into the selected homeowner scope.
- [x] Pool configuration users can create, edit, comment on, delete, and filter configured equipment.
- [x] Pool configuration users can upload, view, zoom, and delete equipment pictures.
- [x] Equipment is grouped by category with an Uncategorized fallback.
- [ ] Company-scoped equipment management exists in the service layer and tests but `/catalog/poolequipment` currently redirects to the dashboard, so there is no company-scoped catalog UI.
- [ ] Equipment item delete removes rows, while category delete archives by setting inactive.

## Tests

- `AuthorizationTests.System_admin_can_create_and_archive_global_equipment`
- `AuthorizationTests.System_admin_can_seed_global_equipment_from_catalog_rows`
- `AuthorizationTests.Company_admin_can_create_and_archive_company_equipment`
- `AuthorizationTests.Company_admin_can_copy_starter_equipment_to_custom_records`
- `AuthorizationTests.Homeowner_can_manage_only_own_equipment`
- `AuthorizationTests.Seed_data_includes_requested_test_companies_catalogs_and_users`
- `OnboardingTests` coverage for Independent Home Owner registration seeding owner-scoped equipment

## User Documentation Impact

- User-facing behavior is documented in [Pool Equipment Catalog](../user-guide/pool-equipment-catalog.md).

## Current Implementation

- Implemented by `PoolEquipmentPage.razor`, `CompanyAdminService`, `PlatformAdminService`, and `OnboardingService`.
- `/admin/catalog/poolequipment` manages global equipment scope owner `global`.
- `/poolequipment` manages homeowner-scoped pool configuration records for the current Independent Home Owner or a selected company client.
- `/catalog/poolequipment` exists as a route but redirects to the dashboard before loading company-scoped equipment.
- Pool configuration category choices combine existing scoped categories with global categories and copy missing global categories into the selected homeowner scope before saving selected equipment.
- Adding global equipment creates a new homeowner-scoped row with a GUID-suffixed id so multiple configured copies can exist.
- Equipment pictures are stored as `HomeOwnerPoolEquipmentPhoto` rows with data URLs.

## Outstanding Tasks

- Decide whether company-scoped equipment catalog management should be implemented, removed from product wording, or kept as service-layer-only support.
- Align equipment item delete behavior with archive/reactivate language or update product language to call it delete.
- Add browser/component coverage for global catalog and pool configuration interactions.
- Consider moving uploaded image payloads from table data URLs to blob storage when production image volume grows.

## Change Log

- 2026-08-05: Documented implemented global equipment catalog and pool configuration behavior, route differences, tests, and remaining gaps.
