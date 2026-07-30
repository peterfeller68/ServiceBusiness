# Service Business Follow-On Requirements

## 1. Landing Page

When a user navigates to the home page, they should see a clear and concise landing page that provides an overview of the service business.
The landing page should include a picture of a nice pool with waterfall in a lush landscape.
The user can then either Sign In or Register to access the full features of the service business.
The only menu option available when no one is signed in is Help

### 1.1 Register
When a user clicks on the "Register" button, they should be taken to a registration page where they can create an account.
They need to register as either a business owner, business employee, business client or home owner.
Once they pick that, they will authenticate using a gmail account and then taken to a page where they can fill out the necessary information to complete their registration.

If they register as a business owner, they will be taken to a page where they can create their business profile and set up their services.

If they register as a business employee, they will be given a dropdown to choose which business they want to be associated with and then taken to a page where they can fill out their profile information and set up their account settings.
They will not have access to the service until the business owner approves their account. Once approved, they will be able to access the features corresponding to their role.

If they register as a business client, they will be given a dropdown to choose which business they want to be associated with and then taken to a page where they can fill out their profile information and set up their account settings.
They will not have access to the service until the business owner approves their account. Once approved, they will be able to access the features corresponding to their role.

If they register as a home owner, they need to provide their home address.

Current implementation:

- `/register` supports Business Owner, Business User, Business Client, and Independent Homeowner account types.
- Business User and Business Client registrations require a selected business and create pending company memberships.
- Independent Homeowner registration does not require a business selection or approval.
- Independent Homeowner registration captures home address and access notes.
- Independent Homeowner registration creates an active user account with no company membership, stores an owner profile, and seeds owner-scoped pool equipment starter records.

## 1.2 Sign In
When a user clicks on the "Sign In" button, they should be taken to a sign-in page where they can enter their credentials to access their account.
They will be taken to a Dashboard page where they can see their profile information, manage their account settings, and access the features based on their Role.
List their role under their name in the title.
When a user is signed in, the Home button is no longer visible, but the picture should be on the Dashboard page.

Current implementation:

- Authenticated navigation hides the Home link and keeps Dashboard, role-appropriate Settings, Reports, and Help.
- The user, company, and system-admin dashboard pages show the current system-mode hero image.
- The shell profile indicator lists the current user's role under their display name.

## 1.3 Test Users
Keep test users in mind as you implement the registration and sign-in process. There will be a table of users and there needs to be a way to identify which user is a test user. Test users can skip the authentication.

## 1.4 Authentication
Implement authentication using Gmail accounts. Users should be able to sign in using their Gmail credentials, and the system should handle the authentication process securely.

## 1.5 User Roles
Keep a table of roles in mind as you implement the registration and sign-in process. There will be a table of roles and there needs to be a way to identify which role a user has. The system should enforce access control based on the user's role, ensuring that users can only access features and information relevant to their role (business owner, business user, or business client).

## 1.6 Account Approval
When a user registers as a business user or business client, their account should be pending until approved by the business owner. The business owner should have the ability to review and approve or reject account requests from users associated with their business. Once approved, the user should receive a notification and gain access to the features corresponding to their role.

## 1.7 Storage
As you implement the registration and sign-in process, keep in mind the storage entities that will be used to store user information, roles, and account approval status. Ensure that the necessary data is stored securely and can be retrieved efficiently when needed for authentication and access control purposes.
Implement the necessary database tables and relationships to support user registration, authentication, role management, and account approval processes.

## 1.8 System Admin User
A user needs to be able to log in as a System Admin. The first System Adminuser can be seeded in the database during application initialization. The System Admin will have elevated privileges and access to manage the overall system, including user accounts, roles, and permissions.
A system admin user should be able to manage other users, and needs to be able to set the roles of other users. A system admin needs to make another already registered user a system admin.
A user with privileges to manage other users should not be able to disable himself.
Users need to be able to be disabled and enabled. Only a system adim or company admin/owner can disable/ enable other users.

Current implementation:

- The seeded `sys-admin` user is an active system admin test user.
- `AppUser.Status` tracks whether a user is `Active` or `Disabled`.
- Disabled users fail authorization before role checks.
- System admins can view platform summary metrics from `/admin` and focused user management from `/admin/users`.
- System admins can promote or remove system-admin privileges for already registered users.
- System admins can enable or disable users.
- A system admin cannot disable their own account.
- The service prevents removing or disabling the last active system admin.

### 1.8.1 System Admin Functions
User Management
Role Management

Current implementation:

