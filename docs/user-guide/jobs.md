# Jobs

Last reviewed: 2026-08-04

Jobs are background-style application services that perform follow-up work for invoicing and email delivery. There is not currently a dedicated Jobs page.

## Where You See Job Results

- Invoices created by the invoicing job appear on the Invoices page.
- Invoice email messages created by the invoicing job appear in Logs / Email Log with `New` status until processed.
- Sent or failed email attempts appear in Logs / Email Log.

## Invoicing Job

The invoicing job creates invoices from closed service visits that do not already have an invoice id.

When an invoice is created, the system:

- Stores the invoice.
- Stores the generated invoice HTML.
- Updates the related visit with the invoice id.
- Creates an invoice email log entry with `New` status.

## Emailing Job

The emailing job processes Email Log rows with `New` status.

For test users or DevTest mode, the job marks the message as `Sent` without sending it through the email provider.

For non-test users, the job sends the email through the configured provider and updates the Email Log row to `Sent` or `Failed`.

## Automatic Scheduler

When the WebApp is running, a hosted scheduler automatically runs the invoicing job and then the emailing job.

The scheduler can be configured with:

- `Jobs:Scheduler:Enabled`
- `Jobs:Scheduler:InitialDelaySeconds`
- `Jobs:Scheduler:IntervalMinutes`

By default, the scheduler is enabled, waits 60 seconds after startup, and then runs every 5 minutes.

## Current Limitations

- Retry count, retry scheduling, and dead-letter handling are not currently shown to users.
- There is no dedicated Jobs page or job history page.
