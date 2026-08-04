# Dashboard

Status: Implemented
Owner: Product
Last reviewed: 2026-08-04

## Problem

After sign-in, each user needs a role-appropriate workspace that surfaces the work they can act on immediately without sending every persona into the same administration page.

## Personas

- System Administrator
- Business Owner
- Business Employee
- Business Client
- Independent Home Owner
- Pending approval user

## Requirements

- Authenticated users land on `/dashboard`.
- Dashboard content is persona-aware and based on system-admin status, active company memberships, pending company memberships, or independent homeowner profile.
- Dashboard pages use the current application mode hero image and product branding.
- Pending-only users see a pending approval dashboard instead of unlocked feature panels.
- System Administrators see platform health, approvals, and system workspace links.
- Business Owners see service-client health, approvals, assigned/pending visit panels, and business workspace links.
- Business Employees see assigned visit panels scoped to the signed-in employee.
- Business Clients see their upcoming visits, assigned service package, Pool configuration in Pool mode, and completed visits.
- Independent Home Owners see Pool equipment/configuration and service history panels.
- Pool-only panels and navigation are hidden in Landscape mode.

## User Flows

### Sign In to Dashboard

1. The user signs in or completes registration.
2. The application redirects to `/dashboard`.
3. The dashboard evaluates the signed-in user's access overview.
4. The user sees the highest-priority dashboard available for their access state.

### Approve Access Requests

1. A System Administrator or Business Owner opens the dashboard.
2. The user expands Approvals.
3. The user approves individual pending requests with the approval toggle or selects Approve All.
4. The dashboard reloads the pending request list.

### Work Assigned Visits

1. A Business Owner or Business Employee opens `/dashboard`.
2. Today's assigned visits are expanded by default.
3. The user can mark visits complete and open/edit allowed visit details.
4. Business Employees can add completed services and out-of-scope services while editing assigned visits.

### Client Visit Notes

1. A Business Client opens `/dashboard`.
2. Upcoming Visits is expanded by default.
3. The client edits only Notes To Service Provider.
4. The note is saved back to the visit's service-client notes field.

### Homeowner Service History

1. An Independent Home Owner opens `/dashboard`.
2. The user opens Service History.
3. The user creates, edits, or deletes their own service history rows.

## UI Expectations

- Dashboard panels are collapsible.
- Toggle buttons use compact icon-style controls.
- Dashboard metrics are clickable when they represent a workspace.
- System and Business Owner dashboards include System Health, Approvals, and workspace link sections.
- Visit info actions open a detail view with the full visit field set.
- Pool equipment info actions open a detail view with manufacturer, model number, description, and comment.

## Data Model Impact

- `CompanyDashboard` summarizes company health counts and pending access requests.
- `IndependentHomeOwnerDashboard` summarizes homeowner profile, Pool equipment, and service history.
- `ServiceVisit` supports dashboard visit panels through assigned user, scheduled date, status, notes, planned services, completed services, out-of-scope services/materials, and invoice id.
- `CompanyMembership` and `AccessRequest` drive approval panels.
- `ServicePackage` supports Business Client package display.
- `PoolEquipmentOverview` supports Business Client and Independent Home Owner Pool configuration panels.

## Authorization Rules

- System Administrator dashboard requires `AppUser.IsSystemAdmin`.
- Business Owner dashboard requires an active `CompanyAdmin` membership.
- Business Employee dashboard requires an active `CompanyUser` membership.
- Business Client dashboard requires an active `CompanyClientUser` membership with a linked business client.
- Independent Home Owner dashboard requires an independent homeowner profile and no active or pending company memberships.
- Pending approval dashboard appears for users with pending memberships and no active company access.
- Business Owner dashboard access to company summary data requires `CompanyAdmin`.
- Business Employee dashboard visit data is scoped to the signed-in employee.
- Business Client dashboard visit, service package, Pool configuration, and invoice data are scoped to the signed-in user's linked business client.

## Acceptance Criteria

