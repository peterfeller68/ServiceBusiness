# 1. Visit Scheduling Component
This section describes the Visit Scheduling Component
The Visit Scheduling Menu wil be at the Level of the Settings Menu in the Nav bar

- Visit Entity
  - Visit ID
  - Visit Type (Service Package Visit, Ad-Hoc Visit). The distinction is that the Ad-Hoc visit will be billed for, the Serive Package Visit 
    is included in the Monthly Service Package.
  - Visit Date
  - Visit Name 
  - Business Client ID
  - Assigned To (Business Employee ID)
  - Notes To Business Client
  - Notes to Service Client (Business Owner)
  - Internal Notes
  - Invoice ID
  - List of Services to be provided
  - List of out of scope services that were provided. These will be billed for separately.
  - List of out of scope materials that were used. These will be billed for separately.

- Visit Status (New, Assigned, In Progress, Complete, Closed)
  - Keep the visit Status up-to-date based on these rules.
  - When a visit is created and there is no Visit Date or it's not Assigned -> New
  - When a visit date is set and it's assigned -> Assigned
  - Allow the Status to be Set to In Progress or Complete
  - Only the Business Owner can set the visit to Closed. Only the Business Owner can edit a CLosed Visit
- Columns: Visit Type, Visit Date, Visit Name, Service Client, Business Client, Assigned To

Features: 
- The visits shall be shown in four Panels.
  - Top Panel shoes Unscheduled, and Scheduled Visits. These are incomplete as they need to be assigned to an Employee
  - Second Panel shows Assigned Visits. These are ready for execution
  - Third Panel shows recently completed visits. 
  - Fourth Panel shows Closed visits
- These visits will be at least partially populated by an automated process that looks at the Business CLients
  Service Package and when the last visit occurred.
- Provide the ability to add an ad-hoc visit by adding a Create button on the Top Panel
  - When creating a visit allow adding multiple Services from the global and business scoped services 
  - Next to each service have a choose button. When a service is chosen, add it before the list of services assigned to the visit.
    Put the save button below the chosen serices. 
    This should work the same way as it does when creating a service package.
- Allow assigning the visit to a Business Employee, or Business Owner

- Actions: Edit, Delete, Info - shown as icons 
  - The info button will show all information of the visit  

## 1.1 Sys Admin
Access to all visits
Show only visits for the service (Pool or Landscape)
Allow edits and deletes for all entities.
See section ### 1.10.1 for Dashboard impact

## 1.2 Business Owner
Show only visits for the service client
Allow edits and deletes for accessible  entities.
- Columns: Visit Type, Visit Date, Visit Name, Business Client, Assigned To
See section ### 1.10.2 for Dashboard impact

## 1.3 Business Employee
Actions: Complete Visit (icon), Edit Visit (icon), Info (icon)

The following edits are available:
- Notes To Business Client
- Notes to Service Client (Business Owner)
- Internal Notes
- Ability to set each service in the visit as complete or not complete
- Ability to add other services that were done. These are outside the scope of the service package visit and will be billed for separately.

When a Business Employee edits a visit to add additional services to a visit, the page should work the same way as when a 
business owner creates a visit. The service can be chosen with a check mark, and the Save Visit button is below the chosen 
services.

See section ### 1.10.3 for Dashboard impact

## 1.4 Business Client
Actions: Edit Visit (icon), Info (icon)
A Business client can only update the Notes To Service Provider field. 
See section ### 1.10.4 for Dashboard impact

## 1.5 Independent Home Owner
No Access

Current implementation:

- Visit scheduling is implemented on `/visits` for Business Owners, `/admin/visits` for System Administrators, and the legacy `/schedule` route. The navigation includes a top-level `Visit Scheduling` section alongside Settings; it is visible to System Administrators, Business Owners, and Business Employees only. Independent Home Owners and Business Clients do not receive the scheduling menu.
- Visits carry the Section 15 visit metadata: visit type, visit date, visit name, business client, assigned employee/owner, notes to the business client, notes to the service client, internal notes, invoice id, planned services, completed planned services, and out-of-scope service/material collections. Visit status is normalized from the saved visit data: no visit date or no assigned user is `New`, a visit date with an assigned user is `Assigned`, and `In Progress`, `Complete`, and `Closed` can be set explicitly. Completing a visit no longer assigns a placeholder invoice id; completed visits remain available for invoice creation until the invoicing service creates an invoice.
- The Visit Scheduling page shows the required four panels: Unscheduled and Scheduled Visits, Assigned Visits, Recently Completed Visits, and Closed Visits. The create/edit form supports ad-hoc and package visits, business-client selection, assignment to active business employees or business owners, status updates through the Section 15 statuses, and service-client/global services. Services are chosen through a filtered Choose Services grid with icon-only Choose actions; chosen services appear above the chooser, can be removed, and the Save/Cancel buttons sit directly below the Chosen Services section.
- System Administrators see service-client columns and service-type-filtered service clients. Business Owners see only their own service client and omit the service-client column. Both roles can edit visits, delete incomplete non-closed visits, close completed visits, and open the icon-driven info dialog for the full visit detail set.
- Business Owners see visits assigned to the signed-in owner on `/dashboard` in an expanded `Assigned Visits Today` panel with quick complete, edit, and info actions, and in a collapsed, view-only `Upcoming Visits` panel; New/Unscheduled/Scheduled visits remain in the collapsed `Unscheduled and Scheduled Visits` panel with inline assignment to active business employees or business owners. `/visits` remains the detailed scheduling workspace. Business Employees see collapsible `/dashboard` panels scoped to visits assigned to the signed-in employee for today's assigned visits, future assigned visits, and recently completed visits. Today's and upcoming panels are expanded by default, while recently completed is collapsed by default. Today's visits can be completed from the dashboard, upcoming visits are view-only, and completed visits can be moved back to In Progress. Employee edit actions support Notes To Business Client, Notes To Service Client, Internal Notes, completed planned-service selection, and added out-of-scope services; visit info actions show the full visit detail set. `/field` remains the detailed route execution workspace. Completing a visit updates the `ServiceVisit` status and stores completion details directly on the visit, including completed timestamp, completed-by user, completed service ids, material usage, notes, and invoice id; the separate `VisitCompletions` table is no longer used. Business Clients see Upcoming Visits and Completed Visits panels on `/dashboard`, with completed and closed visits shown only in Completed Visits; the separate Business Client Service History dashboard panel has been removed because it duplicated Completed Visits. The only editable Business Client visit field is Notes To Service Provider, saved back to the visit's service-client notes. Independent Home Owners have no visit scheduling access.
