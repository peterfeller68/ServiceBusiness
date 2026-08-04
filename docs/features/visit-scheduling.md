# Visit Scheduling

Status: Implemented
Owner: Product
Last reviewed: 2026-08-04

## Problem

Service clients need a reliable way to create, assign, work, complete, close, inspect, and later invoice service visits while keeping each persona limited to the visit actions they are allowed to perform.

## Personas

- System Administrator
- Business Owner
- Business Employee
- Business Client

Independent Home Owners do not currently have visit scheduling access.

## Requirements

- Visit Scheduling is available from a top-level navigation section at the same level as Settings.
- System Administrators use `/admin/visits`.
- Business Owners use `/visits`.
- `/schedule` remains available as a legacy route to the same page.
- Visits are grouped into four panels:
  - Unscheduled and Scheduled Visits
  - Assigned Visits
  - Recently Completed Visits
  - Closed Visits
- System Administrators see service-client columns and only service clients matching the current Pool or Landscape mode.
- Business Owners see only their active service client and omit the service-client column.
- Business Employees work assigned visits from `/dashboard` and `/field`.
- Business Clients see upcoming and completed visits from `/dashboard`.
- Independent Home Owners have no visit scheduling access.

## User Flows

### Create an Ad-Hoc Visit

1. A System Administrator or Business Owner opens Visit Scheduling.
2. The user expands Unscheduled and Scheduled Visits.
3. The user selects Create.
4. The user chooses the service client when in system-admin view, business client, visit type, visit name, date, assignee, status, notes, and planned services.
5. The user saves the visit.
6. The system normalizes visit status from the saved visit data.

### Assign a Visit

1. A Business Owner opens `/dashboard` or Visit Scheduling.
2. The user chooses an active business owner or employee.
3. The system validates the assignee has active company admin or company user membership.
4. The visit becomes `Assigned` when it has a scheduled date, or remains `New` when it is still unscheduled.

### Complete Field Work

1. A Business Employee opens `/dashboard` or `/field`.
2. The employee opens an assigned visit.
3. The employee records completed planned services, materials used, notes to the business client, and internal notes.
4. The system marks the visit `Completed`, stores completion details directly on the visit, queues the completion notification, and records telemetry.

### Close a Visit

1. A System Administrator or Business Owner marks a completed visit closed.
2. The visit moves to Closed Visits.
3. Closed visits without invoice ids become eligible for invoice creation.

### Business Client Notes

1. A Business Client opens `/dashboard`.
2. The client edits Notes To Service Provider on an upcoming visit.
3. The system stores the note as the visit's service-client note.

## UI Expectations

- The Visit Scheduling page displays four collapsible management panels.
- Unscheduled/Scheduled, Assigned, and Recently Completed are expanded by default; Closed is collapsed by default.
- Visit rows show Visit Type, Visit Date, Visit Name, Business Client, Assigned To, Status, Invoice ID and actions.
- System Administrator rows also show Service Client.
- Invoice ID is only shown on Closed Visits.
- Create/Edit Visit uses an inline editor inside the Unscheduled and Scheduled Visits panel.
- Services are selected from a searchable/filterable chooser that combines service-client and app-mode global services.
- Chosen services are shown before the service chooser, and Save/Cancel buttons sit below Chosen Services.
- Actions are icon-only controls for edit, complete, close, info, and delete where allowed.
- The visit info action shows the full visit details, including notes, services, materials, timestamps, completion data, and invoice id.

## Data Model Impact

- `VisitStatus` includes `New`, `Unscheduled`, `Scheduled`, `Assigned`, `InProgress`, `Completed`, `Closed`, `Canceled`, and `Skipped`.
- `VisitType` includes `ServicePackageVisit` and `AdHocVisit`.
- `ServiceVisit` stores service client, business client, assignee, scheduled date, service window, status, planned services, route order, notes, visit type, visit name, invoice id, out-of-scope services/materials, completion user, completed services, and materials used.
- `MaterialUsage` stores material id and quantity.
- Completion details are stored directly on `ServiceVisit`; the separate `VisitCompletions` table is not used by the current implementation.
- Azure Table storage stores visits in `ServiceVisits` by service-client partition and visit id row key.

## Authorization Rules

- System Administrators can view and manage visits across service clients matching the current app mode.
- Business Owners can view and manage visits for their active service client.
- Business Owners can assign visits only to active business owners or employees in the same service client.
- Business Employees can view and update only visits assigned to them or completed by them.
- Business Employees cannot edit closed, canceled, or skipped visits.
- Business Clients can view their own visits and edit only Notes To Service Provider.
- Independent Home Owners have no visit scheduling access.

## Acceptance Criteria

