# Logs

Status: Implemented
Owner: Product
Last reviewed: 2026-08-06

## Problem

Service businesses and platform administrators need a role-aware audit view for email notification attempts so they can confirm what was sent, inspect failures, and troubleshoot customer communication without exposing other tenants' messages.

## Personas

- System Administrator
- Business Owner
- Business Client
- Independent Home Owner

Business Employees do not currently have Logs access.

## Requirements

- The Logs navigation group appears at the same navigation level as Settings and Reports, after Reports.
- Email Log is available from the Logs navigation group.
- Payment Events and Payment API are available from the Logs navigation group for System Administrators only.
- Email Log shows `EmailLogEntry` records the signed-in user is allowed to see.
- The first row shows count panels by email delivery status.
- The second row shows a collapsible Email Details panel.
- The Email Details panel supports an optional date filter.
- Email Log rows include From, To, CC, Subject, Status, SendDate, FailedMessage, and a View action.
- System Administrators also see a Service Client column.
- The View action opens the email body.
- The legacy System Administrator route `/admin/email-log` remains available.
- Non-admin roles use `/logs/email`.
- System Administrators can open `/admin/payment-events` and `/admin/payment-api`.

## User Flows

### Review Email Log

1. An authorized user opens Logs / Email Log.
2. The system loads email logs visible to the user's role and active app mode.
3. The user reviews status-count panels.
4. The user expands or collapses Email Details as needed.
5. The user scans the visible log table.

### Filter By Date

1. An authorized user opens Logs / Email Log.
2. The user selects a date.
3. The user applies the filter.
4. The system reloads visible logs created on that date.
5. The user clears the date filter to return to the full visible log list.

### View Message Body

1. An authorized user opens Logs / Email Log.
2. The user chooses the View icon for an email row.
3. The system opens a modal with the message subject, From, To, CC, Status, and Body.
4. The user closes the modal to return to the table.

### Review Payment Logs

1. A System Administrator opens Logs / Payment Events or Logs / Payment API.
2. The system loads payment logs through System Administrator-only services.
3. The System Administrator reviews provider events, API operation status, sanitized failure reasons, and provider references.

## UI Expectations

- The page title is Email Log.
- The page includes a role-specific eyebrow.
- Status Counts render one metric tile per `EmailDeliveryStatus`.
- Email Details is a collapsible panel and is expanded by default.
- Date filtering uses a date input with Apply and Clear actions.
- Empty filtered states explain that no matching email log entries were found.
- System Administrators see Service Client, From, To, CC, Subject, Status, SendDate, FailedMessage, and Action columns.
- Business Owners, Business Clients, and Independent Home Owners see From, To, CC, Subject, Status, SendDate, FailedMessage, and Action columns.
- The View action is an icon-only button with accessible label text.
- The body modal displays email metadata and the stored body.
- Payment Events shows trusted provider webhook/idempotency rows.
- Payment API shows sanitized checkout, portal, return, and webhook operation rows.

## Data Model Impact

- `EmailLogEntry` stores:
  - Unique id
  - Optional service client id
  - Email type
  - Recipient user id
  - Original recipient email
  - Actual recipient email
  - Subject
  - Body
  - Delivery status
  - Provider message id
  - Failure reason
  - Created timestamp
  - Sent timestamp
  - From email
  - CC email
- `EmailDeliveryStatus` includes `New`, `Queued`, `Sent`, `Failed`, `TestRerouted`, and `Suppressed`.
- `IServiceBusinessStore` exposes email log read and upsert operations.
- Azure Table storage stores email logs in the `EmailLogs` table, partitioned by service client when present and by `PLATFORM` for platform records.
- `PaymentProviderEvent` stores trusted provider event/idempotency rows.
- `PaymentOperationLog` stores sanitized payment API operation rows.

## Authorization Rules

- System Administrators can view platform logs plus service-client logs for the current Pool or Landscape app mode.
- Business Owners can view logs for their active service client.
- Business Employees cannot view email logs.
- Business Clients can view only messages addressed to them or their linked client email.
- Independent Home Owners can view only messages addressed to them.
- Payment Events and Payment API are visible only to System Administrators.

