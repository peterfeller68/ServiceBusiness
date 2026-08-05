# Test Mode And Test Users

Status: Implemented
Owner: Product
Last reviewed: 2026-08-05

## Problem

Development and QA need a controlled way to bypass Google authentication, create persona-specific test users, and avoid sending real email to test recipients.

## Personas

- Developer/Tester
- System Administrator

## Requirements

- DevTest mode is enabled only when `SystemSettings:DevTest` is true.
- DevTest mode exposes Skip Google Auth on sign-in and registration flows.
- DevTest mode shows a Test navigation section with Test Page and Test Users.
- `/auth/test-signin` signs in only existing `IsTestUser` accounts when DevTest is enabled.
- Test Users page supports creating and editing test users by persona.
- Test user personas include System Admin, Business Owner, Business Employee, Business Client, Homeowner, and General Test User.
- Test users can be active or disabled, pending or approved, associated with a business when required, and configured for notification email and email notification preference.

## User Flows

### Test Sign In

1. DevTest is enabled.
2. A tester opens `/signin`.
3. The tester enters a seeded test user id or email and selects Skip Google Auth.
4. The app signs in only if the account exists and is marked `IsTestUser`.

### Manage Test Users

1. A user opens Test / Test Users.
2. The page forces demo current user `sys-admin`.
3. The user creates or edits a test account.
4. The page configures system-admin flag, company membership, approval state, and homeowner profile marker as needed.

## UI Expectations

- Test navigation appears only when DevTest is enabled and the user is not pending-only.
- Test Users page has a focused Test Users management panel.
- The editor fields are user type, associated business, display name, login email, notification email, phone, approval needed, and email notifications.
- The table shows id, active toggle, membership status, type, name, email, business, approval needed, and actions.

## Data Model Impact

- `AppUser.IsTestUser` marks test users.
- `CompanyMembership` stores test-user business role and approval state.
- Independent Home Owner test users are identified by a marker on `IndependentHomeOwnerProfile.AccessNotes`.
- `AppUser.NotificationEmail` and `EmailNotificationsEnabled` are editable for test users.

## Authorization Rules

- DevTest endpoints require DevTest mode.
- `/auth/test-signin` rejects non-test users.
- Test Users page and backing service methods use System Administrator access.
- Business-associated test user types require a selected company.

## Acceptance Criteria

- [x] DevTest setting defaults to false and is enabled only by `true`.
- [x] Skip Google Auth appears only in DevTest mode.
- [x] Test navigation appears only in DevTest mode.
- [x] Test Users page can create and edit test users.
- [x] Test Users page can disable and reactivate test users.
- [x] Test users can be configured with persona, business, approval state, notification email, and email preference.
- [x] Email jobs and notification queueing avoid provider delivery for test users.
- [ ] Test Users page is not protected by route-level authorization attributes; it relies on DevTest navigation visibility and system-admin service calls.

## Tests

- `ApplicationModeTests` coverage for DevTest configuration.
- `RegistrationBrowserScenarioTests` coverage for DevTest registration/sign-in visibility.
- `AuthorizationTests.Seed_data_includes_requested_test_companies_catalogs_and_users`
- `EmailNotificationTests` coverage for test-user rerouting and provider bypass.

## User Documentation Impact

- User-facing behavior is documented in [Test Mode And Test Users](../user-guide/test-mode-and-test-users.md).
- Getting Started already notes test sign-in availability when DevTest is enabled.

## Current Implementation

- `SystemSettingsConfiguration.IsDevTestEnabled` controls DevTest UI and endpoints.
- `SignInPage.razor` and `RegisterPage.razor` expose skip-auth flows only in DevTest.
- `NavMenu.razor` exposes Test navigation only when `appMode.DevTest`.
- `TestUsersPage.razor` provides the management UI.
- `PlatformAdminService.CreateUserAsync`, `UpdateUserAsync`, `SetUserStatusAsync`, and `ConfigureTestUserAccessAsync` perform persistence.
- `EmailJobService` and notification queueing treat test recipients safely.

## Outstanding Tasks

- Add explicit route authorization for Test Users and Test Page.
- Add direct tests for Test Users page save behavior.
- Decide whether Test Users should support hard delete or remain disable/reactivate only.

## Change Log

- 2026-08-05: Created implemented test-mode-and-test-users spec from auth, navigation, page, services, and tests.
