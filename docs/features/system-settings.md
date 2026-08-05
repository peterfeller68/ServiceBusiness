# System Settings

Status: Implemented
Owner: Product
Last reviewed: 2026-08-05

## Problem

Deployments need a stable way to run the application as PoolShark or TreeShark and to enable or disable development test-user sign-in without code changes.

## Personas

- System Administrator
- Developer/Operator

## Requirements

- `SystemSettings.SystemMode` supports `Pool` and `Landscape`.
- Pool mode uses PoolShark branding, the pool hero image, the Pool global catalog scope, and Pool Equipment/Pool Configuration visibility.
- Landscape mode uses TreeShark branding, the landscape hero image, the Landscape global catalog scope, and hides/redirects pool-equipment workflows.
- `SystemSettings.DevTest` controls test sign-in and Test navigation visibility.
- Configuration values provide startup defaults when persisted settings are absent.
- The `/settings` page shows SystemMode, DevTest, and active product name to System Administrators as read-only deployment guidance.

## User Flows

### Operator Configures Defaults

1. An operator sets `SystemSettings__SystemMode` and `SystemSettings__DevTest` in app settings or configuration.
2. The app starts with those defaults if the singleton system-settings row is missing.

### System Admin Views Settings

1. A System Administrator opens `/settings`.
2. The page shows configured defaults and the currently resolved product mode.
3. The page explains the Azure App Service setting names.

## UI Expectations

- `/settings` heading is General settings.
- System Administrators see a System Settings panel with SystemMode, DevTest, and Product metrics.
- Non-system-admin users see only generic available-settings guidance.
- Settings are not edited from the current UI.

## Data Model Impact

- `SystemSettings` stores `SystemMode` and `DevTest`.
- Azure Table storage stores the singleton row with partition `SYSTEM_SETTINGS` and row `CURRENT`.
- `ApplicationModeSnapshot` derives product name, hero image, pool-mode flag, global catalog company id, and DevTest flag.

## Authorization Rules

- The read-only `/settings` route is not linked as a role menu leaf.
- The System Settings panel is visible only to System Administrators.
- Service-layer update methods require System Administrator access.
- DevTest endpoints require `SystemSettings:DevTest`/configured DevTest to be true and the user to be a test user.

## Acceptance Criteria

- [x] Pool mode resolves PoolShark, pool hero image, Pool global catalog scope, and pool equipment visibility.
- [x] Landscape mode resolves TreeShark, landscape hero image, Landscape global catalog scope, and hides pool equipment.
- [x] DevTest defaults to disabled unless configured true.
- [x] `/settings` displays read-only SystemMode, DevTest, and Product for System Administrators.
- [x] Test sign-in is blocked when DevTest is disabled.
- [ ] The current UI does not provide a SystemMode editor despite service-layer update methods.
- [ ] Application mode service currently reads configured defaults for the web UI; source docs should not imply a visible save flow.

## Tests

- `ApplicationModeTests.Pool_mode_uses_pool_branding_and_pool_equipment`
- `ApplicationModeTests.Landscape_mode_uses_landscape_branding_and_hides_pool_equipment`
- `ApplicationModeTests.Application_mode_service_reads_configured_system_settings`
- `ApplicationModeTests.Dev_test_setting_is_enabled_only_when_configured_true`
- `ApplicationModeTests.Dev_test_setting_defaults_to_disabled_when_missing`
- `ApplicationModeTests.Configured_defaults_use_pool_mode_when_missing`

## User Documentation Impact

- User-visible behavior is documented in [System Settings](../user-guide/system-settings.md).

## Current Implementation

- `SystemSettingsConfiguration` reads configured defaults from configuration.
- `ApplicationModeService` returns the current `ApplicationModeSnapshot`.
- `SettingsPage.razor` renders the read-only settings panel.
- `PlatformAdminService.GetSystemSettingsAsync`, `UpdateSystemModeAsync`, and `UpdateSystemSettingsAsync` exist for system-admin service-layer access.
- `NavMenu.razor`, dashboards, catalog pages, pool equipment pages, and auth/test endpoints consume the resolved mode.

## Outstanding Tasks

- Decide whether to build a persisted System Settings editor or keep deployment settings as operator-owned configuration.
- Align architecture/source-of-truth wording around configured defaults versus persisted settings.
- Add UI/component tests for `/settings` visibility.

## Change Log

- 2026-08-05: Created implemented system-settings spec and corrected read-only settings behavior.
