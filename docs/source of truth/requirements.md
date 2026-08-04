# Service Business SaaS Requirements

## 1. Product Summary

Build a multi-tenant SaaS application for small pool cleaning and landscaping businesses. The SaaS operator manages platform-level configuration. Each subscribed company manages its own staff, clients, services, materials, schedules, billing settings, field work, reports, and client communication.

The application will be hosted on Azure, implemented with Blazor, authenticated through Google Authentication, and persisted in Azure Storage. Stripe will be used for payment collection and billing history.

## 2. Core Personas

### 2.1 System Administrator

The System Administrator manages the SaaS platform itself.

Responsibilities:

- Manage company types, such as Pool Cleaning Service and Landscaping Service.
- Manage SaaS companies, meaning the businesses subscribed to the platform.
- View platform health, tenant counts, active users, pending onboarding requests, and billing status.
- Configure platform-level defaults used when creating a new company.
- Disable or suspend companies when needed.

### 2.2 Company Admin

The Company Admin is usually the business owner or office manager for a pool cleaning or landscaping company.

Responsibilities:

- Configure company profile, contact information, operating areas, service settings, notification settings, and billing settings.
- Manage users who belong to the company.
- Approve or reject employee requests to join the company.
- Approve or reject homeowner client requests.
- Create and maintain client records.
- Configure client types and reimbursement models.
- Manage offered services.
- Manage materials and supplies.
- Manage recurring and one-time schedules.
- Assign scheduled client visits to company users.
- Review completed service visits.
- View reports by date range, user, client, service, material, and revenue.
- Manage Stripe payment setup and company-client billing relationships.

### 2.3 Standard Company User

The Standard Company User is usually a field technician, pool cleaner, landscaper, crew member, or route worker.

Responsibilities:

- View assigned client visits for a selected date.
- Use the application primarily from a mobile device.
- See client contact information, address, service notes, gate/access notes, and scheduled work.
- Generate or view an optimized route for assigned visits.
- Start, update, and complete service visits.
- Record services performed.
- Record materials used.
- Add free-form notes.
- Optionally upload photos if implemented in a later phase.
- Trigger service-completed client email when work is completed.

### 2.4 Company Client User

The Company Client User is usually a homeowner or property owner receiving service.

Responsibilities:

- View their service history.
- View upcoming scheduled service dates.
- View bills, invoices, and payment history.
- Send questions or messages to the company.
- Maintain basic contact preferences.

### 2.5 Independent Home Owner

The Independent Home Owner is a homeowner who is not associated with a service company tenant.

Responsibilities:

- Register without selecting a company.
- Provide home address and access notes.
- Manage owner-scoped pool equipment categories and equipment items.
- Maintain personal pool equipment records.
- Use homeowner tools without requiring company-owner approval.

## 3. Tenant and Role Model

The application must be multi-tenant.

Definitions:

- Platform: The SaaS application operated by the System Administrator.
- Company: A tenant/customer of the SaaS platform.
- Company User: A user who works for a company.
- Company Client: A customer/property serviced by a company.
- Company Client User: A homeowner user account associated with one or more company client records.

Rules:

- Every company-owned record must include `CompanyId`.
- Users may have memberships in one or more companies.
- A user may have different roles in different companies.
- System Administrators have platform-level access and are not scoped to one company.
- Company Admins can only access data for companies where they have the CompanyAdmin role.
- Standard Company Users can only access assigned visits and company data needed to complete their assigned work.
- Company Client Users can only access client-facing data tied to their approved client memberships.
- Disabled users cannot sign in or complete authorized operations.

## 4. Authentication and Authorization

### 4.1 Authentication

- Use Standard Google Authentication for sign-in.
- Store external Google subject identifier as the stable login key.
- Capture user email, display name, and profile image when available.
- First sign-in creates an application user profile if one does not already exist.
- Test users may be marked with `IsTestUser` and can bypass Google authentication only through a dedicated test sign-in path.
- Real, non-test users must authenticate through Google.

