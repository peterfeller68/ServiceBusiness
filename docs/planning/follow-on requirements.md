# Service Business Follow-On Requirements

## 1. Landing Page

When a user navigates to the home page, they should see a clear and concise landing page that provides an overview of the service business.
The landing page should include a picture of a nice pool with waterfall in a lush landscape.
The user can then either Sign In or Register to access the full features of the service business.
The only menu option available when no one is signed in is Help

## 1.1 Register
When a user clicks on the "Register" button, they should be taken to a registration page where they can create an account.
They need to register as either a business owner, business employee, business client or home owner.

Once they pick that, they will authenticate using a gmail account and then taken to a page where they can fill out the necessary information to complete their registration.
If the System is in DevTest Mode, have a button to skip the Google Auth, similar to the Sign-In functionality.

### 1.1.1 Business Owner
Once the registration is complete, navigate to the Business Owner Dashboard. 
From the Dashboard they can create their business profile and set up their services.
Ensure that all needed menu options are available under Settings.

## 1.1.2 Business Employee
They will be given a dropdown to choose which business they want to be associated with and then taken to a page where they can 
fill out their profile information and set up their account settings. Once the registration is complete, navigate to the Business Employee Dashboard.
They will have access to the Dashboard, which will indicate that their registration is pending Business Owner Approval.
Once approved, they will be able to access the features corresponding to their role.
Ensure that all needed menu options are available under Settings.

## 1.1.3 Business Client
They will be given a dropdown to choose which business they want to be associated with.
Then they need to choose which Business Client, they need to be associated with. This drop down will be a list of Addresses.
Then they are taken to a page where they can fill out their profile information and set up their account settings. 
Once the registration is complete, navigate to the Business Client Dashboard.
They will have access to the Dashboard, which will indicate that their registration is pending Business Owner Approval.
Once approved, they will be able to access the features corresponding to their role.
Ensure that all needed menu options are available under Settings.

## 1.1.4 Independent Home Owner
During registration the home owner needs to provide his address
Once the registration is complete, navigate to the Independent Home Owner Dashboard. From the Dashboard they can create their profile and set up their pool equipment 
and services.
Ensure that all needed menu options are available under Settings.

Current implementation:

- `/register` supports Business Owner, Business User, Business Client, and Independent Homeowner account types.
- Business User registrations require a selected business and create pending company memberships.
- Business Client registrations require a selected business plus a selected Business Client address from that business's active client records; the pending membership stores the selected Business Client id so approval grants access to the correct client/home.
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
- Settings / Catalog / Pool Configuration -> /catalog/......
- Settings / Catalog / Materials -> /catalog/materials
- Settings / Catalog / Services -> /catalog/services
- Settings / Catalog / Service Packages -> /catalog/......
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
There will be two types of system settings. Settings stored in appsettings.json, which configure the appservice via AppService 
Settings, and ones stored in the Database.

## 12.1 AppSettings.json
Only the system administrator will have access to the System Settings.

### 12.1.1 System Mode
There should be a setting called SystemMode, with two possible values {Pool, Landscape}
If the SystemMode is Pool, then show the current Pool graphic, and name the application PoolShark.
If the SystemMode is Landscape, then show a graphic of a nicely manicured lawn with Mature Fruit Trees, and name the application TreeShark.
In Landscape Mode, do not show anything related to Pool Equipment.

### 12.1.2 DevTest Mode
In DevTest Mode the application allows Google Auth to be skipped. This enables easier testing. The Test Menu in the Navbar 
is not visible if DevTest is false.

## 12.2 Storage hosted Settings
These storage hosted Settings will be managed under the Settings/General menu item desribed in Section 14.1.

### 12.2.1 Sys Admin
No Items at this time
### 12.2.2 Business Owner
No Items at this time
### 12.2.3 Business Employee
No Items at this time
### 12.2.4 Business Client
No Items at this time
### 12.2.5 Independent Home Owner
No Items at this time


Current implementation:

- `SystemSettings:SystemMode` is read from `appsettings.json` or Azure App Service settings. It accepts `Pool` or `Landscape` and defaults to `Pool` when missing or invalid.
- `SystemSettings:DevTest` is read from `appsettings.json` or Azure App Service settings. It is enabled only when configured as `true`.
- Pool mode names the application `PoolShark`, uses `/images/pool-waterfall-hero.png`, and shows Pool Equipment / Pool Configuration features.
- Landscape mode names the application `TreeShark`, uses `/images/landscape-fruit-trees-hero.png`, hides Pool Equipment navigation links, and redirects direct Pool Equipment routes back to the dashboard.
- DevTest mode displays the `Skip Google Auth` option on sign-in and registration, allows seeded test users to bypass Google authentication, and shows the collapsible `Test` menu.
- When DevTest is false, Google Auth cannot be skipped and the `Test` menu is hidden.
- The `/settings` page displays the configured `SystemMode`, `DevTest`, and active product name for System Administrators as read-only App Service configuration guidance.
- Section 14 currently gives `Settings / General` no role access, so storage-hosted General settings are not exposed in the navigation.