- User management is implemented in `PlatformAdminService`.
- The System Admin Users page exposes Make Admin, Remove Admin, Disable, and Enable actions.
- Role management is implemented in `PlatformAdminService`.
- The System Admin Roles page at `/admin/roles` exposes editable role display names, descriptions, owner-approval requirements, and permissions.
- Role identities remain the built-in `CompanyAdmin`, `CompanyUser`, and `CompanyClientUser` values for this slice.

### 1.8.1 System Admin User Dashboard
The System Admin should have access to a dashboard where they can view the overall state of the system.
Summary of users, roles, and account approval status.
Summary of businesses and their associated users.

Current implementation:

- `/admin` shows platform summary metrics.
- Focused pages are available for Companies (`/admin/companies`), Users (`/admin/users`), Roles (`/admin/roles`), Email Log (`/admin/email-log`), Materials (`/admin/catalog/materials`), and Services (`/admin/catalog/services`).

## 1.9 Personas

### 1.9.1 System Administrator
 The system administrator who manages the SaaS service. Has access to all data and configurations across the system, and can manage users, roles, and companies. Responsible for maintaining the overall health and security of the application.

### 1.9.2 Company Admin or Owner
 The company admin is generally the owner of the pool cleaning business or landscaping business and maintains all data and settings associated with the Business Client

### 1.9.3 Company User or Employee
 The company user or employee will be the one providing the service to the company clients. That user will mostly interact with the system via the mobile UI.

### 1.9.4 Company Client User or Service Recipient
 The company client or service recipient will be using the application to see checked and provided services and do the payments. That user will mostly interact with the system via the mobile UI.

### 1.9.5 Independent Homeowner User
 The independent homeowner user will be a user that is not associated with any company, but can use the application to find service providers in the area. This user can also use the service to manage their own pools, to see a history of services performed by himself.

Current implementation:

- Independent Homeowner users are represented as active users with no company memberships.
- Independent Homeowner profile data stores home address and access notes.
- Their dashboard shows an Independent Homeowner workspace; in Pool mode it includes a Pool Equipment action.
- In Pool mode their Settings menu exposes Pool Equipment at `/poolequipment`.
- In Pool mode `/poolequipment` scopes equipment categories and items to the current user ID.

## 1.10 Dashboard
Once a user has successfully authenticated, they should be taken to a dashboard page. The Dashboard will be different by persona.

### 1.10.1 System Administrator

### 1.10.2 Company Admin or Owner
When a company admin logs in he will be redirected to the Dashboard.
That user needs access to all pages under Settings.
On the dashboard there should be a row of tiles for Customers, Employees, Pool Equipment, Materials and Services. Each should show a count.
Under the Workspace there will be a row of tiles is for work that needs to be completed. Pending Employee Approvals, Pending Custoemr Approvals. Each should show a count.
Clicking the Pending .... Approval buttons will bring up a panel with all unapproved requests. A request shall be approved using a toggle button.
Noting else should be on the Dashboard at the moment

### 1.10.3 Company User or Employee

### 1.10.4 Company Client User or Service Recipient

### 1.10.5 Independent Homeowner User

Current implementation:

- Authenticated users land on `/dashboard`, which shows persona-aware workspace access based on the signed-in user's company memberships or independent homeowner profile.
- Company admins can open the company dashboard at `/company`.
- The company admin dashboard shows setup tiles for Customers, Employees, Pool Equipment in Pool mode, Materials, and Services.
- The company admin dashboard shows work tiles for Pending Employee Approvals and Pending Customer Approvals.
- Clicking a pending approval tile expands an approval panel scoped to that request type; pending users can be approved with the toggle or rejected from the action column.
- Independent homeowners with no company memberships see an independent homeowner workspace and, in Pool mode, a Pool Equipment action.


# 2. Email Support
Sending emails will be used in various location in order to notify users of important events, such as account approval, service updates, or appointment reminders.
Go ahead and wire up the project to support emails using Azure COmmunication Services. You can use the Azure Communication Services Email SDK to send emails from your application.

Current implementation:

- The application uses `Azure.Communication.Email` through `AzureCommunicationEmailNotificationQueue`.
- Email sending is configuration-backed and safe by default. If Azure Communication Services settings are absent, email attempts are logged with `Queued` status instead of failing the business workflow.
- Account approval decisions and visit completion notifications flow through the same notification abstraction.

## 2.1 Logging of sent emails
Implement logging of sent emails in the database. Create a table to store information about each email sent
Allow System Admins to view the sent emails in the admin dashboard. The email log should include details such as the recipient's email address, subject, body, timestamp, and status (sent, failed, etc.). This will help in tracking email communications and troubleshooting any issues that may arise with email delivery.