### 4.2 Authorization

Use role-based and tenant-scoped authorization.

Platform roles:

- `SystemAdmin`

Company roles:

- `CompanyAdmin`
- `CompanyUser`
- `CompanyClientUser`

Independent Home Owner access:

- Independent Home Owner users are active application users without company memberships.
- Owner-scoped features must validate the current user and use the user's ID as the owner scope.
- Independent Home Owner access does not grant company-scoped or platform-scoped permissions.

Role definitions:

- Store role key, display name, description, owner-approval requirement, and permissions.
- System Administrators can edit built-in role metadata and permission lists.
- Current runtime authorization uses the fixed company role keys; permission-level enforcement can be expanded as more workflows are implemented.

Authorization requirements:

- Every request must validate the authenticated user.
- Every company-scoped operation must validate the user's membership and role for that company.
- Every client-scoped operation must validate the user's access to that company client.
- Field users cannot modify company configuration.
- Client users cannot view internal company notes, employee notes marked internal, route details for other clients, or data for other homeowners.

## 5. Company Management Requirements

### 5.1 Company Types

System Admins can:

- Create, edit, archive, and view company types.
- Define display name and description.
- Mark a company type active or inactive.

Default types:

- Pool Cleaning Service
- Landscaping Service

### 5.2 Companies

System Admins can:

- Create a company.
- Edit company profile.
- Assign company type.
- Assign initial Company Admin.
- Activate, deactivate, or suspend a company.
- View company billing status.

Company Admins can maintain:

- Company name.
- Business address.
- Phone.
- Email.
- Website.
- Time zone.
- Service area description.
- Logo.
- Client email notification settings.
- Default service completion email template.
- Stripe connection status.

## 6. User and Membership Requirements

### 6.1 User Profile

The app stores:

- User ID.
- Google subject ID.
- Email.
- Notification email.
- Email notifications enabled flag.
- Display name.
- Phone.
- Profile image URL.
- Test user flag.
- User status, including `Active` and `Disabled`.
- Created date.
- Last login date.
- Global status.

Current implementation:

- The profile page at `/profile` lets signed-in users update display name, notification email, and phone.
- Signed-in users can turn email notifications on or off from `/profile`; existing users without the stored flag are treated as opted in.
- Login email remains read-only on the profile page because it is tied to Google/test-user identity lookup.
- The authenticated shell shows a profile indicator that opens `/profile`; logout is available from the profile page.
- Public navigation shows only Home and Help when no user is signed in.
- Signed-in navigation is filtered by active company role and system-admin status.
- Signed-in navigation uses collapsible sections with distinct focused routes for visible leaf items.

### 6.1.1 System Admin User Management

System Admins can:

- View all users.
- View each user's system-admin flag, test-user flag, global status, company roles, and account approval status.
- Promote an already registered user to System Admin.
- Remove System Admin privileges from a user when at least one other active System Admin remains.
- Disable or enable users.
- View and edit built-in role definitions and their permission lists.

Rules:

- The first System Admin may be seeded during application initialization.
- A System Admin cannot disable their own account.
- The system must keep at least one active System Admin.
- Disabled users cannot sign in or pass authorization checks.

Current implementation:

- `/admin` is the platform summary dashboard.
- `/admin/companies`, `/admin/users`, `/admin/roles`, and `/admin/email-log` provide focused pages for company, user, role, and email-log management.
- `/admin/companies` supports company create, edit, suspend, archive, and reactivate.
- `/admin/users` supports user create, edit, system-admin promotion/removal, disable, and enable.
- `/admin/roles` supports editing built-in role display metadata, permissions, and owner-approval requirements; role identities remain fixed to the built-in company role keys.
- Focused admin data editors use collapsible table panels with right-aligned row actions; Create and Edit actions expand inline editor panels.

### 6.2 Company User Management

Company Admins can:

