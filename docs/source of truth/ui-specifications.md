# UI Specifications

## 1. Global UI Requirements

The application must be responsive and usable on desktop, tablet, and mobile browsers. Field-user and client-user workflows should be optimized for mobile first.

Global layout:

- Authenticated shell with role-aware navigation.
- Company selector when a user belongs to more than one company.
- Persona-specific dashboard as the default landing page after sign-in.
- Clear pending approval and incomplete setup indicators.
- Consistent form validation.
- Toast or inline feedback for save, error, and background-job states.

Global screens:

- Sign In
- First-Time Profile Setup
- Company Selection
- Access Pending
- Unauthorized
- Account Profile

## 2. Sign In Screen

Persona:

- All users.

Purpose:

- Authenticate using Google.

UI elements:

- Product name.
- Sign in with Google button.
- Test-user email field for seeded development accounts.
- Skip Google Auth button for test users only.
- Short support/contact link.

Behavior:

- Redirect to Google Authentication.
- On successful authentication, create or update user profile.
- Route user to persona dashboard or onboarding flow.
- Test users marked `IsTestUser` may bypass Google authentication and route through the test sign-in endpoint.

Current implementation:

- `/register` includes Business Owner, Business User, Business Client, and Independent Homeowner choices.
- Independent Homeowner registration captures account contact fields, home address, and access notes, and does not show the business association selector.
- Successful Independent Homeowner registration routes to `/poolequipment`.

## 3. First-Time Profile Setup Screen

Persona:

- All users.

Purpose:

- Capture missing application profile details.

Fields:

- Display name.
- Phone.
- Preferred contact email, defaulted from Google.
- Email notifications enabled toggle.

Actions:

- Save profile.
- Continue.

Current implementation:

- `/profile` provides the current account profile form after sign-in.
- The user can edit display name, notification email, phone, and email notification preference.
- Login email is shown as read-only.
- Logout is available from the profile screen and top-right shell profile area.

## 4. System Administrator Screens

### 4.1 System Admin Dashboard

Purpose:

- Provide a mode-filtered platform overview and approval workspace.

Content:

- System Health metrics for Service Clients, Business Clients, Users, Materials, Services, and Service Packages.
- Pool Equipment and Pool Configurations metrics in Pool mode.
- Collapsible Approvals panel with pending requester name, email, company, user type, and approval toggle.
- System Workspaces links for focused admin pages.
- Application health and telemetry summary when Application Insights data is surfaced in a future dashboard slice.
- Recent platform audit events when audit events are surfaced in a future dashboard slice.
- Stripe/payment health summary when payment processing is surfaced in a future dashboard slice.

Actions:

- Open focused admin workspaces.
- Approve a pending access request using the approval toggle.
- Approve all pending access requests shown in the mode-filtered dashboard.

### 4.2 Company Types List

Purpose:

- Manage available tenant business categories.

Columns:

- Name.
- Description.
- Active status.
- Sort order.
- Last updated.

Actions:

- Add company type.
- Edit company type.
- Archive company type.
- Reactivate company type.

### 4.3 Company Type Form

Purpose:

- Create or edit company type.

Fields:

- Name.
- Description.
- Active.
- Sort order.

Actions:

- Save.
- Cancel.

Validation:

- Name is required.
- Name must be unique among active company types.

### 4.4 Companies List

Purpose:

- View and manage SaaS customer companies.

Filters:

- Company type.
- Status.
- Search by name or email.

Columns:

- Company name.
- Company type.
- Primary email.
- Status.
- Stripe status.
- Created date.

Actions:

- Add company.
- View company.
- Edit company.
- Suspend.
- Reactivate.

### 4.5 Company Detail

Purpose:

- View tenant summary from the platform perspective.

Sections:

- Company profile.
- Company admins.
- User count.
- Client count.
- Billing status.
- Recent audit events.

Actions:

- Edit company.
- Assign initial admin.
- Suspend or reactivate.

### 4.6 Create/Edit Company

Purpose:

- Create or update a SaaS customer company.

Fields:

- Company name.
- Legal name.
- Company type.
- Business email.
- Business phone.
- Website.
- Address.
- Time zone.
- Status.

Actions:

- Save.
- Cancel.

### 4.7 Assign Company Admin

Purpose:

- Assign or invite the first Company Admin.

Fields:

- Email.
- Display name.
- Invitation message.

Actions:

- Send invite.
- Assign existing user.

## 5. Company Admin Screens

### 5.1 Company Admin Dashboard

Purpose:

- Give the business owner a focused setup, approval, and visit-triage workspace.

Content:

- System Health metrics for Business Clients, Users, Materials, Services, and Service Packages.
- Pool Configurations metric in Pool mode.
- Collapsible Approvals panel with pending requester name, email, user type, and approval toggle.
- Assigned Visits Today panel scoped to visits assigned to the signed-in business owner.
- Upcoming Visits panel scoped to future visits assigned to the signed-in business owner.
- Unscheduled and Scheduled Visits panel for New, Unscheduled, and Scheduled visits.
- System Workspaces links for focused business pages.

Actions:

- Open focused editor pages from health metrics and workspace links.
- Approve a pending access request using the approval toggle.
- Approve all pending access requests.
- Complete assigned visits for today.
- Edit allowed visit details.
- View full visit information.
- Assign unscheduled and scheduled visits to active business owners or employees.

### 5.2 Company Profile and Settings

Purpose:

- Maintain company configuration.

Sections:

- Business profile.
- Logo.
- Contact information.
- Time zone.
- Service area.
- Email notification settings.
- Service completion email template.
- Self-service settings.

Actions:

- Save.
- Upload logo.
- Send test email.

### 5.3 Company Users List

Purpose:

- Manage employees and admins.

Filters:

- Role.
- Status.
- Search by name or email.

Columns:

- Name.
- Email.
- Role.
- Status.
- Last login.

Actions:

- Invite user.
- Edit role.
- Deactivate.
- Reactivate.
- Remove.

Current implementation:

- `/company/users` is a focused company user-management page.
- The page shows active, inactive, and removed company memberships plus pending access requests.
- Pending access and company access are presented as collapsible table panels with right-aligned row actions.
- Company admins can approve/reject pending requests, update the company-scoped role for an approved user, deactivate company access, and reactivate company access.
- The page does not show system-admin promotion, global account disablement, company editor, role-definition editor, catalog editor, or email-log controls.
- Global account enable/disable remains on `/admin/users`; company-user deactivate/reactivate changes only the company membership status.

### 5.4 Invite Company User

Purpose:

- Invite an employee or admin.

Fields:

- Email.
- Role.
- Invitation message.

Actions:

- Send invite.
- Cancel.

### 5.5 Pending Employee Requests

Purpose:

- Approve or reject employee requests to join the company.

Columns:

- Name.
- Email.
- Requested date.
- Requested role if applicable.

Actions:

- Approve as Company User.
- Approve as Company Admin.
- Reject.

### 5.6 Clients List

Purpose:

- Manage serviced homeowners/properties.

Filters:

- Status.
- Client type.
- Search by name, email, phone, or address.

Columns:

- Client name.
- Service address.
- Client type.
- Assigned default user if implemented.
- Status.
- Next scheduled visit.

Actions:

- Add client.
- View client.
- Edit client.
- Deactivate.

### 5.7 Client Detail

Purpose:

- View and manage one company client.

Sections:

- Contact information.
- Billing information.
- Service address.
- Property and access notes.
- Client users.
- Upcoming visits.
- Service history.
- Billing history.
- Messages.

Actions:

- Edit client.
- Add schedule.
- Invite client user.
- Create message.
- View report filtered to client.

### 5.8 Create/Edit Client

Purpose:

- Create or update a serviced client/property.

Fields:

- Client display name.
- Primary contact name.
- Email.
- Phone.
- Billing address.
- Service address.
- Property notes.
- Access notes.
- Preferred service days.
- Client type.
- Rate override.
- Notification preferences.
- Active status.

Actions:

- Save.
- Cancel.
- Geocode address if maps are enabled.