Current implementation:

- Email attempts are persisted as `EmailLogEntry` records.
- The System Admin dashboard displays total email log count.
- `/admin/email-log` displays recent sent-email log entries with recipient, subject, type, status, timestamp, reroute details, and failure/suppression details.

# 2.2 Test User Support
When sending emails, ensure that test users are handled appropriately. Test users should be able to receive emails without affecting real users. You can implement a mechanism to identify test users and route their emails to a designated test email address or log them separately for testing purposes. This will allow you to verify the email functionality without sending emails to actual users during development and testing phases.
The test user accounts will not be real accounts, so we cannot send emails there. So the user definition should have an email accress, but if the email address is the rowkey of the table, the table still needs a recipient email address.

Current implementation:

- `AppUser.Email` remains the identity/login email.
- `AppUser.NotificationEmail` stores the normal notification target.
- `AppUser.EmailNotificationsEnabled` stores the user's email notification preference; missing legacy values are treated as enabled.
- Test users are marked with `AppUser.IsTestUser`.
- When `Email:TestRecipientEmail` is configured, test-user email is rerouted to that address and the log status is `TestRerouted`.

# 3. Authentication
Implement Google Auth and provide instructions how to configure it for test users. This will allow users to sign in using their Google accounts, providing a convenient and secure authentication method. Ensure that the authentication process is properly integrated with the user registration and sign-in flow, and that user roles and access control are enforced based on the authenticated user's role. Provide clear instructions for configuring Google Auth for test users to facilitate testing and development.
Google Auth should be skipped for test users, as it is not a real gmail account. Test users should be able to sign in without authentication, allowing for easier testing and development without the need for real Google accounts. Implement a mechanism to identify test users and bypass the Google Auth process for those users, while still enforcing authentication for real users. This will ensure that test users can access the application without any issues while maintaining security for real users.

Current implementation:

- Real users sign in through ASP.NET Core cookie authentication and the Google OAuth handler.
- `/auth/google` starts the Google challenge when credentials are configured.
- `/auth/google-complete` creates or updates the application profile using Google subject ID, email, display name, and profile image.
- `/auth/test-signin` allows only seeded users marked `IsTestUser` to bypass Google authentication.
- Configuration instructions are in `docs/google-auth-setup.md`.

# 4. Storage
Setup and configure the application to connect to Azure Storage

Current implementation:

- The application references `Azure.Data.Tables`.
- `AzureStorageTableInitializer` connects to the configured storage account and creates the current logical table set when `AzureStorage:UseAzureStorage` is `true`.
- When `AzureStorage:UseAzureStorage` is `true`, dependency injection uses `AzureTableServiceBusinessStore`.
- `AzureTableServiceBusinessStore` seeds the MVP data set into Azure Tables when the `Users` table is empty.
- Google sign-in creates or updates the signed-in `AppUser` in the `Users`, `UserByEmail`, and `UserByGoogleSubject` tables.
- Local development still uses `InMemoryServiceBusinessStore` when Azure Storage is disabled.

# 5. Observability
Add Application Insights to the project and configure it to monitor the application's performance, track user interactions, and log important events. This will help in identifying and diagnosing issues, understanding user behavior, and improving the overall performance and reliability of the application. Ensure that Application Insights is properly integrated with the application and that relevant telemetry data is being collected for analysis.

Current implementation:

- The web project references `Azure.Monitor.OpenTelemetry.AspNetCore`, Microsoft's recommended Azure Monitor OpenTelemetry distro for new ASP.NET Core applications.
- Azure Monitor export is registered only when `ApplicationInsights:ConnectionString` is configured.
- The application registers the custom `ServiceBusiness` activity source and meter.
- Business workflows emit telemetry for account approval decisions, visit completions, and email notifications.
- Configuration instructions are in `docs/observability-setup.md`.

# 6. Application Features

# 6.1 User Management
There needs to be the ability to manage users in the system. This includes the ability to view and edit all users, their roles, and their account approval status.
Users need to be able to be enabled and disabled. Only a system admin or company admin/owner can disable/ enable other users. A user with privileges to manage other users should not be able to disable himself.
Users need the ability to be approved/rejected.

Current implementation:

- System-admin user management is implemented.
- Company-owner approval/rejection for pending company access is implemented at `/company/users`; `/approvals` remains as a compatibility route for the earlier approval page.
- Company-owner deactivation/reactivation for existing company users is implemented as company membership status management.

