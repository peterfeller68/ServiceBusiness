# Jobs

Status: Implemented
Owner: Product
Last reviewed: 2026-08-04

## Problem

Service businesses need durable application jobs for work that should be performed after a user-facing workflow succeeds, including creating invoices from closed visits and sending queued email notifications without blocking the originating workflow.

## Personas

- System Administrator
- Business Owner
- Business Client
- Independent Home Owner
- Business Employee

Jobs do not currently expose a dedicated user-facing Jobs page. Users see job outcomes through Invoices, Logs / Email Log, dashboards, and email notifications.

## Requirements

- The invoicing job scans closed service visits that do not have an invoice id.
- The invoicing job creates a customer-facing invoice for each eligible closed visit.
- Invoice HTML is generated and stored with the invoice.
- Invoice creation updates the related service visit with the new invoice id.
- Invoice creation writes an invoice email row to the EmailLogs table with `New` status.
- The emailing job scans EmailLogs rows with `New` status.
- The emailing job sends non-test-user messages through the configured email sender.
- The emailing job never sends messages to test users through the email provider.
- The emailing job marks test-user messages as `Sent` without provider delivery.
- The emailing job marks successful provider sends as `Sent`.
- The emailing job marks failed provider sends or invalid recipient addresses as `Failed` and records a failure message.

## User Flows

### Create Invoices For Closed Visits

1. A job caller invokes `InvoicingJobService.CreateInvoicesForCompletedVisitsAsync`.
2. The service scans active service clients.
3. The service finds closed visits without invoice ids.
4. The service creates invoices, stores invoice HTML, updates visits with invoice ids, and writes `New` invoice email log entries.
5. Users see the created invoices on the Invoices page and the queued invoice messages in Logs / Email Log.

### Create One Invoice

1. A System Administrator or Business Owner uses the Invoices page to create an invoice for a selected closed visit.
2. The page calls `InvoicingJobService.CreateInvoiceForVisitAsync`.
3. The service validates that the visit is closed and not already invoiced.
4. The service creates the invoice, updates the visit, and creates a `New` invoice email log entry.

### Process New Emails

1. A job caller invokes `EmailJobService.ProcessNewEmailLogsAsync`.
2. The service loads EmailLogs rows with `New` status in creation order.
3. DevTest mode or test-recipient rows are marked `Sent` without provider delivery.
4. Non-test rows with valid recipient addresses are sent through `IEmailSender`.
5. The service stores `Sent` or `Failed` results, including provider message id or failure message when available.

### Automatic Scheduled Run

1. The WebApp starts `ServiceBusinessJobScheduler` when the host starts.
2. The scheduler waits for the configured initial delay.
3. The scheduler creates a scoped `ScheduledJobRunner`.
4. The runner creates invoices for eligible closed visits and then processes `New` email log rows.
5. The scheduler repeats the run at the configured interval until the host stops.

## UI Expectations

- Jobs do not currently have a dedicated UI.
- Invoice job output appears in the Invoices page as New invoices.
- Invoice email job output appears in Logs / Email Log as `New`, `Sent`, or `Failed` email rows.
- Users should not need to manually inspect jobs to complete ordinary visit, invoice, or email workflows.
- Operations users can disable the hosted scheduler or change its timing through `Jobs:Scheduler` configuration.

## Data Model Impact

- `ServiceVisit.InvoiceId` links closed visits to generated invoices.
- `Invoice` stores invoice id, visit id, business client id, service package id, billable services, billable materials, total cost, status, generated HTML, and timestamps.
- `EmailLogEntry` acts as the current email job table for invoice emails waiting to be sent and for final delivery audit history.
- `EmailDeliveryStatus.New` means a row is waiting for `EmailJobService`.
- `IServiceBusinessStore` exposes visit, invoice, and email-log read/write operations used by the job services.

## Authorization Rules

- Job services do not perform user-facing role checks when invoked directly.
- User-facing invoice creation is authorized by the Invoices page and `CompanyAdminService` workflows for System Administrators and Business Owners.
- Email log visibility is authorized by `EmailLogService`, not by `EmailJobService`.
- Any future scheduled or external job trigger must be restricted to trusted application infrastructure.

## Acceptance Criteria