### 5.9 Pending Client User Requests

Purpose:

- Approve or reject homeowner access requests.

Columns:

- User name.
- Email.
- Requested client.
- Requested date.
- Match confidence if self-service matching is implemented.

Actions:

- Approve.
- Link to different client.
- Reject.

### 5.10 Client Types List

Purpose:

- Manage reimbursement models and default rates.

Columns:

- Name.
- Billing frequency.
- Default rate.
- Active status.

Actions:

- Add client type.
- Edit.
- Archive.
- Reactivate.

### 5.11 Client Type Form

Fields:

- Name.
- Description.
- Billing frequency.
- Default rate.
- Currency.
- Active.

Actions:

- Save.
- Cancel.

Validation:

- Name is required.
- Billing frequency is required.
- Default rate must be zero or greater.

### 5.12 Services List

Purpose:

- Manage service catalog.

Columns:

- Category.
- Name.
- Default duration.
- Default price.
- Taxable.
- Active.

Actions:

- Add service.
- Edit.
- Archive.
- Reactivate.

### 5.13 Service Form

Fields:

- Category.
- Name.
- Description.
- Default duration.
- Default price.
- Taxable.
- Active.
- Sort order.

Actions:

- Save.
- Cancel.

### 5.14 Materials List

Purpose:

- Manage material catalog.

Columns:

- Category.
- Name.
- Unit of measure.
- Default unit cost.
- Default billable price.
- Taxable.
- Active.

Actions:

- Add material.
- Edit.
- Archive.
- Reactivate.

### 5.15 Material Form

Fields:

- Category.
- Name.
- Description.
- Unit of measure.
- Default unit cost.
- Default billable unit price.
- Taxable.
- Active.
- Sort order.

Actions:

- Save.
- Cancel.

Current implementation:

- `/catalog` displays services and materials grouped by category cards.
- `/catalog/materials` and `/catalog/services` provide focused company catalog editors.
- `/admin/catalog/materials` and `/admin/catalog/services` provide focused system-admin catalog editors for the seeded company catalog in this slice.
- Focused catalog editors use collapsible table panels for category and item lists.
- Create buttons expand empty inline editor panels; Edit actions expand the same editor panels populated from the selected row.
- Focused catalog editors include archive/reactivate actions.
- System/starter categories are visually marked.
- System/starter categories and items expose copy-as-custom actions that create editable records in the current scope.
- Empty categories and legacy uncategorized rows have explicit states.

### 5.16 Schedule Calendar

Purpose:

- View and manage service visits in grouped status panels.

Routes:

- `/visits` for Business Owners.
- `/admin/visits` for System Administrators.
- `/schedule` as a legacy route.

Panels:

- Unscheduled and Scheduled Visits.
- Assigned Visits.
- Recently Completed Visits.
- Closed Visits.

Content:

- Visit Type.
- Visit Date.
- Visit Name.
- Service Client for System Administrators.
- Business Client.
- Assigned To.
- Status.
- Invoice ID for Closed Visits.
- Icon action column.

Actions:

- Create one-time visit.
- Edit visit.
- Complete assigned or in-progress visit.
- Close completed visit.
- Show visit information.
- Delete incomplete, non-closed visit.

Current limitations:

- Calendar day/week/month views are not yet implemented.
- Recurring schedule creation is not yet implemented.

### 5.17 Create/Edit Visit

Purpose:

- Create or update one scheduled visit.

Fields:

- Service client for System Administrators.
- Visit type.
- Visit name.
- Business client.
- Scheduled date.
- Assigned user.
- Status.
- Notes To Business Client.
- Notes To Service Client.
- Internal Notes.
- Chosen planned services.
- Service filter and service chooser.

Actions:

- Save.
- Cancel.
- Choose service.
- Remove chosen service.

### 5.18 Create/Edit Recurring Schedule

Purpose:

- Configure recurring visits in a future scheduling slice.

Fields:

- Client.
- Assigned user.
- Start date.
- End date.
- Recurrence type.
- Interval.
- Days of week.
- Day of month.
- Service window.
- Planned services.
- Notes.