- Invite users by email.
- View active, pending, rejected, inactive, and removed users.
- Approve employee join requests.
- Reject employee join requests.
- Assign roles.
- Deactivate a user within the company.
- Reactivate a deactivated company user.

Standard Company Users can:

- Request to join a company using invite code, company code, or invitation link.
- View the status of their request.

Current implementation:

- `/company/users` shows company users and pending employee/client-user access requests for the seeded company scope.
- `/company/users` uses collapsible table panels for pending access and company access management.
- Company admins can approve or reject pending company access requests.
- Company admins can deactivate and reactivate approved company memberships.
- Company admins can update company-scoped roles by removing the previous membership role and activating the replacement role.
- Company admins cannot deactivate or reassign their own Company Admin access, and at least one active Company Admin must remain.
- Global account disablement remains a System Admin action on `/admin/users`.

### 6.2.1 Independent Home Owner Registration

Current implementation:

- `/register` includes an Independent Homeowner account type.
- Independent Homeowner registration creates or updates an active `AppUser` without creating a company membership.
- Independent Homeowner registration captures and persists home address and access notes in an owner profile.
- Independent Homeowner registration seeds owner-scoped pool equipment records under `EquipmentScope.HomeOwner` for the new user ID.
- Independent Homeowner users can open `/poolequipment` immediately without owner approval.

### 6.3 Company Client User Management

Company Admins can:

- Invite homeowner users to access a company client account.
- Approve homeowner access requests.
- Reject homeowner access requests.
- Link one client user to one or more company client records.
- Remove a client user's access.

Company Client Users can:

- Request access to their service account.
- View pending request status.

## 7. Company Client Requirements

Company Admins can create and maintain company clients.

Client fields:

- Client name.
- Primary contact name.
- Email.
- Phone.
- Billing address.
- Service address.
- Property notes.
- Access notes.
- Gate code or access instruction.
- Preferred service days.
- Client type.
- Standard rate override.
- Active status.
- Taxable status.
- Notification preferences.

Rules:

- A company client belongs to exactly one company.
- A company client can have multiple client users.
- A company client can have multiple service locations in a future phase, but phase one may model one service address per client.
- Inactive clients should not be scheduled unless explicitly reactivated.

## 8. Client Type and Pricing Requirements

Company Admins can configure client types.

Supported reimbursement models:

- Fee For Service
- Weekly Service
- Bi-Weekly Service
- Monthly Service

Client type fields:

- Name.
- Description.
- Billing frequency.
- Default rate.
- Currency.
- Active status.

Rules:

- Default rate can be overridden per company client.
- Fee-for-service billing is based on completed services and materials.
- Recurring weekly, bi-weekly, and monthly billing may use a standard recurring amount, with optional additional charges for materials or one-off services.

## 9. Services, Materials, and Pool Equipment Requirements

### 9.1 Services

Company Admins can:

- View services grouped by service category.
- Create, edit, archive, and view services.
- Assign each service to a service category.
- Set service name, description, default duration, default price, taxable flag, and active status.

Standard Company Users can:

- Select one or more active services when completing a visit.

### 9.2 Materials

Company Admins can:

- View materials grouped by material category.
- Create, edit, archive, and view materials.
- Assign each material to a material category.
- Set material name, unit of measure, default unit cost, default billable price, taxable flag, and active status.

Standard Company Users can:

- Select materials used during a visit.
- Enter quantity used.

### 9.3 Pool Equipment

System Admins can:

- View pool equipment grouped by equipment category.
- Create, edit, archive, and reactivate global equipment categories and equipment items.
- Set equipment category manufacturer, name, description, scope, and active status.
- Set equipment item category, name, description, image URL reference, and active status.

Company Admins can:

- View pool equipment grouped by equipment category for their company scope.
- Create, edit, archive, and reactivate company-scoped equipment categories and equipment items.

Home Owners can:

- View pool equipment grouped by equipment category for their owner scope.
- Create, edit, archive, and reactivate owner-scoped equipment categories and equipment items.

