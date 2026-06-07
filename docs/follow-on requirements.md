# Service Business Follow-On Requirements

## 1. Landing Page

When a user navigates to the home page, they should see a clear and concise landing page that provides an overview of the service business. 
The landing page should include a picture of a nice pool with waterfall in a lush landscape.
The user can then either Sign In or Register to access the full features of the service business.

### 1.1 Register
When a user clicks on the "Register" button, they should be taken to a registration page where they can create an account. 
They need to register as either a business owner, business user or business client.
Once they pick that, they will authenticate using a gmail account and then taken to a page where they can fill out the necessary information to complete their registration.
If they register as a business owner, they will be taken to a page where they can create their business profile and set up their services.
If they register as a business user, they will be given a dropdown to choose which business they want to be associated with and then taken to a page where they can fill out their profile information and set up their account settings.
They will not have access to the service until the business owner approves their account. Once approved, they will be able to access the features corresponding to their role.
If they register as a business client, they will be given a dropdown to choose which business they want to be associated with and then taken to a page where they can fill out their profile information and set up their account settings. 
They will not have access to the service until the business owner approves their account. Once approved, they will be able to access the features corresponding to their role.

## 1.2 Sign In
When a user clicks on the "Sign In" button, they should be taken to a sign-in page where they can enter their credentials to access their account.
They will be taken to a Dashboard page where they can see their profile information, manage their account settings, and access the features based on their Role.

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
- System admins can view user summaries, company access, pending access counts, and role status from `/admin`.
- System admins can promote or remove system-admin privileges for already registered users.
- System admins can enable or disable users.
- A system admin cannot disable their own account.
- The service prevents removing or disabling the last active system admin.

### 1.8.1 System Admin Functions
User Management
Role Management

Current implementation:

- User management is implemented in `PlatformAdminService`.
- The System Admin dashboard exposes Make Admin, Remove Admin, Disable, and Enable actions.
- Role management is implemented in `PlatformAdminService`.
- The System Admin dashboard exposes editable role display names, descriptions, owner-approval requirements, and permissions.
- Role identities remain the built-in `CompanyAdmin`, `CompanyUser`, and `CompanyClientUser` values for this slice.

### 1.8.1 System Admin User Dashboard
The System Admin should have access to a dashboard where they can view the overall state of the system.
Summary of users, roles, and account approval status.
Summary of businesses and their associated users.

Current implementation:

- `/admin` shows companies, total users, system admins, pending memberships, disabled users, email log count, company summaries, role/access summaries by user, and recent email logs.

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
- The System Admin dashboard displays total email log count and the latest sent-email log entries with recipient, subject, type, status, timestamp, and reroute details.

# 2.2 Test User Support
When sending emails, ensure that test users are handled appropriately. Test users should be able to receive emails without affecting real users. You can implement a mechanism to identify test users and route their emails to a designated test email address or log them separately for testing purposes. This will allow you to verify the email functionality without sending emails to actual users during development and testing phases.
The test user accounts will not be real accounts, so we cannot send emails there. So the user definition should have an email accress, but if the email address is the rowkey of the table, the table still needs a recipient email address.

Current implementation:

- `AppUser.Email` remains the identity/login email.
- `AppUser.NotificationEmail` stores the normal notification target.
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
- Company-owner approval/rejection for pending company access is implemented at `/approvals`.
- Company-owner enable/disable for existing company users remains a follow-up slice.

# 6.2 Role Management
There needs to be the ability to define and manage roles in the system. This includes the ability to view and edit all roles. 
A role is defined by a name and a set of permissions. Permissions define what actions a user with that role can perform in the system. The system should enforce access control based on the user's role, ensuring that users can only access features and information relevant to their role (business owner, business user, or business client).

Current implementation:

- Role definitions are stored as `RoleDefinition` records with role, display name, description, and owner-approval requirement.
- Role definitions include an editable permission list.
- System admins can view and update role metadata and permissions from `/admin`.
- The authorization layer enforces role checks using active company memberships and system-admin flags.
- Runtime authorization still uses the fixed role identities for protected workflows; mapping every individual permission string to enforcement policies remains a future enhancement.

# 6.3 User Profile
Once a user has successfully autheticated, a User Profile indicator should show up at the top right of the page. By clicking that indicator, the users profile page should be shown allowing the user to change his or or profile.
A user should be able to logout from the profile page.

Current implementation:

- The authenticated shell shows a top-right profile indicator with the current user's display name.
- `/profile` allows the current user to edit display name, notification email, and phone.
- The profile page displays immutable login email, profile image when available, and account status.
- Users can log out from the top-right profile area or from `/profile`.

# 7. Workflow

# 7.1 Initial Workflow
- When a user logs in with the seeded sysadmin account, he/she will need to update another user to a sysadmin, so this seeded sys-admin account can be disabled.

# 8. User Interface 

# 8.1 Nav menu
A menu item should be hidden from a user if he does not have access to the functionality. 
A non-system admin should not see the System Admin menu

Current implementation:

- Navigation loads the current user's active company access and system-admin flag.
- Company Admin links are visible only to users with active `CompanyAdmin` membership.
- Field links are visible only to users with active `CompanyUser` membership.
- Client Portal is visible only to users with active `CompanyClientUser` membership.
- System Admin is visible only when `AppUser.IsSystemAdmin` is true.
