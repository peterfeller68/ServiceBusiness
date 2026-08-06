# System Settings

Status: Implemented
Owner: Product
Last reviewed: 2026-08-06

## Problem

Deployments need a stable, persisted way for System Administrators to control product mode, development test-user sign-in, and Independent Home Owner trial length without redeploying the application.

## Personas

- System Administrator
- Developer/Operator

## Requirements

- `SystemSettings.SystemMode` supports `Pool` and `Landscape`.
- Pool mode uses PoolShark branding, the pool hero image, the Pool global catalog scope, and Pool Equipment/Pool Configuration visibility.
- Landscape mode uses TreeShark branding, the landscape hero image, the Landscape global catalog scope, and hides/redirects pool-equipment workflows.
- `SystemSettings.DevTest` controls test sign-in and Test navigation visibility.
- `SystemSettings.HomeOwnerTrialDays` controls the trial length assigned to new Independent Home Owner subscriptions.
- Configuration values provide first-run defaults when persisted settings are absent.
- The `/settings` page lets System Administrators view and edit SystemMode, DevTest, and HomeOwnerTrialDays.

## User Flows

### Operator Configures First-Run Defaults

1. An operator sets `SystemSettings__SystemMode`, `SystemSettings__DevTest`, and `SystemSettings__HomeOwnerTrialDays` in app settings or configuration.
2. The app starts with those defaults if the singleton system-settings row is missing.

### System Admin Updates Settings

1. A System Administrator opens `/settings`.
2. The page shows current persisted settings.
3. The System Administrator edits SystemMode, DevTest, or HomeOwnerTrialDays.
4. The app validates and persists the settings in storage.
5. New Independent Home Owner subscriptions use the saved HomeOwnerTrialDays value.

## UI Expectations

- `/settings` heading is System Settings.
- System Administrators see editable controls for SystemMode, HomeOwnerTrialDays, and DevTest.
- The Settings navigation menu links to System Settings for System Administrators.
- Non-system-admin users are told System Administrator access is required.

## Data Model Impact

- `SystemSettings` stores `SystemMode`, `DevTest`, and `HomeOwnerTrialDays`.
- Azure Table storage stores the singleton row in the `SystemSettings` table with partition `SYSTEM_SETTINGS` and row `current`.
- HomeOwnerTrialDays is normalized to zero or greater before it is saved.
- `ApplicationModeSnapshot` derives product name, hero image, pool-mode flag, global catalog company id, and DevTest flag.

## Authorization Rules

- The `/settings` route is linked from the Settings menu only for System Administrators.
- The System Settings editor is visible only to System Administrators.
- Service-layer update methods require System Administrator access.
- DevTest endpoints require `SystemSettings:DevTest`/configured DevTest to be true and the user to be a test user.

## Acceptance Criteria

- [x] Pool mode resolves PoolShark, pool hero image, Pool global catalog scope, and pool equipment visibility.
- [x] Landscape mode resolves TreeShark, landscape hero image, Landscape global catalog scope, and hides pool equipment.
- [x] DevTest defaults to disabled unless configured true.
- [x] `/settings` displays editable SystemMode, DevTest, and HomeOwnerTrialDays controls for System Administrators.
- [x] Saving `/settings` persists the singleton SystemSettings row in storage.
- [x] HomeOwnerTrialDays saved through System Settings controls the trial length for new Independent Home Owner subscriptions.
- [x] Test sign-in is blocked when DevTest is disabled.
- [x] Configuration values are treated as first-run defaults when no persisted SystemSettings row exists.

## Tests

- `ApplicationModeTests.Pool_mode_uses_pool_branding_and_pool_equipment`
- `ApplicationModeTests.Landscape_mode_uses_landscape_branding_and_hides_pool_equipment`
- `ApplicationModeTests.Application_mode_service_reads_configured_system_settings`
- `ApplicationModeTests.Dev_test_setting_is_enabled_only_when_configured_true`
- `ApplicationModeTests.Dev_test_setting_defaults_to_disabled_when_missing`
- `ApplicationModeTests.Configured_defaults_use_pool_mode_when_missing`
- `OnboardingTests.System_admin_can_update_persisted_homeowner_trial_days`
- `OnboardingTests.Updated_system_settings_drive_new_homeowner_subscription_trial`

## User Documentation Impact

- User-visible behavior is documented in [System Settings](../user-guide/system-settings.md).

## Current Implementation

- `SystemSettingsConfiguration` reads configured defaults from configuration.
- `ApplicationModeService` returns the current `ApplicationModeSnapshot`.
- `SettingsPage.razor` renders the System Administrator editor for SystemMode, DevTest, and HomeOwnerTrialDays.
- `PlatformAdminService.GetSystemSettingsAsync`, `UpdateSystemModeAsync`, and `UpdateSystemSettingsAsync` provide system-admin service-layer access and normalize HomeOwnerTrialDays before saving.
- `AzureTableServiceBusinessStore` persists the singleton settings row in the `SystemSettings` table.
- `InMemoryServiceBusinessStore` persists the same settings shape for local development and tests.
- `NavMenu.razor`, dashboards, catalog pages, pool equipment pages, and auth/test endpoints consume the resolved mode.

## Outstanding Tasks

- Add UI/component tests for `/settings` visibility.
- Consider adding audit history for System Settings changes before production operations depend on frequent edits.

## Change Log

- 2026-08-05: Created implemented system-settings spec and corrected read-only settings behavior.
- 2026-08-06: Implemented editable persisted System Settings for SystemMode, DevTest, and HomeOwnerTrialDays, linked it from Settings navigation, and connected saved trial length to new homeowner subscriptions.