Current implementation:

- Company catalogs include service categories and material categories.
- Seeded Clearwater catalog data groups pool services under maintenance/equipment categories and materials under chemicals.
- Seed data includes four richer test companies: `Pool1Clean1`, `PoolClean2`, `Landscape1`, and `Landscape2`.
- Each richer test company includes at least five services, five materials, three equipment items, and five users across owner, business user, and business client roles.
- Seed data includes three independent homeowner test users with `homeowner-1@independent.com`, `homeowner-2@independent.com`, and `homeowner-3@independent.com`; each has no company memberships and has owner-scoped pool equipment.
- The `/catalog` page displays company services and materials grouped by category.
- The `/catalog/materials` and `/catalog/services` pages split the company catalog into focused material and service editors.
- The `/admin/catalog/materials` and `/admin/catalog/services` pages provide focused system-admin catalog editors for this slice.
- Focused material and service editors support category and item create, edit, archive, and reactivate.
- The `/admin/catalog/poolequipment`, `/catalog/poolequipment`, and `/poolequipment` pages provide focused pool-equipment editors for global, company, and homeowner scopes.
- Focused pool-equipment editors support category and item create, edit, archive, reactivate, and image URL reference display.
- Focused service, material, and pool-equipment editors use collapsible table panels for category and item lists; Create and Edit actions expand inline editor panels.
- Focused service, material, and pool-equipment editors support copy-as-custom actions for seeded starter categories and starter items.
- Copied starter records become editable non-system-managed custom records in the current scope with unique `-custom` IDs.
- Existing uncategorized rows are displayed under an uncategorized fallback group.

### 9.4 System Mode

Current implementation:

- `SystemSettings.SystemMode` is persisted as a system setting and supports `Pool` and `Landscape`; `SystemSettings:SystemMode` configuration supplies the default only when the persisted row is missing.
- `Pool` mode brands the application as `PoolShark` and uses the pool waterfall hero image.
- `Landscape` mode brands the application as `TreeShark` and uses the mature fruit-tree landscape hero image.
- Dashboard pages show the current mode's hero image.
- The `/dashboard` page is persona-aware for System Administrators, Business Owners, Business Employees, Business Clients, Independent Home Owners, and pending-only users.
- The business owner dashboard shows health metrics for Business Clients, Users, Pool Configurations in Pool mode, Materials, Services, and Service Packages.
- The business owner dashboard shows pending approval controls, assigned visits today, upcoming visits, unscheduled/scheduled visits, and workspace links.
- The business employee dashboard shows assigned visits today, upcoming visits, and recently completed visits scoped to the signed-in employee.
- The business client dashboard shows upcoming visits, service package, Pool Configuration in Pool mode, and completed visits.
- The independent homeowner dashboard shows Pool Equipment and Service History panels.
- Authenticated navigation hides Home; public navigation still shows Home and Help.
- Landscape mode hides Pool Equipment navigation and redirects direct Pool Equipment routes back to the dashboard.
- System Administrators can change `SystemMode` from the General Settings page.

## 10. Scheduling Requirements

Company Admins can:

- Create one-time visits.
- Create ad-hoc and service package visits.
- Assign visits to active Company Users or Company Admins.
- Reassign visits.
- Reschedule visits.
- View visits in grouped status panels.
- Complete and close visits.
- Delete visits that are not completed or closed.

Schedule fields:

- Company client.
- Service address.
- Assigned user.
- Scheduled date.
- Scheduled start window.
- Scheduled end window.
- Visit type.
- Visit name.
- Services planned.
- Completed planned services.
- Out-of-scope services.
- Out-of-scope materials.
- Invoice id.
- Status.
- Notes to business client.
- Notes to service client.
- Internal notes.

Visit statuses:

- New
- Unscheduled
- Scheduled
- Assigned
- InProgress
- Completed
- Closed
- Canceled
- Skipped