# 6.2 Role Management
There needs to be the ability to define and manage roles in the system. This includes the ability to view and edit all roles.
A role is defined by a name and a set of permissions. Permissions define what actions a user with that role can perform in the system. The system should enforce access control based on the user's role, ensuring that users can only access features and information relevant to their role (business owner, business user, or business client).

Current implementation:

- Role definitions are stored as `RoleDefinition` records with role, display name, description, and owner-approval requirement.
- Role definitions include an editable permission list.
- System admins can view and update role metadata and permissions from `/admin/roles`.
- The authorization layer enforces role checks using active company memberships and system-admin flags.
- Runtime authorization still uses the fixed role identities for protected workflows; mapping every individual permission string to enforcement policies remains a future enhancement.

# 6.3 User Profile
Once a user has successfully autheticated, a User Profile indicator should show up at the top right of the page. By clicking that indicator, the users profile page should be shown allowing the user to change his or or profile.
A user should be able to logout from the profile page.

Current implementation:

- The authenticated shell shows a top-right profile indicator with the current user's display name.
- `/profile` allows the current user to edit display name, notification email, phone, and email notification preference.
- The profile page displays immutable login email, profile image when available, and account status.
- Users can log out from `/profile`.

# 7. Workflow

# 7.1 Initial Workflow
- When a user logs in with the seeded sysadmin account, he/she will need to update another user to a sysadmin, so this seeded sys-admin account can be disabled.


# 8. User Interface

There are several pages that allow editing of reference data. Examples are: Equipment, Users, Companies, Services, Materials, etc.

As a default, structure the pages to edit Data as follows:
In a collapsible panel, have a table with the list of items. Add action icons on the very right to allow editing or deletion of items.
Have a mechanism to allow a user to add an item.
When the user clicks the Add button, a panel should expand with the fields needed to add the new item.
When the user clicks the button to edit the item, a panel should expand with the fields populated, so they can be updated.

Example for Service Categories and Services

                                                                                       Create
---------------------------------------------------------------------------------------------
- Service Categories                                                                      ^ -
---------------------------------------------------------------------------------------------
  ID     Active   Name       Description                                               Action

---------------------------------------------------------------------------------------------
- Services                                                                                ^ -
---------------------------------------------------------------------------------------------
  ID     Active   Name       Category           Description                            Action

Current implementation:

- Reference-data editors use collapsible management panels with table lists and right-aligned action controls.
- Create buttons expand an inline editor panel with empty fields for new rows.
- Edit actions expand the same inline editor panel with the selected row populated.
- Archive/delete actions are implemented as status toggles where the entity supports reactivation.



## 8.1 Navigation Routing Rules

Each leaf menu item must navigate to a distinct route dedicated to that function.
Do not route multiple editor leaf menu items to a combined dashboard page unless explicitly stated.
Dashboards may summarize multiple areas, but editor menu items must open focused editor pages.

Required routes:

### System Admin
- Dashboard -> /admin
- Settings / Companies -> /admin/companies
- Settings / Users -> /admin/users
- Settings / Roles -> /admin/roles
- Settings / Catalog / Pool Equipment -> /admin/catalog/poolequipment
- Settings / Catalog / Materials -> /admin/catalog/materials
- Settings / Catalog / Services -> /admin/catalog/services
- Log / Email -> /admin/email-log
- Reports -> /reports
- Help -> /help

### Company Admin
- Dashboard -> /dashboard
- Settings / Customers -> /clients
- Settings / Users -> /company/users
- Settings / Catalog / Pool Equipment -> /catalog/poolequipment
- Settings / Catalog / Materials -> /catalog/materials
- Settings / Catalog / Services -> /catalog/services
- Reports -> /reports
- Help -> /help

### Company Employee User
- Dashboard -> /dashboard
- Reports -> /reports
- Help -> /help

### Company Client User
- Dashboard -> /dashboard
- Reports -> /reports
- Help -> /help

### Home Owner
- Dashboard -> /dashboard
- Settings / Catalog / Pool Equipment -> /poolequipment
- Settings / Catalog / Materials -> /catalog/materials
- Reports -> /reports
- Help -> /help


## 8.2 Company Editor

Route: /admin/companies for System Admins.
Route: /company/profile for Company Admins.

The Company Editor must be a focused company-management page.
It must not show unrelated user management, role management, email logs, or platform dashboard summaries.

System Admins can:
- View companies.
- Create companies.
- Edit company details.
- Set company type.
- Enable, disable, or suspend companies.

Company Admins can:
- Edit their own company profile and settings only.

Acceptance criteria:
- Clicking Settings / Companies opens the Company Editor.
- The page title is Companies.
- User management and role management controls are not shown on this page.

