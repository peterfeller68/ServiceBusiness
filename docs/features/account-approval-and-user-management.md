# Account Approval and User Management

Status: Implemented
Owner: Product
Last reviewed: 2026-08-04

## Problem

System Administrators and Business Owners need focused tools to manage user records, approve or reject company access requests, control user status, and keep tenant access safe without exposing unrelated administration screens.

## Personas

- System Administrator
- Business Owner
- Business Employee
- Business Client

## Requirements

- System Administrators can view registered users and company access summaries from `/admin/users`.
- System Administrators can edit user profile fields, notification email, phone, test-user flag, and email-notification preference.
- System Administrators can create application users from the Users page or Test Users page.
- System Administrators can promote registered users to System Administrator or remove System Administrator access.
- System Administrators can disable or enable global user accounts.
- System Administrators cannot disable their own account.
- The system must keep at least one active System Administrator.
- Business Owners can view company users and pending access requests from `/company/users`.
- Business Owners can approve or reject pending employee and client-user access requests.
- Business Owners can deactivate or reactivate approved company memberships.
- Business Owners can update company-scoped user roles.
- Business Owners cannot deactivate their own Company Admin membership or remove their own Company Admin role.
- The system must keep at least one active Company Admin per company.
- Pending Business Employees and Business Clients cannot access company-scoped feature navigation until approved.
- Approval decisions queue account approval decision emails.

## User Flows

### System Admin Manages Users

1. A System Administrator opens `/admin/users`.
2. The system displays user metrics and a focused Users table.
3. The administrator edits user details, toggles active status, toggles System Admin access, or updates the primary company user type.
4. The system saves the change and reloads the user overview.

### Business Owner Reviews Pending Access

1. A Business Owner opens `/company/users`.
2. The system displays company metrics and company user rows, including pending requests.
3. The owner toggles approval on a pending user.
4. The system marks the request Active or Rejected, stores decision metadata, and queues an approval decision email.

### Business Owner Changes Company Access

1. A Business Owner opens `/company/users`.
2. The owner toggles an approved user's active state.
3. The system updates the company membership between Active and Inactive.

### Business Owner Changes Company Role

1. A Business Owner opens `/company/users`.
2. The owner selects a new role for an approved company user.
3. The system marks the old membership role Removed and creates or reactivates the replacement role with the previous active/inactive state.

## UI Expectations

- `/admin/users` has a System Administrator page heading and focused user-management content.
- `/admin/users` shows metrics for users, active users, system admins, pending access, and disabled users.
- `/admin/users` renders a collapsible Users panel with status, name, email, flags, company, user type, approved, Sys Admin, and action columns.
- `/admin/users` disables self-changing controls for the current user.
- `/company/users` has a Company Admin page heading and focused company user-management content.
- `/company/users` shows metrics for company, active users, and pending access.
- `/company/users` renders a collapsible Users panel with status, name, email, flags, user type, approved, and action columns.
- User actions use the existing toggle, select, and icon action conventions.
- Success and error messages are displayed inline.

## Data Model Impact

- `AppUser.Status` stores global active/disabled state.
- `AppUser.IsSystemAdmin` stores platform-admin access.
- `AppUser.IsTestUser`, `NotificationEmail`, and `EmailNotificationsEnabled` are editable user metadata.
- `CompanyMembership.Status` stores Pending, Active, Rejected, Inactive, and Removed states.
- `CompanyMembership.DecidedUtc` and `DecidedByUserId` store approval and access-change decision metadata.
- `CompanyMembership.CompanyClientId` links client-user memberships to a business-client address.
- `UserManagementRow` and `PlatformUserManagementOverview` shape system-admin user-management read models.
- `CompanyUserManagementRow` and `CompanyUserManagementOverview` shape company-admin user-management read models.
- Azure Table storage writes membership rows to both `CompanyMemberships` and `UserCompanyMemberships`.