Actions:

- Save.
- Generate upcoming visits.
- Cancel.

Current limitations:

- Recurring schedule creation and automatic visit generation are not yet implemented.

### 5.19 Assignment Board

Purpose:

- Assign scheduled client visits to company users in a future scheduling slice.

Layout:

- Date picker.
- Unassigned visits list.
- User columns or grouped lists.
- Visit cards with client name, address, window, and status.

Actions:

- Assign to user.
- Reassign.
- Clear assignment.
- Open visit detail.

Current implementation:

- Assignment is available from Visit Scheduling edit forms and Business Owner dashboard inline assignment controls.

### 5.20 Visit Detail

Purpose:

- Review scheduled or completed visit.

Sections:

- Client.
- Address.
- Assigned user.
- Schedule.
- Status.
- Services planned.
- Services performed.
- Materials used.
- Notes.
- Completion timestamps.
- Email notification status.

Actions:

- Edit visit.
- Reassign.
- Complete visit.
- Close visit.
- Delete incomplete, non-closed visit.

Current limitations:

- Resend completion email is not yet implemented.

### 5.21 Reports

Purpose:

- Generate operational and billing reports.

Report types:

- Completed visits.
- Scheduled visits.
- Revenue summary.
- Materials usage.
- User productivity.
- Client service history.
- Billing and payment summary.

Filters:

- Date range preset.
- Custom date range.
- User.
- Client.
- Service.
- Material.
- Visit status.

Actions:

- Run report.
- Export CSV.

### 5.22 Billing Dashboard

Purpose:

- View and manage invoices for completed service visits.

Content:

- Create Invoice selector for closed visits without valid invoice records.
- Collapsible New Invoices panel.
- Collapsible Invoiced Invoices panel.
- Collapsible Paid Invoices panel.
- Invoice columns for Invoice ID, Business Client, Invoice Date, Paid Date, and Cost.

Actions:

- Create invoice.
- Mark New invoice as Invoiced.
- Mark Invoiced invoice as Paid.
- View generated invoice HTML as a rendered preview.
- Show invoice detail.
- Delete invoice.

System Administrator differences:

- Uses `/admin/invoices`.
- Shows invoices only for service clients matching the current application mode.
- Adds Invoice GUID and Service Client columns.

Business Owner differences:

- Uses `/invoices`.
- Shows and manages invoices for the active service client.
- The Create Invoice panel remains available when no invoice records exist yet.

### 5.23 Messages Inbox

Purpose:

- Manage homeowner messages.

Filters:

- Open.
- Pending.
- Closed.
- Client.

Columns:

- Client.
- Subject.
- Last message.
- Status.
- Last updated.

Actions:

- Open thread.
- Reply.
- Close thread.

## 6. Standard Company User Screens

### 6.1 Field User Dashboard

Purpose:

- Show the signed-in business employee's assigned work.

Content:

- Assigned Visits Today panel.
- Upcoming Visits panel.
- Recently Completed Visits panel.
- Visit rows with client, date, status, service window, and action controls.

Actions:

- Mark today's assigned visit complete.
- Move recently completed visit back to In Progress.
- Edit allowed visit details.
- View full visit information.

### 6.2 My Assigned Visits

Purpose:

- List all assigned visits for a date.

Content:

- Client name.
- Service address.
- Scheduled window.
- Visit status.
- Planned services.

Actions:

- Open visit.
- Start visit.
- Open navigation.

### 6.3 Route View

Purpose:

- Help the field user service clients efficiently.

Content:

- Ordered stop list.
- Map if maps are enabled.
- Distance and duration estimate if available.

Actions:

- Optimize route.
- Manually reorder.
- Open navigation for stop.
- Open visit.

### 6.4 Field Visit Detail

Purpose:

- Show visit information needed on site.

Content:

- Client name.
- Service address.
- Contact phone.
- Access notes.
- Property notes.
- Planned services.
- Prior customer-visible notes if allowed.

Actions:

- Start visit.
- Mark arrived.
- Complete visit.
- Open navigation.