Current implementation:

- `/admin/companies` is a focused company-management page showing company types and companies without user, role, catalog, email-log, or platform-dashboard controls.
- Company Types and Companies render as collapsible table panels.
- The Companies panel has a Create action that opens the company editor panel; Edit opens the same panel populated from the selected company row.
- System admins can create companies, edit company details, suspend active companies, archive active companies, and reactivate suspended or inactive companies.

## 8.3 User Editor

Route: `/admin/users` for System Admins.
Route: `/company/users` for Company Admins.

The User Editor must be a focused user-management page. It must not show unrelated company editor controls, role editor controls, catalog editor controls, email logs, or platform dashboard summaries.

System Admins can:
- View all registered users.
- Filter users by company, role, account status, system-admin status, test-user status, and approval status.
- View each user's company memberships and roles.
- Enable or disable users.
- Promote an already registered user to System Admin.
- Remove System Admin privileges when at least one other active System Admin remains.
- View pending account approval status across companies.

Company Admins can:
- View users associated with their company.
- View pending employee and client-user access requests for their company.
- Approve or reject employee join requests.
- Approve or reject client-user access requests.
- Enable or disable users within their company scope where permitted.
- Assign or update company-scoped roles where permitted.

Rules:
- A System Admin must be able to select a company when viewing company-scoped users.
- A Company Admin must only see users and access requests for companies where they have active `CompanyAdmin` membership.
- A user with user-management privileges must not be able to disable their own account.
- The system must keep at least one active System Admin.
- Disabled users cannot sign in or pass authorization checks.

Acceptance criteria:
- Clicking System Admin Settings / Users opens `/admin/users`.
- Clicking Company Admin Settings / Users opens `/company/users`.
- The page title is Users.
- Company management, role definition editing, catalog editing, and email log controls are not shown on this page.
- User actions are scoped to the current user's privileges.

Current implementation:

- `/admin/users` is a focused system-admin user-management page with create-user, edit-user, company access summaries, system-admin promotion/removal, and user enable/disable actions.
- System-admin Users render in a collapsible table panel with right-aligned edit, admin, and status actions.
- The Create action opens the user editor panel; Edit opens the same panel populated from the selected user row.
- `/company/users` is a focused company-admin user page for pending employee and client-user access decisions, active/inactive company access management, and company-scoped role updates.
- Company-admin enable/disable is implemented as company membership activation/deactivation, not global account disablement.
- Company admins cannot deactivate or reassign their own Company Admin access, and the service layer requires at least one active Company Admin to remain.

## 8.4 Role Editor

Route: /admin/roles.

The Role Editor must be a focused system-wide role-management page.
It must not show company editor controls, user editor controls, or email logs.

System Admins can:
- View role definitions.
- Edit role display names.
- Edit descriptions.
- Edit permission lists.
- Edit owner approval requirements.

Acceptance criteria:
- Clicking Settings / Roles opens the Role Editor.
- The page title is Roles.
- Only role-management controls are shown.

Current implementation:

- `/admin/roles` is a focused role editor for built-in role display names, descriptions, permissions, and owner-approval requirements.
- Roles render in a collapsible table panel; Edit opens an inline editor for the selected fixed role.

## 8.5 Materials Editor

Route: `/admin/catalog/materials` for System Admins.
Route: `/catalog/materials` for Company Admins.

The Materials Editor must be a focused catalog-management page for material categories and material items. It must not show unrelated service editor controls, company editor controls, user editor controls, role editor controls, email logs, or dashboard summaries.

System Admins can:
- View global material categories and global material items.
- Create, edit, archive, and reactivate global material categories.
- Create, edit, archive, and reactivate global material items.
- Select a company to view or manage company-scoped material categories and material items.
- Copy global material categories or material items into a selected company scope so they can be customized by that company.

Company Admins can:
- View material categories and material items available to their company.
- Create, edit, archive, and reactivate company-scoped material categories.
- Create, edit, archive, and reactivate company-scoped material items.
- Copy available system/global material categories or material items into their company scope.
- Customize copied materials without changing the system/global definition.

Material category fields:
- Name.
- Description.
- Scope: system/global or company-scoped.
- Active status.

Material item fields:
- Material category.
- Name.
- Description.
- Unit of measure.
- Default unit cost.
- Default billable unit price.
- Taxable flag.
- Active status.

Rules:
- System/global material categories and material items are starting templates.
- Once a company chooses a system/global category or material item, it is copied into that company scope.
- Company edits to copied materials must not change the system/global source record.
- Company Admins must only manage company-scoped material records for companies where they have active `CompanyAdmin` membership.
- Standard Company Users may view/select active materials during visit completion but must not manage catalog definitions.

