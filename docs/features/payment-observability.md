# Payment Observability

Status: Implemented
Owner: Product
Last reviewed: 2026-08-06

## Problem

System Administrators need enough payment diagnostics to support Stripe checkout, customer portal, browser return, and webhook problems without storing card data, secrets, or raw provider payloads.

## Personas

- System Administrator
- Support or operations user

## Requirements

- Keep trusted provider webhook events separate from general payment API operation logs.
- Continue using `PaymentProviderEvents` for webhook/provider event idempotency.
- Add sanitized `PaymentOperationLog` records for checkout session requests, checkout session results, portal session requests, portal session results, browser checkout returns, webhook processing, duplicate webhooks, and rejected webhooks.
- Do not store card data, raw webhook payloads, API keys, Authorization headers, or webhook signing secrets.
- Show Payment Events and Payment API pages under the Logs navigation group for System Administrators only.
- Enforce System Administrator authorization in the application service, not only in the NavBar.

## User Flows

### Review Provider Events

1. A System Administrator opens Logs / Payment Events.
2. The app loads persisted `PaymentProviderEvent` rows.
3. The System Administrator reviews provider, mode, event type, status, related entity, and summary.

### Review Payment API Operations

1. A System Administrator opens Logs / Payment API.
2. The app loads persisted `PaymentOperationLog` rows.
3. The System Administrator reviews operation, status, provider ids, user/subscription references, summary, and sanitized failure reason.

### Troubleshoot Checkout

1. An Independent Home Owner starts checkout.
2. The app records a checkout-session requested log.
3. The app records either a checkout-session succeeded log with provider ids or a failed log with a sanitized failure reason.
4. The browser return records a checkout-return log without activating access.

### Troubleshoot Webhooks

1. Stripe posts a webhook.
2. The app validates and processes the webhook.
3. The app records the provider event for idempotency.
4. The app records a payment API operation log for processed, duplicate, or rejected webhook outcomes.

## UI Expectations

- Logs / Payment Events is visible only to System Administrators.
- Logs / Payment API is visible only to System Administrators.
- Payment Events shows status counts and a provider-event detail table.
- Payment API shows status counts and an operation detail table.
- Empty states explain that no rows have been recorded yet.
- Failure text is sanitized and bounded.

## Data Model Impact

- `PaymentProviderEvent` remains the provider webhook event/idempotency record.
- `PaymentOperationLog` stores:
  - Unique id
  - Operation type
  - Operation status
  - Provider
  - Provider mode
  - Optional app user id
  - Optional app subscription id
  - Optional provider event id
  - Optional provider customer id
  - Optional provider subscription id
  - Optional provider checkout session id
  - Optional HTTP status code
  - Sanitized summary
  - Sanitized failure reason
  - Created timestamp
- Azure Table storage stores operation logs in `PaymentOperationLogs`.

## Authorization Rules

- Only System Administrators can view Payment Events.
- Only System Administrators can view Payment API logs.
- `PaymentLogService` enforces System Administrator authorization before returning either log list.
- Independent Home Owners, Business Owners, Business Employees, and Business Clients do not see these menu items.

## Acceptance Criteria

- [x] Checkout session requests are logged as payment API operations.
- [x] Successful checkout session creation is logged with provider checkout/customer/subscription references when available.
- [x] Failed checkout session creation is logged with sanitized failure text.
- [x] Customer portal session requests, successes, and failures are logged.
- [x] Browser checkout returns are logged without activating access.
- [x] Processed and duplicate webhooks are logged as payment API operations.
- [x] Rejected webhooks are logged without storing raw payloads.
- [x] Payment provider events remain separate from payment API operation logs.
- [x] Logs / Payment Events and Logs / Payment API are visible only to System Administrators.
- [x] Direct service access to payment logs requires System Administrator authorization.

## Tests

- `OnboardingTests.Homeowner_checkout_session_writes_payment_api_logs`
- `OnboardingTests.Webhook_processing_writes_provider_event_and_payment_api_log`
- `OnboardingTests.Payment_logs_require_system_admin`
- Existing payment integration tests continue to cover checkout reference persistence, webhook idempotency, duplicate events, and signature validation.

## User Documentation Impact

- Updated [Logs](../user-guide/logs.md) with System Administrator payment-log behavior.

## Current Implementation

- `PaymentOperationLog`, `PaymentOperationType`, and `PaymentOperationStatus` are defined in the domain model.
- `IServiceBusinessStore`, `InMemoryServiceBusinessStore`, and `AzureTableServiceBusinessStore` persist payment operation logs.
- `AzureStorageTableInitializer` provisions `PaymentOperationLogs`.
- `PaymentIntegrationService` records sanitized payment operation logs for checkout, portal, browser return, and webhook outcomes.
- `PaymentLogService` provides System Administrator-only access to payment provider events and payment operation logs.
- `PaymentEventsPage.razor` renders `/admin/payment-events`.
- `PaymentApiLogPage.razor` renders `/admin/payment-api`.
- `NavMenu.razor` shows Payment Events and Payment API under Logs for System Administrators only.

## Outstanding Tasks

- Add filtering by date, operation, and status if log volume grows.
- Add correlation ids across application logs, payment operation logs, and provider dashboard links.
- Consider an export action for support troubleshooting.

## Change Log

- 2026-08-06: Implemented Payment Observability with separate provider event and payment API operation logs, SysAdmin-only Logs menu entries, storage, pages, and tests.