# 13. Test Menu
As the last menu item in the Nav menu have a collapsible "Test" section.
The Test Menu shall only be visible in DevTest Mode
Under the Test menu have a Test Page item with a link to a page called "Test Page". This page will be used for testing purposes and can include various test components and features that are being developed. It will serve as a sandbox environment for testing new functionality and ensuring that it works as expected before being released to production. The Test Page can be accessed by clicking on the "Test" menu item in the navigation menu, and it should be clearly labeled as a testing environment to avoid confusion with the main application features.
Under the Test menu have a Test Users item with a link to a page called "Test Users". This page will be used for managing test user accounts and their associated data. It will allow developers and testers to create, edit, and delete test user accounts, as well as view their details and activity within the application. The Test Users page can be accessed by clicking on the "Test" menu item in the navigation menu, and it should be clearly labeled as a testing environment to avoid confusion with the main application features. This will help ensure that test user management is organized and easily accessible for testing purposes.

Current implementation:

- The navigation menu ends with a collapsible `Test` section.
- The `Test` section contains `Test Page` and `Test Users` links.
- `/test` renders a sandbox page with the current session state and a clear testing-environment label.
- `/test/users` lets System Administrators create and edit test users, and delete/reactivate test users by toggling account status.


# 14. Settings
This describes the Settings Menu from the Navigation bar.
Each menu item is described in sections 14.1 - 14.8

## 14.0 System Administrator
This menu item will only be visible to SysAdmin users. The sections below describe the sub-menu items and their functionality.

### 14.0.1 Delete User
Functionality to delete a user from storage.
Ask for the user id and then delete any rows in tables associated with this user

### 14.0.2 User Roles
Allows managing User Roles. 

## 14.0.3 Pool Equipment
Allows managing Pool Equipment. 
If the Service is not setup as a pool cleaning service, do not show this menu item.

Features: 
- Seed Equipment 
- Manage Equipment Categories 
  - Create new Category
  - Allow searching and filtering
  - Columns: Active (Toggle), Category, Description
  - Actions: Edit, Delete - shown as icons 
- Manage Equipment
  - Create new Equipment
    - The category for a piece of pool equipment needs to be chosen, but there needs to be support for a Category called Uncategorized
  - Allow searching and filtering
  - Columns: Active (Toggle), Manufacturer, Category, Model No, Description
  - Actions: Edit, Delete - shown as icons 
All panels shall be collapsible

## 14.1 General
No Access
### 14.1.1 Sys Admin
No Access
### 14.1.2 Business Owner
No Access
### 14.1.3 Business Employee
No Access
### 14.1.4 Business Client
No Access
### 14.1.5 Independent Home Owner
No Access

## 14.2 Service Clients
Allows managing Service Clients. Service Clients are the Businesses that Business Owners create. The are customers of this service.
The clients listed need to belong to the service. Filter the clients by type. If the Service is setup as a pool cleaning service, only show pool cleaning clients.
Service clients cannot be created, as they are created via Business Owner Registration
Columns: Status (Toggle), Name, Type, Service Package (drop down), Email, Phone
Actions: Edit, Delete - shown as icons 
rename - old name was Companies

### 14.2.1 Sys Admin
Full Access
### 14.2.2 Business Owner
No Access
### 14.2.3 Business Employee
No Access
### 14.2.4 Business Client
No Access
### 14.2.5 Independent Home Owner
No Access

## 14.3 Business Clients
Allows managing Business Clients. Business Clients are the Clients or Customers of the Service Clients. They are generally home owners. 
The business clients listed need to belong to the service. Filter the clients by type. If the Service is setup as a pool cleaning service, only show pool cleaning business clients.
Business clients are created by the Business Owner
Columns: Status (Toggle), Client Type, Company, Name, Type, Email, Phone
Actions: Create, Edit, Delete - shown as icons 