### 6.5 Complete Visit

Purpose:

- Record work performed.

Fields:

- Services performed dropdown or checklist.
- Materials used dropdown with quantity.
- Completion notes.
- Customer-visible note toggle.
- Internal note field if supported separately.

Actions:

- Save draft.
- Complete visit.
- Cancel.

Validation:

- At least one service or note should be required to complete a visit.
- Material quantity must be greater than zero.

### 6.6 Visit Completion Confirmation

Purpose:

- Confirm successful completion.

Content:

- Completion timestamp.
- Email notification status.
- Next assigned visit.

Actions:

- Open next visit.
- Return to route.
- Return to assigned visits.

### 6.7 Join Company Request

Purpose:

- Let an employee request access to a company.

Fields:

- Company invite code or company search.
- Message to admin.

Actions:

- Submit request.
- Cancel.

## 7. Company Client User Screens

### 7.1 Client Dashboard

Purpose:

- Show the signed-in business client's service overview.

Content:

- Upcoming Visits panel.
- Service Package panel.
- Pool Configuration panel in Pool mode.
- Completed Visits panel.

Actions:

- Edit Notes To Service Provider on upcoming visits.
- View visit information.
- View Pool equipment information in Pool mode.

Current limitations:

- Open message summaries and outstanding payment summaries are not yet shown on the dashboard.

### 7.2 My Services

Purpose:

- Display service history.

Filters:

- Date range.
- Service type.

Content:

- Service date.
- Services performed.
- Materials shown only if company allows customer visibility.
- Customer-visible notes.

Actions:

- Open service detail.

### 7.3 Service Detail

Purpose:

- Show details from a completed visit.

Content:

- Date completed.
- Services performed.
- Customer-visible materials.
- Customer-visible notes.
- Company contact option.

Actions:

- Message company about this service.

### 7.4 Bills and Payments

Purpose:

- Show business-client invoice history.

Content:

- Invoice list.
- Status.
- Amounts.
- Invoice date.
- Paid date.

Actions:

- Open invoice.

Current limitations:

- Stripe-hosted payment links are not yet implemented.

### 7.5 Message Threads

Purpose:

- View homeowner-company conversations.

Content:

- Thread subject.
- Last message preview.
- Status.
- Last updated.

Actions:

- Open thread.
- Create new message.

### 7.6 Message Thread Detail

Purpose:

- Read and send messages.

Content:

- Message history.
- Sender.
- Timestamp.

Fields:

- Reply text.

Actions:

- Send reply.

### 7.7 Request Client Access

Purpose:

- Let a homeowner request access to their service account.

Fields:

- Company invite code or company search.
- Service address.
- Contact email.
- Contact phone.
- Message to company.

Actions:

- Submit request.
- Cancel.

### 7.8 Client Profile Preferences

Purpose:

- Let homeowner maintain communication preferences.

Fields:

- Phone.
- Preferred email.
- Service completion email enabled.
- Billing email enabled.

Actions:

- Save.

## 8. Navigation by Persona

### 8.1 System Admin Navigation

Items:

- Dashboard
- Settings
- Settings / Companies
- Settings / Users
- Settings / Roles
- Settings / Catalog
- Settings / Catalog / Pool Equipment
- Settings / Catalog / Materials
- Settings / Catalog / Services
- Reports
- Logs / Email Log
- Help

### 8.2 Company Admin Navigation

Items:

- Dashboard
- Settings
- Settings / Customers
- Settings / Users
- Settings / Catalog
- Settings / Catalog / Pool Equipment
- Settings / Catalog / Materials
- Settings / Catalog / Services
- Reports
- Logs / Email Log
- Help

### 8.3 Standard Company User Navigation

Items:

- Dashboard
- Reports
- Help

### 8.4 Company Client User Navigation

Items:

- Home
- Dashboard
- Settings
- Reports
- Logs / Email Log
- Help

### 8.5 Independent Home Owner Navigation

Items:

- Dashboard
- Settings
- Settings / Catalog / Pool Equipment
- Reports
- Logs / Email Log
- Help