- Implemented: Visit Scheduling is available at `/visits`, `/admin/visits`, and legacy `/schedule`.
- Implemented: The page shows Unscheduled and Scheduled, Assigned, Recently Completed, and Closed panels.
- Implemented: System Administrators see service-client columns and mode-filtered service clients.
- Implemented: Business Owners see only their service-client visits.
- Implemented: Create/Edit supports ad-hoc and service package visits.
- Implemented: Create/Edit supports business-client selection, assignee selection, visit date, visit name, status, and notes.
- Implemented: Create/Edit supports choosing multiple active services from service-client and global catalogs.
- Implemented: Closed Visits shows the Invoice ID column and displays `-` for closed visits that are not invoiced yet.
- Implemented: Visit save normalizes missing date or assignee to `New`, and date plus assignee to `Assigned`.
- Implemented: Direct status updates are limited to `InProgress`, `Completed`, and `Closed`.
- Implemented: Completed and closed visits cannot be deleted.
- Implemented: Completed visits do not receive placeholder invoice ids.
- Implemented: Field completion stores completion details on the visit and queues a completion notification.
- Implemented: Business Employees can move completed visits back to In Progress.
- Implemented: Business Clients can edit only Notes To Service Provider.
- Not implemented: Recurring schedule templates and automatic visit generation.
- Not implemented: Calendar day/week/month views.
- Not implemented: Route map and route optimization.
- Not implemented: Arrival timestamp separate from start/completion.

## Tests

- `FieldWorkTests.Completing_visit_persists_completion_and_marks_visit_completed`
- `FieldWorkTests.Company_admin_visit_save_normalizes_section_15_status_rules`
- `FieldWorkTests.Company_admin_can_assign_visit_to_business_owner`
- `FieldWorkTests.Company_admin_can_close_completed_visit_without_invoice_placeholder`
- `FieldWorkTests.Employee_dashboard_queries_split_today_upcoming_and_completed_visits`
- `FieldWorkTests.Employee_can_update_allowed_visit_details`
- `AuthorizationTests.Company_user_cannot_read_admin_dashboard`

Related tests cover dashboard visit panels, invoice eligibility for closed visits, business-client visit access, service/package data used by visits, and completion notification behavior. Component-level UI coverage for the Visit Scheduling table is still missing.

## User Documentation Impact

- Created `docs/user-guide/visit-scheduling.md` for System Administrator, Business Owner, Business Employee, and Business Client visit workflows.
- User documentation should be updated when recurring schedules, calendar views, route optimization, arrival timestamps, or additional client visit actions are added.

## Current Implementation

- `SchedulePage.razor` implements Visit Scheduling at `/visits`, `/admin/visits`, and `/schedule`.
- System Administrators load service clients filtered to the current app mode and can manage visits across those service clients.
- Business Owners resolve their active service client through `CurrentCompanyContext` and manage only that service client's visits.
- The page loads active business clients, active company admins/employees, accessible services from service-client and global catalogs, and all visits for visible service clients.
- Visits are grouped into Unscheduled and Scheduled, Assigned, Recently Completed, and Closed panels. Recently Completed and Closed panels show the 25 most recent matching visits.
- Closed Visits displays Invoice ID so administrators can see whether a closed visit is still available for invoice creation.
- `CompanyAdminService.UpsertVisitAsync` validates business client, assignee, and active planned services, then normalizes visit status.
- `CompanyAdminService.AssignVisitAsync` assigns a visit to an active business owner or employee.
- `CompanyAdminService.SetVisitStatusAsync` directly supports `InProgress`, `Completed`, and `Closed`.
- `CompanyAdminService.DeleteVisitAsync` prevents deletion of completed or closed visits.
- `FieldWorkService` supports employee start, move completed back to in progress, detail updates, and completion.
- Employee completion stores completed timestamp, completed-by user, completed service ids, materials used, notes, and status directly on `ServiceVisit`, queues a visit-completed email, and increments completed visit telemetry.
- Business Client dashboard visit notes are saved through `ClientPortalService.UpdateCurrentUserVisitServiceProviderNotesAsync`.

## Outstanding Tasks

- Implement recurring schedule templates and automatic visit generation.
- Add calendar day/week/month views if scheduling grows beyond the current grouped-table workspace.
- Add route optimization and map/navigation surfaces.
- Add arrival timestamp support if field workflows require it.
- Add explicit UI tests for `/visits` and `/admin/visits` panel/action behavior.
- Consider delete confirmations for visit deletion.
- Decide whether `/schedule` should remain as a permanent alias or redirect to `/visits`.

## Change Log

- 2026-08-04: Documented the implemented visit scheduling feature, including current code behavior, role access, acceptance criteria status, tests, user documentation, source-of-truth updates, and outstanding tasks.
- 2026-08-04: Implemented the Closed Visits Invoice ID column and added regression coverage that closing a completed visit does not create a placeholder invoice id.