- Implemented: Authenticated users land on `/dashboard`.
- Implemented: Pending-only users see a pending approval dashboard.
- Implemented: System Administrators see health metrics, approvals, and workspace links.
- Implemented: System Administrator dashboard filters service clients and users to the current app mode.
- Implemented: Business Owners see health metrics, approvals, assigned visits today, upcoming visits, unscheduled/scheduled visits, and workspace links.
- Implemented: Business Owners can approve all or individual pending business access requests from the dashboard.
- Implemented: Business Owners can assign pending visits and mark assigned visits complete from the dashboard.
- Implemented: Business Employees see today, upcoming, and recently completed assigned visit panels.
- Implemented: Business Employees can complete today's visits, move completed visits back to In Progress, and edit allowed visit details.
- Implemented: Business Clients see upcoming visits, service package, Pool configuration in Pool mode, and completed visits.
- Implemented: Business Clients can edit only Notes To Service Provider for upcoming visits.
- Implemented: Independent Home Owners see Pool Equipment and Service History panels and can manage service history.
- Implemented: Dashboard hero imagery follows the current application mode.
- Not implemented: Application Insights telemetry dashboard widgets.
- Not implemented: Stripe/payment health dashboard widgets.
- Not implemented: Audit event dashboard widgets.
- Not implemented: Map/route visualization on the employee dashboard.

## Tests

- `AuthorizationTests.Company_user_cannot_read_admin_dashboard`
- `AuthorizationTests.Business_client_dashboard_can_read_assigned_service_package`
- `AuthorizationTests.Company_dashboard_includes_setup_counts_and_pending_approval_splits`
- `FieldWorkTests.Employee_dashboard_queries_split_today_upcoming_and_completed_visits`
- `RegistrationBrowserScenarioTests.Independent_homeowner_can_register_and_open_dashboard`

Related tests also cover visit assignment, employee visit detail updates, business-client visit access, and access approval behavior used by dashboard actions.

## User Documentation Impact

- Created `docs/user-guide/dashboard.md` to explain dashboard behavior by persona.
- User documentation should be updated when telemetry, payment health, audit summaries, route maps, or additional dashboard actions are added.

## Current Implementation

- `/dashboard` is implemented by `UserDashboardPage.razor` and chooses persona content from `OnboardingService.GetAccessOverviewAsync`.
- System Administrators see the current mode's platform dashboard with System Health metrics for Service Clients, Business Clients, Users, Pool Equipment and Pool Configurations in Pool mode, Materials, Services, and Service Packages. They also see Approvals with individual toggle approval and Approve All, plus System Workspaces quick links.
- Business Owners see the active service-client dashboard with System Health metrics for Business Clients, Users, Pool Configurations in Pool mode, Materials, Services, and Service Packages. They also see Approvals, Assigned Visits Today, Upcoming Visits, Unscheduled and Scheduled Visits, and System Workspaces.
- Business Owner Assigned Visits Today is scoped to visits assigned to the signed-in owner for today and supports complete, edit, and info actions. Upcoming Visits is scoped to future assigned visits and is view-only with info. Unscheduled and Scheduled Visits lists New, Unscheduled, and Scheduled visits and supports inline assignment to active business owners or employees.
- Business Employees see Assigned Visits Today, Upcoming Visits, and Recently Completed Visits. Today's visits can be completed; recently completed visits can be moved back to In Progress; editable visit details include customer-facing notes, owner-facing notes, internal notes, completed planned services, and out-of-scope services.
- Business Clients see Upcoming Visits, Service Package, Pool Configuration, and Completed Visits. Upcoming Visits is expanded by default and lets the client edit only Notes To Service Provider. The Service Package panel shows the package assigned to the business client or falls back to the service client's package. Pool Configuration shows configured equipment only in Pool mode. Completed Visits is the service history surface.
- Independent Home Owners see Pool Equipment and Service History panels. Pool Equipment shows category, manufacturer, equipment, and comment with an info action. Service History supports create, edit, and delete actions for homeowner-entered rows.
- Pending-only users see a Pending Approval dashboard listing pending company access requests and role descriptions.
- The legacy focused company dashboard at `/company` still exists for company-admin summary access.

## Outstanding Tasks

- Add telemetry widgets when Application Insights data is surfaced in the UI.
- Add audit-event and payment-health summaries when those features are implemented.
- Decide whether the legacy `/company` dashboard should remain or redirect to `/dashboard`.
- Add explicit dashboard UI/component tests for role-specific panels and hidden Landscape-mode Pool sections.
- Add delete/undo confirmation patterns for dashboard actions that become destructive in future slices.
- Add route/map preview if the employee dashboard becomes a route-execution surface.

## Change Log

- 2026-08-04: Documented the implemented dashboard feature, including persona behavior, current code implementation, acceptance criteria status, tests, and outstanding tasks.
