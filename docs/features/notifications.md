# Notifications

Status: Implemented
Owner: Product
Last reviewed: 2026-08-05

## Problem

Users need important workflow messages such as approval decisions, completed visits, and invoice messages to be recorded reliably, sent when configured, and safe for test users and development environments.

## Personas

- System Administrator
- Business Owner
- Business Employee
- Business Client
- Independent Home Owner

## Requirements

- Notification attempts are persisted as `EmailLogEntry` records.
- Account approval decision emails are queued when access requests are approved or rejected.
- Visit completion emails are queued when field work is completed.
- Invoice generation creates invoice email log rows.
- Users can maintain a notification email and email notification preference from the profile page.
- Test users can be rerouted to a configured test recipient.
- Disabled email notifications are logged as `Suppressed`.
- The email job processes `New` email log rows through the configured email sender.
- DevTest mode and test-recipient rows are marked `Sent` without provider delivery.

## User Flows

### Approval Decision Notification

1. A System Administrator or Business Owner approves or rejects an access request.
2. The application queues an `AccountApprovalDecision` email log.
3. The row is sent, rerouted, suppressed, queued, or failed based on configuration and recipient preferences.

### Visit Completion Notification

1. A Business Employee completes a visit.
2. The application queues a `VisitCompleted` notification.
3. The notification attempt is logged and may be sent through Azure Communication Services.

### Process Email Backlog

1. `EmailJobService.ProcessNewEmailLogsAsync` loads `EmailLogEntry` rows with `New` status.
2. DevTest or test recipients are marked `Sent` without provider delivery.
3. Invalid recipient emails are marked `Failed`.
4. Real recipients are sent through `IEmailSender` and updated to `Sent` or `Failed`.

## UI Expectations

- Profile page exposes Notification email and Email notifications.
- Email Log page shows notification records, status, recipients, subject, failure reason, send date, and body detail.
- There is no dedicated Notifications page.

## Data Model Impact

- `AppUser.NotificationEmail` stores the preferred notification recipient.
- `AppUser.EmailNotificationsEnabled` stores the user's notification preference; missing legacy values are treated as enabled.
- `EmailLogEntry` stores email type, company id, recipient user id, original recipient, actual recipient, subject, body, delivery status, provider message id, failure reason, created timestamp, sent timestamp, from email, and cc email.

## Authorization Rules

- Users can update their own notification email and preference from Profile.
- System Administrators can view mode-filtered platform and service-client email logs.
- Business Owners can view logs for their service client.
- Business Clients and Independent Home Owners can view only messages addressed to them.
- Business Employees do not receive Email Log access.

## Acceptance Criteria

- [x] Approval decision notifications are logged.
- [x] Visit completion notifications are logged.
- [x] Invoice email rows are logged during invoice creation.
- [x] User notification email and opt-in preference are persisted.
- [x] Disabled notifications are logged as `Suppressed`.
- [x] Test users can be rerouted to `Email:TestRecipientEmail`.
- [x] Missing Azure Communication Services settings queue notification rows instead of failing the workflow.
- [x] Email job processes `New` rows and records provider success/failure.
- [x] DevTest and test recipients are marked sent without provider delivery.
- [ ] Immediate notification queueing and email-job processing use different status paths (`Queued`/direct send versus `New` backlog).

## Tests

- `EmailNotificationTests.Approval_email_for_test_user_is_rerouted_and_logged`
- `EmailNotificationTests.Approval_email_is_suppressed_when_user_disables_email_notifications`
- `EmailNotificationTests.Email_job_marks_test_user_messages_sent_without_provider_send`
- `EmailNotificationTests.Email_job_sends_non_test_user_messages_through_provider`
- `EmailNotificationTests.Email_job_records_provider_failure_message`
- `EmailNotificationTests.Email_job_records_invalid_recipient_failure_without_provider_send`
- Email Log visibility tests in `EmailNotificationTests`

## User Documentation Impact

- User-facing profile behavior is documented in [Notifications](../user-guide/notifications.md).
- Email Log visibility is documented in [Logs](../user-guide/logs.md).

## Current Implementation

- `AzureCommunicationEmailNotificationQueue` queues approval and visit-completion notifications and may send immediately when ACS settings are configured.
- `AzureCommunicationEmailSender` sends `EmailLogEntry` rows for the email job.
- `EmailJobService` processes `New` email log rows.
- `EmailLogService` controls visible log access by persona.
- `UserProfileService.UpdateCurrentProfileAsync` updates notification email and email notification preference.
- `ServiceBusinessTelemetry.EmailNotifications` increments for queued/sent/suppressed/failed notification attempts.

## Outstanding Tasks

- Decide whether all notifications should use the `New` email-log backlog plus `EmailJobService`.
- Add user-facing notification preference coverage beyond service tests.
- Add retry policy and attempt counts if email delivery needs operational retries.

## Change Log

- 2026-08-05: Created implemented notification spec from email queue, job, profile, logs, telemetry, and tests.