### 14.3.1 Sys Admin
Full Access. A sys admin can see all clients. 
Columns: Status (Toggle), Company, Name, Type, Email, Phone
### 14.3.2 Business Owner
Full Access. A business owner can only see the clients of his business. 
Columns: Status (Toggle), Name, Type, Email, Phone
### 14.3.3 Business Employee
No Access
### 14.3.4 Business Client
No Access
### 14.3.5 Independent Home Owner
No Access

## 14.4 Users
Allows managing Users. 
Columns: Active (Toggle), Name, Email, Flags, Company, User Type (drop down), Approved (Toggle), Sys Admin (Toggle)
Actions: Edit - shown as icons 
Don't allow the logged in user to change his/her Status, Approval Status or User Type
## 14.4.1 Sys Admin
Column Access: All
Action Access: All
Has access to all users in the system. Users can't be added. The only way users get added into the system is via registration.
The ID should not be displayed, it must be handled in the background. Only a sys admin sees has the permission to make a user a sysadmin.
The users listed need to belong to the service. Filter the users by type. If the Service is setup as a pool cleaning service, only show users belonging to pool cleaning clients.
## 14.4.2 Business Owner
Columns: Active (Toggle), Name, Email, Flags, User Type, Approved (Toggle)
Actions: Edit - shown as icons 
Similar functionality as the Sys Admin with these exceptions:
Can only see users under associated with company. 
## 14.4.3 Business Employee
No Access
## 14.4.4 Business Client
No Access
## 14.4.5 Independent Home Owner
No Access

## 14.5 Pool Configuration
Allows the pool configuration for a home owner or business client to be managed
If the Service is not setup as a pool cleaning service, do not show this menu item.

Features: 
- Choose Client 
  - The clients listed need to belong to the service.
  - Once a client has been chosen, update the Panel text from Choose CLient to the Client Address
  - Add filtering capability
  - List all the Business Clients and allow the user to choose one
    - Columns: Company, Client Address, Client Type (Business Client or Independent Home Owner)
  - Any configuration changes made will be scoped to that client
- Configuration
  - Lists pool equipment owned by the home owner or business client 
  - Columns: Manufacturer, Name, Category, Model No, Comment (allow in-line edit)
  - Actions: Edit, Delete, Show Description - use icons
- Add Pool Equipment
  - Create new Equipment
  - Allows searching and filtering
  - Allows choosing a pievce of equipment which is then added to the Configuration list
  - Columns: Manufacturer, Name, Category, Model No, Description
  - Actions: Choose (icon)
    - A piece of equipment needs to be able to be added to a configuration multiple times. 
      For example a pump can be used for the Spa and Waterfall. 
- Equipment Pictures
  - Allows uploading of pictures and deleting them
  - Displays pictures and when the user clicks a picture it shows it larger

All panels shall be collapsible
## 14.5.1 Sys Admin
Access to all Business Clients or Independent Home Owners
## 14.5.2 Business Owner
Access to Business Clients
- Choose Client
  - Columns: Client Address
## 14.5.3 Business Employee
No Access
## 14.5.4 Business Client
No Access
## 14.5.5 Independent Home Owner
Access to Configuration, Add Pool Equipment, Equipment Pictures

## 14.6 Materials
Allows managing Materials. 
The Materials listed need to belong to the service. Filter the Materials by type. If the Service is setup as a pool cleaning service, only show pool cleaning Materials.

Features: 
- Seed Materials 
- Manage Material Categories 
  - Create new Category
  - Allow searching and filtering
  - Columns: Active (Toggle), Category, Description
  - Actions: Edit, Delete - shown as icons 
- Manage Materials
  - Create new Material
    - The category for a material needs to be chosen, but there needs to be support for a Category called Uncategorized
  - Allow searching and filtering
  - Columns: Active (Toggle), Manufacturer, Name, Category, Model No, Unit, Price, Description
  - Actions: Edit, Delete - shown as icons 
- Add Materials
  Allows adding global Materials to the Materials list
  No functionality to create a Material on this panel
  - Columns: Name, Category, Description
  - Actions: Choose - shown as icon

All panels shall be collapsible
## 14.6.1 Sys Admin
Access to all features, global scope
Allow edits and deletes only for all entities.
## 14.6.2 Business Owner
Access to Manage Materials and Add Materials. Newly created Materials will only be accessible to the customer. 
The Business Owner has access to all Global Materials Categories and Materials.
## 14.6.3 Business Employee
No Access
## 14.6.4 Business Client
No Access
## 14.6.5 Independent Home Owner
No Access

## 14.7 Services
Allows managing Services to be provided. 
The Services listed need to belong to the service. Filter the Services by type. If the Service is setup as a pool cleaning service, only show pool cleaning Services.