## Acceptance Criteria

- Implemented: Logs appears after Reports in the authenticated navigation for authorized roles.
- Implemented: Business Employees do not receive Logs navigation and receive no log rows from the service.
- Implemented: System Administrators can open `/admin/email-log` or `/logs/email`.
- Implemented: Non-admin authorized roles can open `/logs/email`.
- Implemented: System Administrator log visibility is filtered by active app mode.
- Implemented: Business Owner log visibility is scoped to owned service clients.
- Implemented: Business Client and Independent Home Owner visibility is scoped to their own messages.
- Implemented: The page shows status-count panels.
- Implemented: The page supports date filtering and clearing.
- Implemented: The table shows the required columns, with Service Client shown only to System Administrators.
- Implemented: The View action shows the stored message body.
- Implemented: Email notification queuing and processing record log entries for sent, failed, queued, rerouted, and suppressed messages.
- Implemented: System Administrators can open Logs / Payment Events.
- Implemented: System Administrators can open Logs / Payment API.
- Implemented: Payment-log services enforce System Administrator authorization.

## Tests

- `EmailNotificationTests.Approval_email_for_test_user_is_rerouted_and_logged`
- `EmailNotificationTests.Approval_email_is_suppressed_when_user_disables_email_notifications`
- `EmailNotificationTests.Email_log_service_filters_system_admin_logs_by_system_mode`
- `EmailNotificationTests.Email_log_service_limits_business_owner_to_owned_company`
- `EmailNotificationTests.Email_log_service_blocks_business_employee_access`
- `EmailNotificationTests.Email_log_service_limits_client_and_homeowner_to_their_messages`
- `EmailNotificationTests.Email_log_service_filters_visible_logs_by_created_date`
- `EmailNotificationTests.Email_job_marks_test_user_messages_sent_without_provider_send`
- `EmailNotificationTests.Email_job_sends_non_test_user_messages_through_provider`
- `EmailNotificationTests.Email_job_records_provider_failure_message`
- `OnboardingTests.Payment_logs_require_system_admin`

## User Documentation Impact

- Updated `docs/user-guide/logs.md` for role-specific Email Log behavior and System Administrator payment logs.
- User documentation should be updated if resend actions, export/download, provider diagnostics, or audit-event logs are added.

## Current Implementation

- The Logs navigation group appears after Reports for System Administrators, Business Owners, Business Clients, and Independent Home Owners.
- Business Employees do not receive the Logs navigation group.
- `AdminEmailLogPage` implements Email Log at `/logs/email` and retains `/admin/email-log`.
- `EmailLogService.GetVisibleEmailLogsAsync` centralizes role visibility rules and date filtering.
- System Administrator company labels are resolved through `PlatformAdminService.GetCompaniesAsync`; logs without a company id display as Platform.
- Email notification queueing writes `EmailLogEntry` records for account approval, service completion, invoice, and related notification events.
- `EmailJobService.ProcessNewEmailLogsAsync` processes `New` log entries, sends through the configured sender for real recipients, records provider ids, marks invalid recipients as failed, and marks test recipients as sent without provider delivery.
- Email logs are persisted by both `InMemoryServiceBusinessStore` and `AzureTableServiceBusinessStore`.
- `PaymentEventsPage` implements Payment Events at `/admin/payment-events`.
- `PaymentApiLogPage` implements Payment API at `/admin/payment-api`.
- `PaymentLogService` centralizes System Administrator-only payment log access.

## Outstanding Tasks

- Add optional resend support for failed messages if product requirements call for it.
- Add CSV/export support for operational troubleshooting.
- Add provider-specific diagnostics or correlation ids if Azure Communication Services exposes them consistently.
- Decide whether System Administrators need cross-mode search in addition to current app-mode filtering.
- Consider masking or redacting message bodies for sensitive email types.
- Add filtering/export support for Payment Events and Payment API if operational volume requires it.

## Change Log

- 2026-08-04: Created the canonical Logs feature spec from `Logs-oldFormat.md`, captured implemented behavior, acceptance criteria status, tests, user documentation impact, and outstanding tasks.
- 2026-08-06: Added System Administrator Payment Events and Payment API log pages under Logs.