## Authorization Rules

- `/admin/users` actions require System Administrator access.
- `/company/users` actions require active `CompanyAdmin` membership for the current company.
- Disabled users are rejected by authorization before role checks.
- System Administrators cannot disable themselves.
- System Administrators cannot remove or disable the last active System Administrator.
- Company Admins cannot deactivate their own Company Admin access.
- Company Admins cannot remove their own Company Admin role.
- Company Admin role changes and deactivation preserve at least one active Company Admin.
- Pending memberships can only be approved or rejected.
- Approved company memberships can only be activated/deactivated between Active and Inactive.

## Acceptance Criteria

- Implemented: System Administrators can view focused user management at `/admin/users`.
- Implemented: System Administrators can edit user profile and notification fields.
- Implemented: System Administrators can create users.
- Implemented: System Administrators can promote or remove System Administrator access.
- Implemented: System Administrators can enable or disable users.
- Implemented: System Administrators cannot disable themselves.
- Implemented: The system prevents removing or disabling the last active System Administrator.
- Implemented: Business Owners can view company user management at `/company/users`.
- Implemented: Business Owners can approve or reject pending company access.
- Implemented: Business Owners can activate or deactivate approved company memberships.
- Implemented: Business Owners can update company-scoped roles for approved users.
- Implemented: Company role reassignment preserves the previous active/inactive state and marks the old role Removed.
- Implemented: Business Owners cannot deactivate or reassign their own Company Admin access.
- Implemented: The system prevents removing the last active Company Admin.
- Implemented: Approval decisions queue account approval decision email work.

## Tests

- `OnboardingTests.Business_owner_can_approve_pending_access_request`
- `AuthorizationTests.System_admin_can_promote_registered_user_to_system_admin`
- `AuthorizationTests.System_admin_cannot_disable_self`
- `AuthorizationTests.System_admin_can_delete_user_and_user_owned_rows`
- `AuthorizationTests.System_admin_can_create_user`
- `AuthorizationTests.Company_admin_can_manage_company_user_access_and_roles`
- `AuthorizationTests.Company_admin_cannot_remove_last_active_company_admin`
- `AuthorizationTests.Company_dashboard_includes_setup_counts_and_pending_approval_splits`

## User Documentation Impact

- Created `docs/user-guide/account-approval-and-user-management.md`.
- Updated `docs/user-guide/index.md` with an Account Approval and User Management link.
- `docs/user-guide/getting-started.md` already describes pending approval at a high level.

## Current Implementation

- `AdminUsersPage.razor` implements `/admin/users` as a focused System Administrator user-management page.
- `CompanyUsersPage.razor` implements `/company/users` and legacy `/settings/users` as focused company user-management pages.
- `PlatformAdminService` provides platform user overview, user create/update, system-admin promotion/removal, global enable/disable, company membership approval toggles, and company membership role updates for System Administrators.
- `CompanyAdminService` provides pending access request reads, company user management overview, access decision, membership activation/deactivation, and company-scoped role update behavior for Business Owners.
- `TenantAuthorizationService` rejects missing, disabled, and unauthorized users.
- Approval decisions emit `DecideAccessRequest` telemetry and queue approval decision email work through `INotificationQueue`.
- Azure Table storage persists user rows, user lookup rows, and membership rows used by management screens and sign-in access overview.

## Outstanding Tasks

- Add browser-level coverage for `/admin/users` and `/company/users`.
- Add direct tests for system-admin company membership approval and role-update paths exposed through `/admin/users`.
- Add invitation-code or invitation-link workflows if business-led invitations become part of onboarding.
- Decide whether rejected memberships should be editable/reactivatable from company user management.
- Add audit-event records for user status changes, role changes, and approval decisions if audit history is required.

## Change Log

- 2026-08-04: Documented implemented account approval and user-management behavior from code, tests, UI pages, storage, and source-of-truth docs.
