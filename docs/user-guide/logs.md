# Logs

Last reviewed: 2026-08-06

The Logs area lets authorized users review operational records such as email notification attempts and, for System Administrators, payment processing diagnostics.

## Email Log

Open Logs / Email Log from the main navigation.

Email Log shows:

- Status Counts for each delivery status.
- Email Details with From, To, CC, Subject, Status, SendDate, FailedMessage, and View.
- A date filter with Apply and Clear actions.

Use the View action to open the stored email body.

## Role Access

System Administrators see platform email logs and service-client logs for the current Pool or Landscape mode. Their table also includes Service Client.

Business Owners see email logs for their active service client.

Business Clients see only messages addressed to them.

Independent Home Owners see only messages addressed to them.

Business Employees do not have Logs access.

System Administrators also see:

- Payment Events
- Payment API

## Payment Events

Open Logs / Payment Events to review trusted Stripe/provider webhook events used for idempotency and subscription state updates.

Payment Events shows provider, mode, event type, status, related entity, summary, and received time.

## Payment API

Open Logs / Payment API to review sanitized checkout, customer portal, browser return, and webhook operation logs.

Payment API shows operation, status, provider references, user/subscription references, summary, and sanitized failure details. It does not store card data, secrets, raw webhook payloads, or Authorization headers.

## Delivery Statuses

- `New`: waiting for background email processing.
- `Queued`: recorded because email provider configuration is not available.
- `Sent`: sent or safely marked sent for a test recipient.
- `Failed`: delivery failed or the recipient address was invalid.
- `TestRerouted`: sent to the configured test inbox instead of the original test-user address.
- `Suppressed`: not sent because the recipient disabled email notifications.
