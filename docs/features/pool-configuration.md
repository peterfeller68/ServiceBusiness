# Pool Configuration

Status: Implemented
Owner: Product
Last reviewed: 2026-08-05

## Problem

Pool service businesses, service clients, and independent homeowners need a practical way to record the equipment installed at a specific pool, including comments and photos, without modifying the global starter equipment catalog.

## Personas

- System Administrator
- Business Owner
- Business Client
- Independent Home Owner

## Requirements

- Pool Configuration is available in Pool mode at `/poolequipment`.
- Landscape mode hides Pool Configuration navigation and redirects direct pool-equipment routes to the dashboard.
- System Administrators can choose from active business-client pools in the current Pool service mode and independent homeowner profiles.
- Business Owners can choose from active business clients in their service client company.
- Independent Home Owners manage their own pool configuration directly.
- Pool configuration equipment is stored in `EquipmentScope.HomeOwner` using the selected business-client id or independent homeowner user id as the scope owner.
- Users can search existing configured equipment, add global starter equipment, create custom configured equipment, edit configured equipment, edit comments, view descriptions, delete configured equipment, and upload/delete equipment pictures.
- Business Clients can view their pool configuration from the dashboard.

## User Flows

### System Admin Chooses A Pool Configuration

1. A System Administrator opens `/poolequipment`.
2. The page loads pool configuration clients from active Pool service clients and independent homeowner profiles.
3. The user filters or chooses a client.
4. The page loads the selected scope's configured equipment, global starter equipment, and uploaded pictures.

### Business Owner Chooses A Pool Configuration

1. A Business Owner opens `/poolequipment`.
2. The page loads active business clients for the owner's service client company.
3. The first available client is selected by default.
4. The owner can choose another client and manage that client's configured equipment.

### Independent Home Owner Manages Own Pool

1. An Independent Home Owner opens `/poolequipment`.
2. The system uses the current user id as the scope owner.
3. The homeowner manages configured equipment and equipment pictures.

### Add Starter Equipment

1. The user searches global equipment in Add Pool Equipment.
2. The user chooses an equipment row.
3. The system copies any missing global category into the selected owner scope.
4. The system creates a configured equipment item with a GUID-suffixed id and clears the comment.

### Upload Equipment Pictures

1. The user selects one or more image files.
2. The system stores each image as a homeowner equipment photo data URL.
3. The picture list refreshes and pictures can be opened larger or deleted.

## UI Expectations

- The page title and heading are Pool Configuration when the route is `/poolequipment`.
- The page eyebrow is Pool Configuration.
- System Administrators and Business Owners see a Choose Client panel.
- Independent Home Owners do not see the Choose Client panel.
- Configuration, Add Pool Equipment, and Equipment Pictures are separate collapsible management panels.
- The Configuration table shows manufacturer, name, category, model number, comment, and actions.
- The Add Pool Equipment panel includes a Create button, inline equipment editor, global equipment filter, and global starter equipment chooser.
- The Equipment Pictures panel supports multiple image upload, thumbnail display, larger image modal, and delete.
- Description details open in a modal from the row info action.

## Data Model Impact

- `PoolEquipmentCategory` stores categories using `EquipmentScope.HomeOwner` and the selected scope owner id for pool configuration rows.
- `PoolEquipmentItem` stores configured equipment using `EquipmentScope.HomeOwner`, selected scope owner id, category id, manufacturer, name, model number, image URL, description, active flag, and comment.
- `HomeOwnerPoolEquipmentPhoto` stores uploaded picture id, file name, content type, data URL, and uploaded timestamp.
- `PoolConfigurationClientRow` presents selectable scope owners with company name, client address, and client type.
- Business-client pool configurations use the `CompanyClient.Id` as the homeowner equipment scope owner.
- Independent Home Owner pool configurations use the homeowner `AppUser.Id` as the scope owner.

## Authorization Rules

