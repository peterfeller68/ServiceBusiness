# Roles and Permissions

Status: Implemented
Owner: Product
Last reviewed: 2026-08-04

## Problem

The application needs stable built-in company role identities for authorization plus editable role metadata and permission lists so administrators can describe and prepare role capabilities without destabilizing runtime access checks.

## Personas

- System Administrator
- Business Owner
- Business Employee
- Business Client

## Requirements

- The company role identities are fixed to `CompanyAdmin`, `CompanyUser`, and `CompanyClientUser`.
- System Administrators can view role definitions from `/admin/roles`.
- System Administrators can edit role display names.
- System Administrators can edit role descriptions.
- System Administrators can edit whether a role requires owner approval.
- System Administrators can edit role permission lists.
- Permission lists are normalized by trimming, deduplicating case-insensitively, and sorting.
- A role definition must have a display name, description, and at least one permission.
- Legacy role definitions without permissions are normalized with default permissions when read.
- Runtime authorization currently uses fixed role identities, system-admin flags, membership status, and tenant/client/owner scope checks.
- Permission strings are metadata in the current implementation and are not yet enforced as individual authorization policies.

## User Flows

### View Roles

1. A System Administrator opens `/admin/roles`.
2. The system lists built-in role definitions with ID, display name, description, owner-approval flag, and permission count.

### Edit Role Metadata

1. A System Administrator selects Edit for a role.
2. The inline editor opens with role identity disabled.
3. The administrator updates display name, description, permissions, or owner approval.
4. The system validates and saves the role definition.

## UI Expectations

- `/admin/roles` has a System Administrator page heading and focused role-management content.
- Roles are displayed in a collapsible management panel.
- Role identity is shown but disabled in the editor.
- Display name is a text input.
- Description and permissions are multiline text inputs.
- Owner approval is a checkbox.
- Permissions may be separated by line breaks or commas.
- Success and error messages are displayed inline.

## Data Model Impact

- `RoleDefinition` stores `Role`, `DisplayName`, `Description`, `RequiresOwnerApproval`, and `Permissions`.
- `CompanyRole` stores fixed company role identities: `CompanyAdmin`, `CompanyUser`, and `CompanyClientUser`.
- Azure Table storage persists role definitions in `RoleDefinitions`.
- User-management and access-request read models include role definitions for display labels and flags.

## Authorization Rules

- Only System Administrators can read and update role definitions through `PlatformAdminService`.
- Company role identities are not user-editable.
- Permission metadata does not bypass runtime authorization.
- Runtime authorization still requires active memberships and fixed role checks for company-scoped workflows.

## Acceptance Criteria

- Implemented: System Administrators can open a focused Roles page at `/admin/roles`.
- Implemented: Role definitions display ID, name, description, owner approval, permission count, and edit action.
- Implemented: Role identity is immutable in the role editor.
- Implemented: Display name, description, owner approval, and permissions can be edited.
- Implemented: Empty display names are rejected.
- Implemented: Empty descriptions are rejected.
- Implemented: Empty permission lists are rejected.
- Implemented: Permission lists are trimmed, deduplicated, and sorted.
- Implemented: Legacy role rows without permissions receive default permission lists when read.
- Implemented: Runtime access control remains based on fixed roles, active membership status, system-admin flag, and tenant/client/owner scope.
- Not implemented: Permission-string-level runtime policy enforcement.

## Tests

- `AuthorizationTests.System_admin_can_update_role_definition_permissions`
- `AuthorizationTests.Role_definition_requires_at_least_one_permission`
- `AuthorizationTests.Legacy_role_definition_without_permissions_gets_default_permissions`
- `AuthorizationTests.Company_user_cannot_read_admin_dashboard`
- `AuthorizationTests.System_admin_can_read_platform_companies`

## User Documentation Impact

- Created `docs/user-guide/roles-and-permissions.md`.
- Updated `docs/user-guide/index.md` with a Roles and Permissions link.

## Current Implementation

- `AdminRolesPage.razor` implements the focused `/admin/roles` page.
- `PlatformAdminService.GetRoleDefinitionsAsync` requires System Administrator access and normalizes legacy role definitions with defaults when needed.
- `PlatformAdminService.UpdateRoleDefinitionAsync` requires System Administrator access, validates display name, description, and permissions, then stores normalized role metadata.
- `CompanyAdminService` also normalizes role definitions when building access requests and company user-management rows.
- `TenantAuthorizationService` enforces current runtime access using system-admin status and active company memberships rather than permission strings.
- `RoleDefinitions` are seeded by the store and persisted by Azure Table storage.

## Outstanding Tasks

- Add component or browser-level coverage for `/admin/roles`.
- Define whether permission strings should become enforced authorization policies.
- Add audit history for role definition changes if compliance requires traceability.
- Decide whether owner-approval metadata should drive registration behavior dynamically or remain descriptive while registration uses fixed role flows.

## Change Log

- 2026-08-04: Documented implemented role metadata and permission-list behavior from code, tests, UI pages, storage, and source-of-truth docs.