Acceptance criteria:
- Clicking System Admin Settings / Catalog / Materials opens `/admin/catalog/materials`.
- Clicking Company Admin Settings / Catalog / Materials opens `/catalog/materials`.
- The page title is Materials.
- Service category and service item editing controls are not shown on this page.
- Material categories and material items are grouped clearly.
- System/global and company-scoped records are visually distinguishable.

Current implementation:

- `/admin/catalog/materials` and `/catalog/materials` are focused material catalog pages.
- Material categories and material items render as separate collapsible table panels.
- Each panel has a Create action that expands the appropriate editor panel; Edit opens the same editor populated from the selected row.
- System admins and company admins can create, edit, archive, and reactivate material categories and material items for the selected company scope.
- Starter categories and material items can be copied into editable custom records within the current company scope.
- Cross-company global template management and company selection remain future slices.

# 8.6 Equipment Editor

Route: `/admin/catalog/poolequipment` for System Admins.
Route: `/catalog/poolequipment` for Company Admins.
Route: `/poolequipment` for Home Owners.

System Admins can:
- View global equipment categories and global equipment items.
- Create, edit, archive, and reactivate global equipment categories.
- Create, edit, archive, and reactivate global equipment items.
- Select a company to view or manage company-scoped equipment categories and equipment items.
- Copy global equipment categories or equipment items into a selected company scope so they can be customized by that company.

Company Admins can:
- View pool equipment categories and equipment items available to their company.
- Create, edit, archive, and reactivate company-scoped equipment categories.
- Create, edit, archive, and reactivate company-scoped equipment items.
- Copy available system/global equipment categories or equipment items into their company scope.
- Customize copied equipment without changing the system/global definition.
- Upload images of the home owner equipment and associate them with equipment items.

Home Owners can:
- View pool equipment categories and equipment items available.
- Create, edit, archive, and reactivate owner-scoped equipment categories.
- Create, edit, archive, and reactivate owner-scoped equipment items.
- Copy available system/global equipment categories or equipment items into their owner scope.
- Customize copied equipment without changing the system/global definition.
- Upload images of their equipment and associate them with equipment items.

Equipment category fields:
- Manufacturer.
- Name.
- Description.
- Scope: system/global or company-scoped.
- Active status.

Equipment item fields:
- Equipment category.
- Name.
- Description.
- Image - stored as blob in Azure Storage with reference URL in the item record.
- Active status.

Rules:
- System/global equipment categories and equipment items are starting templates.
- Once a company chooses a system/global category or equipment item, it is copied into that company scope.
- Company edits to copied equipment must not change the system/global source record.
- Company Admins must only manage company-scoped equipment records for companies where they have active `CompanyAdmin` membership.
- Standard Company Users may view/select active equipment during visit completion but must not manage catalog definitions.

Acceptance criteria:
- Clicking System Admin Settings / Catalog / Equipment opens `/admin/catalog/poolequipment`.
- Clicking Company Admin Settings / Catalog / Equipment opens `/catalog/poolequipment`.
- Clicking Homeowner Settings / Catalog / Equipment opens `/poolequipment`.
- The page title is Pool Equipment.
- Service and material category/item editing controls are not shown on this page.
- Equipment categories and equipment items are grouped clearly.
- System/global and company-scoped records are visually distinguishable.

Current implementation:

- `/admin/catalog/poolequipment`, `/catalog/poolequipment`, and `/poolequipment` are focused pool-equipment catalog pages.
- Equipment categories and equipment items render as separate collapsible table panels.
- Each panel has a Create action that expands the appropriate editor panel; Edit opens the same editor populated from the selected row.
- System admins can create, edit, archive, and reactivate global equipment categories and equipment items.
- Company admins can create, edit, archive, and reactivate Clearwater company-scoped equipment categories and equipment items.
- Homeowners can create, edit, archive, and reactivate their own owner-scoped equipment categories and equipment items.
- Equipment categories and items are grouped clearly; starter records and active/archived status are visually marked.
- Equipment item image references are captured as image URLs and rendered as thumbnails.
- Starter equipment categories and equipment items can be copied into editable custom records within the current global, company, or homeowner scope.
- Company selection, cross-scope copy-from-global workflows, and direct Azure Blob upload UI remain future slices.

# 8.7 Services Editor
Shall work the same as the Materials Editor, but for services.

Current implementation:

