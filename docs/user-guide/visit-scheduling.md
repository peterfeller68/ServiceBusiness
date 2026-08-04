# Visit Scheduling

Last reviewed: 2026-08-04

Visit Scheduling is where service-client administrators plan, assign, complete, close, and inspect service visits.

## Who Can Use Visit Scheduling

- System Administrators can manage visits across service clients matching the current Pool or Landscape app mode.
- Business Owners can manage visits for their own service client.
- Business Employees work their assigned visits from the Dashboard or Field page.
- Business Clients see their own upcoming and completed visits from the Dashboard.
- Independent Home Owners do not have visit scheduling access.

## Opening Visit Scheduling

- System Administrators use `/admin/visits`.
- Business Owners use `/visits`.
- `/schedule` is still available as a legacy route.

## Visit Panels

Visit Scheduling groups visits into four panels:

- Unscheduled and Scheduled Visits
- Assigned Visits
- Recently Completed Visits
- Closed Visits

The first three panels are open by default. Closed Visits is collapsed by default.

## Creating a Visit

1. Open Visit Scheduling.
2. In Unscheduled and Scheduled Visits, select Create.
3. Choose the business client.
4. Choose Ad-Hoc Visit or Service Package Visit.
5. Enter the visit name, date, assignee, status, and notes.
6. Choose one or more services from the service list.
7. Select Save Visit.

System Administrators also choose the service client before selecting the business client and services.

## Choosing Services

The service chooser includes active services from the service client and the current app mode's global catalog. Use the filter box to search by service name, description, or scope.

Chosen services appear above the chooser. Remove a chosen service with the delete action.

## Assigning Visits

Visits can be assigned to active business owners or business employees. If a visit has a date and an assignee, the system marks it Assigned. If the date or assignee is missing, the system keeps it New.

## Completing and Closing Visits

System Administrators and Business Owners can mark assigned or in-progress visits complete. Completed visits can then be closed.

Closed visits without invoice ids are available for invoice creation.

The Closed Visits panel shows Invoice ID. A dash means the closed visit has not been invoiced yet.

Completed and closed visits cannot be deleted.

## Business Employee Visit Work

Business Employees use the Dashboard or Field page for assigned visits. They can:

- Complete today's assigned visits.
- Move recently completed visits back to In Progress.
- Edit allowed visit details.
- Mark planned services complete.
- Add out-of-scope services.
- Save customer-facing, service-provider, and internal notes.

## Business Client Visit Notes

Business Clients see upcoming and completed visits on their Dashboard. They can edit only Notes To Service Provider on upcoming visits.

## Current Limitations

- Recurring schedule templates are not implemented yet.
- Calendar day/week/month views are not implemented yet.
- Route maps and route optimization are not implemented yet.
- Arrival timestamps are not tracked separately from start and completion.