Current implementation:

- Visit Scheduling is implemented at `/visits`, `/admin/visits`, and the legacy `/schedule` route.
- Visits are grouped into Unscheduled and Scheduled, Assigned, Recently Completed, and Closed panels.
- System Administrators manage visits across service clients matching the current application mode.
- Business Owners manage visits for their active service client.
- Business Employees work assigned visits from the Dashboard and Field page.
- Business Clients view their own upcoming and completed visits from the Dashboard and can edit only Notes To Service Provider.
- Recurring schedule templates, automatic generation, and calendar day/week/month views are future work.

## 11. Route Optimization Requirements

Standard Company Users can:

- View all assigned visits for a selected date.
- Display visits on a map.
- Request an optimized route.
- Reorder visits manually.
- Open navigation directions for a selected stop.

Phase one implementation may use:

- Stored service addresses with latitude and longitude.
- Azure Maps or another mapping service for geocoding and route optimization.
- A fallback route ordered by scheduled window and address when optimization is unavailable.

Route data to persist:

- Route date.
- Assigned user.
- Ordered visit IDs.
- Optimization provider.
- Distance estimate.
- Duration estimate.
- Last optimized date.

## 12. Service Visit Completion Requirements

Standard Company Users can:

- Start a visit.
- Record arrival timestamp.
- Select services performed.
- Select materials used.
- Enter quantities.
- Enter free-form completion notes.
- Mark notes as customer-visible or internal if supported.
- Complete the visit.

On completion:

- Persist visit completion details.
- Persist services performed.
- Persist materials used.
- Persist notes.
- Persist timestamps.
- Generate a service completion email to the Company Client if notifications are enabled.
- Make the visit visible in the Company Client User service history.
- Make the visit available for billing and reporting.

## 13. Email and Notification Requirements

Email events:

- Employee invited.
- Employee join request submitted.
- Employee join request approved or rejected.
- Client user invited.
- Client user access request submitted.
- Client user access request approved or rejected.
- Visit completed.
- Invoice or payment event notification.
- Message received.

Email logging:

- Every attempted email send must create an email log entry.
- Email logs must include company ID when applicable, email type, recipient user ID, original recipient email, actual recipient email, from email, CC email, subject, body, timestamp, status, provider message ID when available, and failure reason when applicable.
- System Administrators, Business Owners, Business Clients, and Independent Home Owners can view role-scoped email log entries from Logs / Email Log.
- Business Employees do not have email log access.
- Test-user email must be routed to a configured test inbox when available and must not accidentally send to fake seeded addresses.
- If a recipient user disables email notifications, the provider send is skipped and an email log entry is written with `Suppressed` status.

Company Admins can configure:

- From display name.
- Reply-to email.
- Service completion email template.
- Whether services performed are included.
- Whether materials used are included.
- Whether field notes are included.

## 14. Messaging Requirements

Company Client Users can:

- Create a message thread with the company.
- Send a message.
- View replies.

Company Admins can:

- View all client message threads.
- Reply to messages.
- Mark messages open, pending, or closed.

Optional future enhancement:

- Allow Standard Company Users to participate in assigned-client message threads.

## 15. Billing and Stripe Requirements

The current billing implementation supports application-managed invoices for completed service visits. Stripe payment processing remains a future integration.

Company Admins can:

- View invoices for company clients.
- Create invoices from closed visits that do not already have an invoice id.
- Move invoice status forward from New to Invoiced to Paid.
- Delete invoices; deleting an invoice clears the related visit invoice id.

Company Client Users can:

- View invoices.
- View generated invoice HTML as a rendered preview.

System Administrators can:

- View and manage invoices for service clients matching the current application mode.

Invoice requirements:

- Invoice ids increment per service client.
- Ad-hoc visits bill planned services, out-of-scope services, and out-of-scope materials.
- Service package visits treat planned services as included and bill out-of-scope services and out-of-scope materials.
- Creating an invoice stores an invoice snapshot, updates the service visit invoice id, and queues an invoice email.
- A service visit invoice id is valid only when it references an existing invoice record for the same service client.
- Closed visits with stale invoice ids and no matching invoice record remain eligible for invoice creation.