- `/admin/catalog/services` and `/catalog/services` are focused service catalog pages.
- Service categories and service items render as separate collapsible table panels.
- Each panel has a Create action that expands the appropriate editor panel; Edit opens the same editor populated from the selected row.
- System admins and company admins can create, edit, archive, and reactivate service categories and service items for the selected company scope.
- Starter service categories and service items can be copied into editable custom records within the current company scope.
- Cross-company global template management and company selection remain future slices.

# 9. Services and Materials Management
Services will be grouped under Service Categories, and materials will be grouped under Material Categories. Each service and material will have a name, description, price, and other relevant details.
The System admin will be able to create and manage service categories, material categories, services, and materials.
Business owners will be able to use the available services and materials, and select the ones that they want to offer to their customers. They can also create their own. The Services and Materials at the system level are serve only as a starting point to a customer. Once they choose to use a system level service or Category, it will be scoped top the customer and can be edited by such.
Business owners will be able to create and manage their own service categories, material categories, services, and materials. They can choose to use the system level services and materials as a starting point, and then customize them to fit their specific needs. Once a system level service or material is selected by a business owner, it will be scoped to that customer and can be edited by the business owner without affecting the system level definition. This allows for flexibility and customization while still providing a baseline set of services and materials for businesses to choose from.

Current implementation:

- Company-scoped `ServiceCategory` and `MaterialCategory` records are modeled and persisted.
- Service and material records now include `CategoryId`.
- The seeded catalog includes starter categories and grouped services/materials for Clearwater.
- `/catalog` displays services and materials grouped by category, with an uncategorized fallback for legacy rows.
- `/catalog/materials` and `/catalog/services` split the company catalog into focused material and service pages.
- `/admin/catalog/materials` and `/admin/catalog/services` provide system-admin focused views over the seeded company catalog for this slice.
- Focused catalog pages include create, edit, archive, and reactivate UI for categories and catalog items.
- Focused catalog pages include copy-as-custom actions for starter categories and starter catalog items.
- Cross-company global template selection and company selection remain later slices.

# 10. Notifications
Notifications will be used to keep users informed about important events and updates related to their service business. This can include notifications for account approval, service updates, appointment reminders, and other relevant information. Notifications can be sent via email or through in-app notifications, depending on the user's preferences and the nature of the notification. Implementing a robust notification system will help ensure that users stay informed and engaged with the application, improving their overall experience and satisfaction with the service business.
Customer users aka employees and customer client users will be able to turn on and off notification in their profile.

Current implementation:

- User profiles include an email notifications toggle.
- Account approval decision and visit completion emails honor `AppUser.EmailNotificationsEnabled`.
- When email notifications are disabled, the email queue writes an `EmailLogEntry` with `Suppressed` status and does not call Azure Communication Services.
- In-app notifications, appointment reminders, billing notifications, and account-request notifications remain future workflow slices.

# 10.1 Notification Types
- Account Approval: Notify users when their account has been approved or rejected by the business owner.
- Account Request: Notify business owners when a new user has registered and is awaiting approval to join their business.
- Service Updates: Notify business client users about service completions, changes, or cancellations related to their appointments.
- Appointment Reminders: Send reminders to business client users about upcoming appointments, including details about the service, time, and location.
- Upcoming Bill Payment: Notify business clients about upcoming bill payments, including the amount due, due date, and payment options.
- Completed Bill Payment: Notify business clients about payment status and receipts for completed services.

# 11. Data Hydration

## 11.1 Test Data
Seed the database with test data for users, businesses, services, materials and pool equipment. This will allow for easier testing and development of the application, as well as providing a baseline set of data to work with.

### 11.1.1 Test Companies
Hydrate the database with at least four test companies, each with different profiles and service offerings. This will allow for testing of various scenarios and use cases within the application.
Two of the test companies will be pool service businesses, and the other two can be landscape service businesses. Each company should have a unique set of services and materials to provide a diverse testing environment.
name the companies Pool1Clean1, PoolClean2, Landscape1, and Landscape2. The pool service businesses can offer services such as pool cleaning, pool maintenance, and pool repair, while the landscape service businesses can offer services such as lawn mowing, landscaping design, and tree trimming. This variety of companies and services will help ensure that the application is thoroughly tested across different business types and service offerings.
Add at least 5 services and 5 materials for each company, with varying prices and details. This will provide a rich dataset for testing the service and material management features of the application, as well as allowing for testing of the appointment scheduling and billing features with different service and material combinations.
Add at least 5 users for each company, with different roles (business owner, business user, business client) and account approval statuses. This will allow for testing of the user management features of the application, as well as ensuring that access control and permissions are properly enforced based on user roles and account statuses.
The urls used for the users should be in the format of `{role}-{n}@{company}.com`, where `{n}` is a unique number for each user and `{company}` is the name of the company they are associated with and {role} is their role. For example, `owner-1@pool1clean1.com`, `user-1@pool1clean1.com`, `client-1@pool1clean1.com`.
Add three independent homeowner emails in the format of `{homeowner}-{n}@{independent}.com`, where `{n}` is a unique number for each user. The are not associated with a company.
Make sure that the User Company Memberships are populated with a mix of active and pending memberships to allow for testing of the approval status workflows.

