# Registration and Authentication

Status: Implemented
Owner: Product
Last reviewed: 2026-08-06

## Problem

Users need a secure way to register for the correct persona, authenticate with Google for real accounts, bypass Google only for seeded test users in DevTest mode, and land in the appropriate role-aware workspace after sign-in.

## Personas

- Public visitor
- System Administrator
- Business Owner
- Business Employee
- Business Client
- Independent Home Owner
- Seeded test user

## Requirements

- Public users can open Home, Sign In, Register, and Help before authentication.
- Registration supports Business Owner, Business Employee, Business Client, and Independent Home Owner account types.
- Real registrations require a Gmail address.
- Real sign-in uses Google Authentication when `Authentication:Google:ClientId` and `Authentication:Google:ClientSecret` are configured.
- Google sign-in creates or updates the application user profile from Google subject id, email, display name, and profile image claims.
- DevTest mode exposes Skip Google Auth flows for seeded or newly registered test users.
- Test sign-in must be available only when `SystemSettings:DevTest` is enabled.
- Only users marked `IsTestUser` can complete the `/auth/test-signin` bypass endpoint.
- Business Owner registration creates an active service client company and active `CompanyAdmin` membership.
- Business Employee registration requires selecting an active service client and creates a pending `CompanyUser` membership.
- Business Client registration requires selecting an active service client and active business-client address, then creates a pending `CompanyClientUser` membership linked to that business client.
- Independent Home Owner registration requires a home address, creates an active user without company memberships, creates an owner profile, and seeds owner-scoped pool equipment starter records.
- Independent Home Owner registration includes subscription plan selection and creates a homeowner subscription through the Subscriptions feature.
- Registered users are signed into the application cookie and redirected to `/dashboard` through the registration sign-in endpoint.
- Disabled users cannot sign in or pass authorization checks.
- Authenticated navigation hides Home and shows routes based on active memberships, system-admin status, pending-only status, and independent-homeowner state.

## User Flows

### Google Sign In

1. The user opens `/signin`.
2. The user selects Sign in with Google.
3. The app starts `/auth/google` with a safe return URL.
4. Google authenticates the user and returns to `/auth/google-complete`.
5. The app creates or updates `AppUser`, signs in with an application cookie, and redirects to `/dashboard`.

### DevTest Sign In

1. A seeded test user opens `/signin` while DevTest mode is enabled.
2. The user enters a seeded test user id or email.
3. The app navigates to `/auth/test-signin`.
4. The endpoint verifies DevTest mode and `IsTestUser`, signs in with an application cookie, and redirects to `/dashboard`.

### Business Owner Registration

1. The public user opens `/register`.
2. The user chooses Business owner.
3. The user authenticates with Google, or skips Google only in DevTest mode.
4. The user enters account and business profile details.
5. The app creates or updates the user, creates the service client company for the current system mode, creates the Home Owner client type, creates active `CompanyAdmin` membership, and redirects to `/dashboard`.

### Business Employee Registration

1. The public user opens `/register`.
2. The user chooses Business employee.
3. The user authenticates with Google, or skips Google only in DevTest mode.
4. The user selects an active service client and enters account details.
5. The app creates or updates the user, creates a pending `CompanyUser` membership, signs the user in, and redirects to `/dashboard`.
6. The dashboard shows pending approval until a Business Owner approves the membership.

### Business Client Registration

1. The public user opens `/register`.
2. The user chooses Business client.
3. The user authenticates with Google, or skips Google only in DevTest mode.
4. The user selects an active service client and active business-client address.
5. The app creates or updates the user, creates a pending `CompanyClientUser` membership linked to the selected business client, signs the user in, and redirects to `/dashboard`.
6. The dashboard shows pending approval until a Business Owner approves the membership.

### Independent Home Owner Registration

1. The public user opens `/register`.
2. The user chooses Home owner.
3. The user authenticates with Google, or skips Google only in DevTest mode.
4. The user enters account details, home address, and optional access notes.
5. The app creates or updates the user, stores the homeowner profile, seeds owner-scoped pool equipment starter records when needed, signs the user in, and redirects to `/dashboard`.
6. The app creates a homeowner subscription using the selected plan and configured trial length.

## UI Expectations

- The landing page shows app-mode branding, hero imagery, Sign In, and Register actions.
- `/signin` shows Sign in with Google for real accounts.
- `/signin` shows the test user id/email field and Skip Google Auth button only in DevTest mode.
- `/register` is a three-step flow: account type, Gmail authentication, role-specific details.
- Registration account type cards are Business owner, Business employee, Business client, and Home owner.
- Business Owner details collect business name, email, phone, and service area.
- Business Employee details collect the selected business and display a pending-approval note.
- Business Client details collect the selected business and selected business-client address, and display a pending-approval note.
- Independent Home Owner details collect home address and access notes, and indicate that no business approval is required.
- Independent Home Owner details include subscription plan selection.
- Pending-only users see Home and Help in navigation and a pending approval dashboard after sign-in.

## Data Model Impact

- `RegistrationAccountType` includes `BusinessOwner`, `BusinessUser`, `BusinessClient`, and `IndependentHomeOwner`.
- `AppUser` stores Google subject id, login email, notification email, display name, phone, profile image URL, system-admin flag, test-user flag, email-notification preference, and active/disabled status.
- `CompanyMembership` stores company id, user id, role, membership status, request/decision timestamps, deciding user id, and optional `CompanyClientId` for client-user access.
- Business Owner registration creates a `Company` and a `CompanyAdmin` membership.
- Business Employee registration creates a pending `CompanyUser` membership.
- Business Client registration creates a pending `CompanyClientUser` membership with `CompanyClientId`.
- Independent Home Owner registration stores an `IndependentHomeOwnerProfile` and owner-scoped pool equipment category/item records.
- Azure Table storage persists users in `Users`, email lookups in `UserByEmail`, Google subject lookups in `UserByGoogleSubject`, company memberships in `CompanyMemberships`, user membership lookups in `UserCompanyMemberships`, and homeowner profiles in `IndependentHomeOwnerProfiles`.