Features: 
- Seed Services 
- Manage Service Categories 
  - Create new Category
  - Allow searching and filtering
  - Columns: Active (Toggle), Category, Description
  - Actions: Edit, Delete - shown as icons 
- Manage Services
  - Create new Service
    - The category for a servicet needs to be chosen, but there needs to be support for a Category called Uncategorized
  - Allow searching and filtering
  - Columns: Active (Toggle), Name, Category, Duration, Price, Taxable, Description
  - Actions: Edit, Delete - shown as icons 
- Add Services
  Allows adding global services to the Services list
  No functionality to create a service on this panel
  - Columns: Name, Category, Description
  - Actions: Choose - shown as icon

All panels shall be collapsible
## 14.7.1 Sys Admin
Access to Seed Services, Manage Service Categories, Manage Serives. Created Categories and Services will have a global scope. 
Allow edits and deletes for all entities.
## 14.7.2 Business Owner
Access to Manage Services and Add Services. Newly Services will only be accessible to the customer. 
The Business Owner has access to all Global Service Categories and Services.
<!--Allow edits and deletes only for entities that do not have a global scope, and disable the buttons for global scoped entities.-->
## 14.7.3 Business Employee
No Access
## 14.7.4 Business Client
No Access
## 14.7.5 Independent Home Owner
Access to Manage Services and Add Services. Created Categories and Services will only be accessible to the home owner. 
The Independent Home Owner Owner has access to all Global Service Categories and Services.
Manage Services  
- Columns: Active (Toggle), Name, Category, Description

## 14.8 Service Packages
Allows managing Service Packages. 
The Service Packages listed need to belong to the service. Filter the Service Packages by type. If the Service is setup as a pool cleaning service, only show pool cleaning Service Packages.
A service package is the service the client will receive. The recurrence of the service package will be defined 
once a service package is assigned to a business client.

An example Service package is:
Name: Pool Service Level 1, Recurrence: Weekly, Cost: $129/month
Services:
  - Brushing, Recurrence: Every visit
  - Clean Skimmer and Pump Baskets, Recurrence: Every visit
  - Backwash Filter, Recurrence: Every 2 weeks
  - Deep Clean Filter, Recurrence: Twice yearly
  - Water Test, Recurrence: Monthly
  
When creating or modifying a service package, get the individual services from the global and service client services.

- Manage Service Packages 
  - Create new Service Packages. The Save Button needs to be at the bottom of the chosen services
  - Allow searching and filtering
  - Columns: Active (Toggle), Name, Recurrence, Description, Cost
  - Actions: Edit, Delete - shown as icons 
  When adding services to a service package, allow searching and filtering of the services
  - List the service with these columns
    - Service, Category, Scope, Recurrence (dropdown), Choose icon. When clicking the choose icon add the selected service into 
      the Service package being constructed
    - The recurrence dropdown should associated with the package should list the following:
      - Weekly, Bi-Weekly, Monthly, Bi-Monthly, Half-Yearly, Yearly
    - The recurrence dropdown associated with the service should list the following:
      - Every Visit, Every X Visits, where x is a number that can be provided

All panels shall be collapsible

## 14.8.1 Sys Admin
Access to all features, global scope
Allow edits and deletes for all entities.
## 14.8.2 Business Owner
Access to all features, service client scope 
Allow edits and deletes for all accessible entities.
## 14.8.3 Business Employee
No Access
## 14.8.4 Business Client
No Access
## 14.8.5 Independent Home Owner
No Access

Current implementation:

- The Settings menu is role-aware. System Administrators, Business Owners, and Independent Home Owners see only the Settings entries allowed for their role; Business Employees and Business Clients do not receive Settings menu access. Pending Business Users and Business Clients see only Home and Help.
- System Administrators receive a separate `System Administrator` navigation section with `Delete User` at `/admin/delete-user`, `User Roles` at `/admin/roles`, and Pool-mode `Pool Equipment` at `/admin/catalog/poolequipment`. Delete User asks for a user id and a typed `DELETE` confirmation, then physically removes the user, login lookup rows, company memberships, homeowner profile/photos/history, owner-scoped pool equipment, owner-scoped catalog rows, directly assigned visits, and email logs addressed to that user. User Roles manages built-in role display names, descriptions, owner-approval requirements, and permissions.
- `Settings / General` is not exposed in the navigation for any role. The `/settings` route remains as a read-only System Administrator view of appsettings-backed `SystemMode`, `DevTest`, and product name.
- System Administrators manage Service Clients at `/admin/service-clients`; the legacy `/admin/companies` route remains available. The page is manage-only, does not expose IDs, does not create service clients, uses active-state toggles, uses icon actions for edit/delete, filters the list by the configured service type, and includes a Service Package dropdown populated from the current service type's global service packages.
- System Administrators manage Business Clients at `/admin/clients` across all service clients, filtered by the configured service type. Business Owners manage their own Business Clients at `/clients`. Both views hide IDs, show the Business Client Type as `Home Owner`, provide create/edit/archive behavior, show active toggles, use icon actions, and include a Service Package dropdown/column populated from accessible service packages.
- System Administrators manage Users at `/admin/users`. Users are not created there; the grid hides IDs and shows Status, Name, Email, Flags, Company, User Type, Approved, and Sys Admin with toggle controls/dropdowns and icon edit actions. Company access is filtered by configured service type, unknown company types are not treated as matches, and the logged-in user cannot edit their own user row or change their own active status, approval status, system-admin flag, or user type. Business Owners manage company users at `/company/users` with company-scoped rows, User Type dropdowns, approval/status toggles, icon edit actions, and the same self-change restrictions for active status, approval status, and user type.
- Pool Equipment catalog management is available only to System Administrators from the `System Administrator` menu at `/admin/catalog/poolequipment`; Business Owners do not see Settings / Pool Equipment, and direct `/catalog/poolequipment` access redirects to the dashboard. It includes seeding for System Administrators, collapsible seed/category/equipment panels, active toggles, filtering/sorting, Uncategorized support, and icon edit/delete actions without exposing copy actions.
- Pool Configuration is available at `/poolequipment` for System Administrators, Business Owners, and Independent Home Owners in Pool mode. System Administrators get a collapsible client panel with service-type-filtered Business Clients, Independent Home Owners, filter support, and Company, Client Address, and Client Type columns; once selected, the panel title changes to the selected client address. Business Owners get a company-scoped client panel with filter support and Client Address. Configuration changes, owner-scoped equipment, and equipment pictures are saved under the selected client/homeowner scope. Independent Home Owners manage their own configuration directly. The page lists configured equipment with inline-editable Comment, uses icon actions for edit/delete/show-description, supports searchable global equipment selection, allows creating owner-scoped equipment with comments, allows the same global equipment item to be added multiple times by creating a new configured row each time, uses an icon-only Choose action, and provides collapsible equipment-picture upload/delete/zoom behavior.
- Materials management is available to System Administrators at `/admin/catalog/materials` and Business Owners at `/catalog/materials` per Section 14.6. Seed Materials and Material Category management are System Administrator only. The Materials panel is collapsible, searchable, sortable, uses active toggles, hides IDs, supports Uncategorized, and uses icon edit/delete actions.
- Business Owner Materials include an `Add Materials` panel for choosing global materials by icon action; that panel does not expose create controls. The material category picker can use global categories, which are copied into the company scope before saving.
- Services management is available to System Administrators at `/admin/catalog/services`, Business Owners at `/catalog/services`, and Independent Home Owners at `/settings/services` per Section 14.7. Seed Services and Service Category management are System Administrator only. Services are collapsible, searchable, sortable, support Uncategorized, use active toggles, and use icon edit/delete actions.
- Business Owner and Independent Home Owner Services include an `Add Services` panel for choosing global services by icon action; that panel does not expose create controls. The service category picker can use global categories, which are copied into the owner/company scope before saving. The Independent Home Owner services grid omits price, taxable, and duration fields per the role-specific Section 14.7 requirements.
- Service Packages management is available to System Administrators at `/admin/catalog/servicepackages` and Business Owners at `/catalog/servicepackages` per Section 14.8. System Administrators manage service-type-specific global packages, while Business Owners manage company-scoped packages. The page uses collapsible management, create/edit forms, filtering, sorting, active toggles, and icon edit/delete actions. Package recurrence is selected from Weekly, Bi-Weekly, Monthly, Bi-Monthly, Half-Yearly, and Yearly. Package services are chosen from a searchable/filterable list of accessible global and service-client services with Service, Category, Scope, recurrence controls, and an icon-only Choose action; chosen services are shown separately, can be removed, support Every Visit or Every X Visits recurrence, and the Save/Cancel buttons sit directly under the Chosen Services section before the Choose Services list.
- Landscape mode hides Pool Equipment and Pool Configuration Settings links, and direct Pool Equipment routes redirect to the dashboard.