Current implementation:

- Seed data includes the requested companies: `Pool1Clean1`, `PoolClean2`, `Landscape1`, and `Landscape2`.
- Each requested company includes at least five services, five materials, and five users across owner, business user, and business client roles.
- Seed users use the requested email shape such as `owner-1@pool1clean1.com`, `user-1@pool1clean1.com`, and `client-1@pool1clean1.com`.
- Each requested company includes active and pending memberships to exercise approval status workflows.
- Azure Storage also populates `UserCompanyMemberships` from `CompanyMemberships` so sign-in and approval-status testing can query memberships by user.
- Seed data includes three independent homeowner users with `homeowner-1@independent.com`, `homeowner-2@independent.com`, and `homeowner-3@independent.com` emails.
- Seeded independent homeowners have no company memberships, each has an owner profile, and each has owner-scoped pool-equipment starter records.
- Seed data includes ten additional general-purpose test users with `other-1@gmail.com` through `other-10@gmail.com`; these users have no company memberships.
- The in-memory store owns this seed data, and the Azure Table store hydrates from it when Azure Storage is enabled and the `Users` table is empty.
- These test users shall never use google auth even if Google Auth is chosen.

### 11.1.2 Additional Test Users, not associated with any Company
Add at least 10 users in the format of '{other}-{n}@gmail.com that are not associated with any company. These are just users that can be used for any purpose.
These test users shall never use google auth even if Google Auth is chosen.

Current implementation:

- Seed data includes `other-1@gmail.com` through `other-10@gmail.com`.
- These users are flagged as test users, have no Google subject, bypass Google authentication through the seeded test-user sign-in flow, and have no company memberships.
- The Test Users page supports General Test User records with no associated business.

# 12. System Settings
Only the system administrator will have access to the System Settings.

## 12.1 System Mode
There should be a setting called SystemMode, with two possible values {Pool, Landscape}

### 12.1.1
If the SystemMode is Pool, then show the current Pool graphic, and name the application PoolShark.

### 12.1.2
If the SystemMode is Landscape, then show a graphic of a nicely manicured lawn with Mature Fruit Trees, and name the application TreeShark.
In Landscape Mode, do not show anything related to Pool Equipment.

Current implementation:

- `SystemSettings.SystemMode` is persisted in Azure Table Storage and accepts `Pool` or `Landscape`; `SystemSettings:SystemMode` configuration supplies the startup default only when the persisted row is missing.
- Pool mode names the application `PoolShark` and uses `/images/pool-waterfall-hero.png`.
- Landscape mode names the application `TreeShark` and uses `/images/landscape-fruit-trees-hero.png`.
- The system-admin Settings page lets System Administrators edit the active `SystemMode`.
- Landscape mode hides Pool Equipment navigation links and redirects direct Pool Equipment routes back to the dashboard.


# 13. Test Menu
As the last menu item in the Nav menu have a collapsible "Test" section.
Under the Test menu have a Test Page item with a link to a page called "Test Page". This page will be used for testing purposes and can include various test components and features that are being developed. It will serve as a sandbox environment for testing new functionality and ensuring that it works as expected before being released to production. The Test Page can be accessed by clicking on the "Test" menu item in the navigation menu, and it should be clearly labeled as a testing environment to avoid confusion with the main application features.
Under the Test menu have a Test Users item with a link to a page called "Test Users". This page will be used for managing test user accounts and their associated data. It will allow developers and testers to create, edit, and delete test user accounts, as well as view their details and activity within the application. The Test Users page can be accessed by clicking on the "Test" menu item in the navigation menu, and it should be clearly labeled as a testing environment to avoid confusion with the main application features. This will help ensure that test user management is organized and easily accessible for testing purposes.

Current implementation:

- The navigation menu ends with a collapsible `Test` section.
- The `Test` section contains `Test Page` and `Test Users` links.
- `/test` renders a sandbox page with the current session state and a clear testing-environment label.
- `/test/users` lets System Administrators create and edit test users, and delete/reactivate test users by toggling account status.