- Implemented: Closed visits without invoice ids are picked up for invoice creation.
- Implemented: Visits that already have invoice ids are not reinvoiced.
- Implemented: Invoice HTML is generated and stored.
- Implemented: Invoice creation stores an invoice and updates the visit invoice id.
- Implemented: Invoice creation writes an EmailLogs row with `EmailType` `Invoice` and `New` status.
- Implemented: Email job processing selects only `New` EmailLogs rows.
- Implemented: Test-user messages are marked `Sent` without provider delivery.
- Implemented: DevTest mode marks `New` messages `Sent` without provider delivery.
- Implemented: Non-test-user messages are sent through `IEmailSender`.
- Implemented: Successful sends store `Sent`, provider message id when supplied, and sent timestamp.
- Implemented: Invalid recipient addresses are marked `Failed` with a failure message.
- Implemented: Provider failures are marked `Failed` with a failure message.
- Implemented: A hosted scheduler automatically invokes the invoicing and email job services.
- Not implemented: Retry count, backoff, and dead-letter handling for failed email rows.

## Tests

- `InvoiceJobTests.Invoicing_service_creates_invoice_for_closed_visit_without_invoice`
- `InvoiceJobTests.Invoicing_service_does_not_reinvoice_visits`
- `EmailNotificationTests.Email_job_marks_test_user_messages_sent_without_provider_send`
- `EmailNotificationTests.Email_job_sends_non_test_user_messages_through_provider`
- `EmailNotificationTests.Email_job_records_provider_failure_message`
- `EmailNotificationTests.Email_job_records_invalid_recipient_failure_without_provider_send`
- `InvoiceJobTests.Scheduled_job_runner_creates_invoices_and_processes_new_email_logs`

## User Documentation Impact

- Updated `docs/user-guide/jobs.md` to explain automatic scheduler behavior and configuration.
- User documentation should be updated when retry status or a dedicated Jobs page is added.

## Current Implementation

- `InvoicingJobService.CreateInvoicesForCompletedVisitsAsync` scans active service clients for closed visits with no `InvoiceId`.
- `InvoicingJobService.CreateInvoiceForVisitAsync` creates one invoice for a selected closed visit and rejects visits that are not closed or already invoiced.
- Invoice calculation bills ad-hoc planned services, ad-hoc out-of-scope services, service-package out-of-scope services, and out-of-scope materials.
- Generated invoice HTML includes service client, business client, visit, line items, and total.
- Invoice creation writes an `EmailLogEntry` with `EmailType` `Invoice`, generated HTML body, service-client sender, business-client recipient, and `New` status.
- `EmailJobService.ProcessNewEmailLogsAsync` processes `New` EmailLogs rows in `CreatedUtc` order.
- `EmailJobService` uses `IEmailSender`; the WebApp registers `AzureCommunicationEmailSender`.
- `EmailJobService` treats DevTest mode and test recipients as safe sends without provider delivery.
- `ScheduledJobRunner.RunOnceAsync` runs the invoicing pass first and then the email pass so invoice emails created during the run can be processed in the same cycle.
- `ServiceBusinessJobScheduler` is registered as a hosted service in the WebApp.
- `Jobs:Scheduler:Enabled` controls whether the hosted scheduler runs.
- `Jobs:Scheduler:InitialDelaySeconds` controls the initial startup delay and defaults to 60 seconds.
- `Jobs:Scheduler:IntervalMinutes` controls the recurring interval and defaults to 5 minutes, with a minimum effective interval of 1 minute.
- Existing notification queue methods for account approvals and visit completion write email logs and may send immediately when Azure Communication Services is configured.
- The WebApp registers `InvoicingJobService`, `EmailJobService`, and `ScheduledJobRunner` for scoped DI.

## Outstanding Tasks

- Add retry metadata, retry policy, and dead-letter handling for failed email sends.
- Add observability events around job start, completion, skipped rows, and failures.
- Add an operations-facing job history page if support users need direct job monitoring.
- Decide whether immediate notification sending should move fully to the `New` EmailLogs plus `EmailJobService` model.
- Decide whether to keep the App Service hosted scheduler long term or move scheduled execution to Azure Functions as the workload grows.

## Change Log

- 2026-08-04: Created the canonical Jobs feature spec from `Jobs-oldFormat.md`, captured implemented invoicing and email job behavior, acceptance criteria status, tests, user documentation impact, and outstanding tasks.
- 2026-08-04: Implemented hosted scheduler execution for invoicing and email jobs, added scheduler-runner test coverage, and documented scheduler configuration.