System requirements:

- Store invoice snapshots, status, line items, totals, generated invoice HTML, and event timestamps.
- Store payment provider references when Stripe is added.
- Store billing status and event history when payment processing is added.
- Process Stripe webhooks idempotently when Stripe is added.
- Do not store raw card data.

## 16. Reporting Requirements

Company Admins can generate reports by:

- Daily range.
- Weekly range.
- Monthly range.
- Custom date range.
- Company user.
- Company client.
- Service.
- Material.
- Visit status.

Reports:

- Completed visits.
- Scheduled visits.
- Revenue summary.
- Materials usage.
- User productivity.
- Client service history.
- Billing and payment summary.

Report exports:

- CSV export should be supported.
- PDF export may be added in a future phase.

## 17. Audit and Compliance Requirements

Persist audit events for:

- User sign-in.
- Company creation and update.
- Company user approval or rejection.
- Client user approval or rejection.
- Service, material, client type changes.
- Schedule creation, reassignment, cancellation, and completion.
- Billing events.

Audit fields:

- Event ID.
- Event type.
- Company ID if applicable.
- Actor user ID.
- Target entity type.
- Target entity ID.
- Timestamp.
- Summary.
- Metadata.

## 18. Non-Functional Requirements

### 18.1 Hosting

- Host the Blazor application on Azure.
- Use Azure Storage for persistent data.
- Use the Azure Table-backed store when `AzureStorage:UseAzureStorage` is enabled; use the in-memory store only for local/demo mode.
- Use Azure services for application logs, configuration, and secrets.
- Use Application Insights through Azure Monitor OpenTelemetry for request, dependency, log, metric, and business workflow telemetry.

### 18.2 Reliability

- Service completion must be durable even if email sending fails.
- Payment webhooks must be idempotent.
- Schedule generation must avoid duplicate visit creation.
- Background jobs should be retryable.
- Current job services are callable application services and are invoked automatically by the WebApp hosted scheduler.
- Retry metadata and dead-letter handling remain future operational work.
- Important workflows should emit telemetry so operational failures can be diagnosed in Application Insights.
- Critical persona workflows should have scenario tests, including application-level workflow tests and Playwright browser tests for high-value UI paths.

### 18.3 Security

- Tenant isolation is mandatory.
- All company-scoped reads and writes must validate company membership.
- Store secrets in Azure Key Vault or secure Azure application configuration.
- Do not store payment card data.
- Minimize exposure of homeowner access notes and gate codes.

### 18.4 Performance

- Mobile route and visit screens should load quickly for a technician's daily assignments.
- Reports should support filtering by indexed partition keys and date ranges.
- Use Azure Table partitioning or equivalent storage design to avoid full scans.

### 18.5 Accessibility and Mobile

- UI must be responsive.
- Field-user workflows must be optimized for mobile.
- Buttons and form controls must be touch-friendly.
- Core workflows must be keyboard accessible.

## 19. Suggested MVP Scope

MVP should include:

- Google sign-in.
- Multi-tenant company model.
- System Admin company type and company management.
- Company Admin company configuration.
- Company user approval workflow.
- Client user approval workflow.
- Client management.
- Services and materials.
- Client types and pricing.
- One-time and recurring schedule management.
- Assignment to field users.
- Field-user daily route list.
- Visit completion with services, materials, and notes.
- Service completion email.
- Client service history.
- Stripe payment record integration.
- Basic reports and CSV export.

Defer to later phases:

- Native mobile app wrapper.
- Advanced route optimization.
- Photo uploads.
- Offline-first field workflow.
- Advanced inventory management.
- Payroll integration.
- Two-way SMS.
- Automated invoice generation beyond Stripe-hosted payment links.