- All pool-configuration management requires Pool mode.
- System Administrators can manage any selected homeowner-scoped pool configuration.
- Business Owners can manage homeowner-scoped pool configurations for business clients owned by their active `CompanyAdmin` service client.
- Independent Home Owners can manage only their own homeowner-scoped configuration.
- Other authenticated users are redirected to the dashboard from `/poolequipment`.
- Business Clients can read their linked business client's pool configuration from the dashboard but do not manage it there.
- The service layer rejects missing scope owner ids, missing item ids, missing item names, invalid category references, and unauthorized homeowner-scope access.

## Acceptance Criteria

- [x] `/poolequipment` opens Pool Configuration in Pool mode.
- [x] Direct pool-equipment routes redirect to the dashboard in Landscape mode.
- [x] System Administrators can choose active Pool business-client addresses and independent homeowner profiles.
- [x] Business Owners can choose active business clients in their company.
- [x] Independent Home Owners manage their own pool configuration without choosing a client.
- [x] Configured equipment is grouped by category with an Uncategorized fallback.
- [x] Users can filter and sort configured equipment.
- [x] Users can add global starter equipment into the selected pool configuration.
- [x] Users can create, edit, comment on, view descriptions for, and delete configured equipment.
- [x] Users can upload, view, zoom, and delete pool equipment pictures.
- [x] Business Clients can view their linked pool configuration from the dashboard.
- [ ] Pool Configuration item delete removes the configured equipment row rather than archiving it.
- [ ] Uploaded pictures are stored as table-backed data URLs rather than external blob objects.

## Tests

- `AuthorizationTests.Homeowner_can_manage_only_own_equipment`
- `AuthorizationTests.Seed_data_includes_requested_test_companies_catalogs_and_users`
- `OnboardingTests.Independent_homeowner_registration_creates_active_owner_workspace_without_company_membership`
- `RegistrationBrowserScenarioTests` checks Independent Home Owner dashboard pool equipment visibility.
- `ClientPortalService.GetCurrentUserPoolEquipmentOverviewAsync` is exercised by client-dashboard code paths, but dedicated business-client pool-configuration read tests are still missing.

## User Documentation Impact

- User-facing behavior is documented in [Pool Configuration](../user-guide/pool-configuration.md).
- Related global starter catalog behavior remains documented in [Pool Equipment Catalog](pool-equipment-catalog.md).

## Current Implementation

- Implemented by the `/poolequipment` branch of `PoolEquipmentPage.razor`.
- The page uses `ApplicationModeService` to redirect away from pool equipment when the current mode is not Pool.
- System Administrator pool-configuration clients come from `PlatformAdminService.GetPoolConfigurationClientsAsync`, filtered to active service clients matching Pool mode plus independent homeowner profiles.
- Business Owner pool-configuration clients come from `CompanyAdminService.GetPoolConfigurationClientsAsync(companyId)`.
- Independent Home Owner users get `selectedPoolConfigurationScopeOwnerId = CurrentUser.UserId`.
- `CompanyAdminService.GetPoolEquipmentOverviewAsync(EquipmentScope.HomeOwner, scopeOwnerId)` loads configured equipment.
- `CompanyAdminService.GetPoolEquipmentOverviewAsync(EquipmentScope.Global, "global")` loads starter equipment for the Add Pool Equipment panel.
- `EnsurePoolConfigurationCategoryAsync` copies a missing global category into the selected owner scope before a starter or custom configured item is saved.
- `AddGlobalEquipmentItemAsync` copies a starter item into the selected owner scope with a GUID-suffixed id and blank comment.
- `AddPoolConfigurationPhotosAsync` and `DeletePoolConfigurationPhotoAsync` manage uploaded equipment pictures for the selected scope owner.
- Business Client dashboard display uses `ClientPortalService.GetCurrentUserPoolEquipmentOverviewAsync`.

## Outstanding Tasks

- Add dedicated tests for System Administrator and Business Owner pool-configuration client selection and photo management.
- Add a dedicated test for Business Client read-only pool configuration visibility.
- Decide whether configured equipment should support archive/reactivate instead of hard delete.
- Move image payload storage from data URLs in table records to blob storage if production image volume grows.
- Consider renaming Independent Home Owner dashboard link text from Pool Equipment to Pool Configuration for consistency.

## Change Log

- 2026-08-05: Created the implemented pool-configuration feature spec from code, tests, storage models, and source-of-truth docs.