## 9. Responsive Design Requirements

Desktop:

- Use left navigation or top navigation with clear active state.
- Tables may show multiple columns.
- Admin schedule can use calendar and assignment board views.

Mobile:

- Use bottom navigation or compact menu for field and client users.
- Replace wide tables with cards or stacked rows.
- Keep visit actions reachable with large touch targets.
- Route and completion screens should require minimal typing.
- Avoid dense admin-only screens as primary mobile workflows, but they must remain usable.

## 10. Empty, Loading, and Error States

Every list screen must define:

- Loading state.
- Empty state.
- Error state.
- No-results state when filters return nothing.

Examples:

- No clients have been created.
- No visits assigned for this date.
- No reports match the selected filters.
- This account is waiting for company admin approval.

## 11. Permission-Based UI Rules

- Hide System Admin navigation from non-System Admins.
- Hide company configuration from Standard Company Users.
- Hide internal notes from Company Client Users.
- Disable visit completion for users not assigned to the visit unless the user is a Company Admin.
- Prevent client users from seeing other client accounts.
- Show pending approval status when a membership is not active.

Current implementation:

- The navigation menu filters authenticated role links from the current user's active memberships and system-admin flag.
- Authenticated navigation groups supported leaf links under collapsible Settings and Logs sections.
- Unauthenticated users see only Home and Help.
- Authenticated users do not see the Home link; Dashboard remains the signed-in workspace entry point.
- The top-right profile indicator shows the logged-in user's display name with their current role label underneath, and opens the profile page.
- Dashboard pages show the current system-mode hero image.
- Persisted `SystemSettings.SystemMode` controls product branding and imagery: Pool mode shows `PoolShark` with the pool waterfall image, while Landscape mode shows `TreeShark` with the mature fruit-tree landscape image.
- The System Admin General Settings page includes a SystemMode selector with `Pool` and `Landscape` values; saving the selector updates branding, hero imagery, and Pool Equipment visibility after the page refreshes.
- Settings, Reports, and Help have stable routes; reporting workflows remain future slices.
- System-admin leaves route to focused pages: `/admin/companies`, `/admin/users`, `/admin/roles`, `/admin/catalog/poolequipment`, `/admin/catalog/materials`, `/admin/catalog/services`, and `/admin/email-log`.
- Company-admin leaves route to focused pages: `/clients`, `/company/users`, `/catalog/poolequipment`, `/catalog/materials`, `/catalog/services`, and `/logs/email`.
- Business Client and Independent Home Owner Logs navigation routes to `/logs/email`.
- In Pool mode, company-client users with homeowner equipment access route to `/poolequipment`.
- In Pool mode, Independent Home Owner users have no company memberships and route to `/poolequipment` for owner-scoped pool equipment management.
- Landscape mode hides Pool Equipment navigation and redirects direct Pool Equipment routes back to Dashboard.
- `/admin/companies`, `/admin/users`, `/catalog/poolequipment`, `/catalog/materials`, `/catalog/services`, `/admin/catalog/poolequipment`, `/admin/catalog/materials`, and `/admin/catalog/services` expose create/edit/archive-reactivate workflows appropriate to their models.
- Data-management pages use collapsible management panels with table rows and right-aligned actions by default.
- Add/Create and Edit actions expand inline editor panels within the relevant management panel.
- `/admin/roles` edits the built-in role definitions; adding arbitrary role identities remains out of scope while roles are represented by the fixed `CompanyRole` enum.
- Pool-equipment editor pages expose category/item forms, active/archive controls, scope labels, and image URL thumbnails without showing material or service editor controls.
- Catalog editor pages expose copy-as-custom controls for starter records without mixing unrelated editor controls onto the focused page.

## 12. Implementation Prompt Guidance

When feeding this UI spec into Codex, implement in vertical slices:

1. Authentication shell and role-aware navigation.
2. System Admin company type and company management.
3. Company Admin company setup, users, clients, services, and materials.
4. Scheduling and assignment.
5. Field-user daily visits and visit completion.
6. Client-user service history and messages.
7. Billing and reports.