## Authorization Rules

- Unauthenticated users can access public registration, sign-in, landing, and Help pages.
- Real non-test users must authenticate through Google.
- `/auth/test-signin` requires DevTest mode and an existing `IsTestUser` account.
- `/auth/registration-signin` allows sign-in only for the authenticated Google user or, in DevTest mode, the newly registered test user.
- Disabled users are rejected during sign-in and authorization.
- Company-scoped application services require active company memberships.
- Pending Business Employee and Business Client memberships do not unlock company-scoped feature navigation.
- Independent Home Owner users are active users with no company memberships and use their user id as owner scope.

## Acceptance Criteria

- Implemented: Public users can open Sign In and Register from the landing page.
- Implemented: Registration supports Business Owner, Business Employee, Business Client, and Independent Home Owner account types.
- Implemented: Registration requires Gmail-format email addresses.
- Implemented: Business Owner registration creates active company admin access.
- Implemented: Business Employee registration creates pending company employee access.
- Implemented: Business Client registration requires and stores a selected business-client address.
- Implemented: Independent Home Owner registration creates an active owner workspace without company membership.
- Implemented: Independent Home Owner registration stores home address/access notes and seeds owner-scoped pool equipment starter rows.
- Implemented: Google sign-in creates or updates app user profiles and signs in with an application cookie.
- Implemented: Seeded test users can skip Google authentication by email.
- Implemented: Seeded test users can skip Google authentication by user id.
- Implemented: DevTest mode controls whether the UI exposes Skip Google Auth.
- Implemented: Disabled users cannot sign in or pass authorization.
- Implemented: Authenticated navigation hides Home and shows persona-appropriate routes.
- Implemented: Independent Home Owner registration creates a homeowner subscription with the selected plan and configured trial length.

## Tests

- `OnboardingTests.Business_owner_registration_creates_active_company_admin_access`
- `OnboardingTests.Business_user_registration_creates_pending_company_membership`
- `OnboardingTests.Business_client_registration_requires_and_stores_selected_client_address`
- `OnboardingTests.Independent_homeowner_registration_creates_active_owner_workspace_without_company_membership`
- `OnboardingTests.Independent_homeowner_registration_uses_selected_subscription_plan_and_trial_days`
- `OnboardingTests.Seeded_test_users_can_skip_gmail_authentication`
- `OnboardingTests.Seeded_test_users_can_skip_gmail_authentication_by_user_id`
- `AuthorizationTests.Business_owner_registration_uses_current_system_mode_company_type`
- `AuthorizationTests.Seed_data_includes_requested_test_companies_catalogs_and_users`

## User Documentation Impact

- Created `docs/user-guide/registration-and-authentication.md`.
- Updated `docs/user-guide/getting-started.md` with registration, sign-in, DevTest, and pending approval behavior.
- Updated `docs/user-guide/index.md` with a Registration and Authentication link.

## Current Implementation

- `LandingPage.razor` exposes Sign In and Register actions with product branding from `ApplicationModeService`.
- `SignInPage.razor` exposes Google sign-in and, when DevTest is enabled, a seeded test-user id/email form.
- `RegisterPage.razor` implements account type, authentication, and details steps for all registration personas.
- `RegisterPage.razor` shows subscription plan choices for Independent Home Owner registration.
- `Program.cs` configures ASP.NET Core cookie authentication, optional Google authentication, `/auth/google`, `/auth/google-complete`, `/auth/test-signin`, `/auth/registration-signin`, and `/auth/signout`.
- `OnboardingService` owns available business lookup, available business-client address lookup, available homeowner subscription plan lookup, registration, test sign-in, Google sign-in completion, and access overview loading.
- `TenantAuthorizationService` rejects missing users, disabled users, and users without active memberships for protected company workflows.
- `AuthenticatedCurrentUserContext` reads the signed-in app user id claim and falls back to `DemoCurrentUserContext` when no HTTP user claim is available.
- Azure Table storage keeps `Users`, `UserByEmail`, `UserByGoogleSubject`, `CompanyMemberships`, `UserCompanyMemberships`, and `IndependentHomeOwnerProfiles` in sync for registration and sign-in.

## Outstanding Tasks

- Add browser-level coverage for `/signin`, `/register`, and DevTest visibility.
- Add endpoint-level tests for `/auth/google`, `/auth/test-signin`, `/auth/registration-signin`, safe return URLs, and disabled-user rejection.
- Replace placeholder/test copy on the Sign In page with the current seeded test-user examples if seed data changes.
- Add invitation-code or invitation-link registration when that workflow is defined.
- Add user-facing setup documentation for Google OAuth configuration under operations docs if the previous setup note is no longer available after the docs reorganization.
- Add browser-level coverage for Independent Home Owner subscription plan selection.
- Add live payment-provider checkout handoff when Payment Integration is completed.

## Change Log

- 2026-08-04: Documented implemented registration and authentication behavior from code, tests, UI pages, storage, and configuration.
- 2026-08-06: Clarified that Independent Home Owner subscription/payment onboarding is a planned downstream handoff owned by Subscriptions and Payment Integration, not current registration behavior.
- 2026-08-06: Implemented Independent Home Owner subscription plan selection and homeowner subscription creation during registration.
