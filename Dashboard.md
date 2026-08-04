# 1. Dashboard
Once a user has successfully authenticated, they should be taken to a dashboard page. The Dashboard will be different by persona.
All Dashboard panels are collapsable.

## 1.1 System Administrator
The first row is titled System Health
A row of panels each indicating the count of entities. 
The first row of panels consist of:
- Service Clients, Business Clients, Users, Pool Equipment, Pool Configurations, Materials, Services, Service Packages
The second row is titled Approvals
The second row will be a collapsible panel of Pending approvals. Provide an approve all button.
- Columns: Name, Email, Company, User Type, Approved Toggle switch
The third row of panels is titled System Workspaces. Each panel will include a one sentence Description. Clicking on the panel will navigate to the appropriate editing page.
- Service Clients, Business Clients, Users, Pool Equipment, Pool Configurations, Materials, Services, Service Packages

## 1.2 Business Owner
The first row is titled System Health
A row of panels each indicating the count of entities. 
The first row of panels consist of:
- Business Clients, Users, Pool Configurations, Materials, Services, Service Packages

The second row is titled Approvals
The second row is titled "Pending Approvals". This panel needs to be collapsable, but expanded on open. 
It should list all approvals for the business. Provide an approve all button.
- Columns: Name, Email, User Type, Approved Toggle switch

The third row is titled "Assigned Visits Today". This panel needs to be collapsable, but expanded on open.
- Show only the visits assigned to the business owner
- Allow the ability to mark a visit complete in a simple manner
- Allow edits as described above.

The fourth row is titled "Upcoming Visits". This panel needs to be collapsable, but collapsed on open.
- Show only the visits assigned to the business owner
- No edit allowed, just view/only

The fifth row is titled "Unscheduled and Scheduled Visits". This panel needs to be collapsable, but collapsed on open.
- It needs to contain the data that is found in New Visits of /visits. This allows the business owner to quickly and conveniently assign these visits.

The sixth row of panels is titled System Workspaces. Each panel will include a one sentence Description. Clicking on the panel will navigate to the appropriate editing page.
- Business Clients, Users, Materials, Services, Service Packages
- Allow edits and deletes

## 1.3 Business Employee
The first row is titled "Assigned Visits Today". This panel needs to be collapseble, but expanded on open.
- Show only the visits assigned to the business employee
- Allow the ability to mark a visit complete in a simple manner
- Allow edits as described above.

The second row is titled "Upcoming Visits". This panel needs to be collapseble, but expanded on open.
- Show only the visits assigned to the business employee
- No edit allowed, just view/only

The first third is titled "Recently Completed Visits". This panel needs to be collapseble, but collapsed on open.
- Allow the ability to mark a visit as In Progress in a simple manner
- Allow edits as described above.

## 1.4 Business Client
The first row is titled Upcoming Visits. This panel needs to be collapseble, but expanded on open.
The individual rows are editable, however, only the Notes To Service Provider field. 
- Actions: Edit (icon)

The second row shows the Service Package. This panel needs to be collapseble, and collapsed on open. 

The third row is titled Pool Configuration. This panel needs to be collapseble, and collapsed on open. 
It will list out the current pool configuration.
Columns: Category, Manufacturer, Equipment, Comment
Actions: Info (icon) - when choosing the info action, list all aother attributes for the Pool Equipment item

The fourth row is Completed Visits. This panel needs to be collapseble, and collapsed on open. 
It will list out all Services provided to the Business Client, and the services the user added him/herself.
- List the services ordered by Date descending
- Ability to add a record - The only records that can be edited or deleted are the ones added by the Business Client himself.
  - When adding a record, pre-fill the Performed By to HomeOwner
- Columns: Date/Time, Service, Performed By, Notes
- Actions: Edit, Delete - icons

## 1.5 Independent Homeowner User
The Dashboard for the Independent HomeOwner should consist of the following collapsible panels:
Pool Equipment
Columns: Category, Manufacturer, Equipment, Comment
Actions: Info (icon) - when choosing the info action, list all aother attributes for the Pool Equipment item

Service History
- Ability to add a record
- Columns: Date/Time, Service, Notes
- Actions: Edit, Delete - icons


Current implementation:

- Authenticated users land on `/dashboard`, which shows persona-aware workspace access based on the signed-in user's company memberships or independent homeowner profile.
- System Administrators see a `/dashboard` with `System Health`, collapsible `Approvals` with approve-all support, and `System Workspaces` panels for Service Clients, Business Clients, Users, Pool Equipment, Pool Configurations, Materials, Services, and Service Packages.
- Business Owners see a `/dashboard` with `System Health` panels for Business Clients, Users, Pool Configurations, Materials, Services, and Service Packages, collapsible business-scoped `Approvals` with approve-all support, expanded `Assigned Visits Today` scoped to visits assigned to the signed-in business owner with quick complete, edit, and info actions, collapsed `Upcoming Visits` scoped to visits assigned to the signed-in business owner with view-only rows and info actions, a collapsed `Unscheduled and Scheduled Visits` panel for New/Unscheduled/Scheduled visits from the visit scheduler with inline assignment to active business employees or business owners, and `System Workspaces` panels for Business Clients, Users, Materials, Services, and Service Packages.
- Business Employees see `/dashboard` visit panels for `Assigned Visits Today`, `Upcoming Visits`, and `Recently Completed Visits`, scoped to visits assigned to the signed-in business employee. Today's visits can be marked complete from the dashboard, upcoming visits are read-only, and recently completed visits can be moved back to In Progress from the dashboard.
- Company admins can still open the focused company dashboard at `/company`.
- Business Clients see collapsible `/dashboard` panels ordered as Upcoming Visits, Service Package, Pool Configuration, and Completed Visits. Upcoming Visits is expanded by default and allows editing only Notes To Service Provider plus visit info. Service Package, Pool Configuration, and Completed Visits are collapsed by default; the Service Package panel shows the package assigned to their Business Client record, falling back to the service client package when needed, including recurrence, cost, description, and included services. Completed Visits is the visit history surface, and the duplicate Business Client Service History dashboard panel is not shown.
- Independent Homeowners with no company memberships see collapsible Pool Equipment and Service History panels on `/dashboard`; Pool Equipment shows Category, Manufacturer, Equipment, and Comment with an info action, and Service History supports add/edit/delete icon actions.


