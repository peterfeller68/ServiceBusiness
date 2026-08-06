# Payment Integration

Status: In Progress
Owner: Product
Last reviewed: 2026-08-06

## Problem

The service needs a secure provider integration for collecting fees, confirming subscription/payment status, and recording payment events without storing card data or trusting browser-only checkout redirects.

## Personas

- System Admin
- Independent Home Owner
- Business Owner
- Support or operations user

## Requirements

- Integrate with Stripe without storing card data in the application.
- Create provider-hosted checkout sessions and customer portal sessions from app-owned subscription context.
- Store provider customer, subscription, checkout session, invoice, and payment identifiers as references.
- Process provider webhooks as the trusted source of truth for payment and subscription state changes.
- Do not activate paid access based only on the browser returning from checkout.
- Handle succeeded, failed, canceled, past-due, resumed, and refunded provider states where applicable.
- Process webhook events idempotently so duplicate provider events do not create duplicate state changes.
- Log payment-provider events and sanitized payment API operations for operational review without logging secrets, card data, raw webhook payloads, or unnecessary personal data.
- Support separate configuration for development/test and production provider modes.

## User Flows

### Subscription Checkout

1. The subscriptions feature creates or loads a pending subscription.
2. The app asks the payment integration to create a provider-hosted checkout session.
3. The user completes checkout with the provider.
4. The browser returns to the app with a success, cancel, or pending result.
5. The app shows a waiting, confirmation, retry, or canceled state based on local and provider-confirmed status.
6. A validated webhook updates the app subscription to `Trialing`, `Active`, `PastDue`, `Canceled`, or another canonical subscription status.

### Payment Event Processing

1. The provider sends a webhook event to the app.
2. The app validates the webhook signature.
3. The app checks whether the provider event id has already been processed.
4. The app maps the provider event to an app-level subscription, invoice, or payment state change.
5. The app records the event outcome and updates the related app record.

### Payment Method or Subscription Management

1. An authorized user opens subscription or billing management.
2. The app creates a provider-hosted customer portal session when supported.
3. The user updates payment method, cancellation, or billing details with the provider.
4. Webhooks synchronize resulting state changes back into the app.

## UI Expectations

- The application does not collect credit card fields directly.
- Checkout and customer payment-method management are provider-hosted.
- Return pages clearly distinguish completed, canceled, failed, and pending confirmation states.
- Profile, subscription, invoice, or billing pages display app-level payment/subscription status rather than raw provider event details.
- System Admin or operations views can inspect payment event status enough to support failed or pending payments.

## Data Model Impact

- Add provider-reference fields to subscription and invoice/payment records as needed.
- Add `PaymentProviderEvent` records keyed by provider event id for idempotency.
- Add `PaymentOperationLog` records for sanitized checkout, portal, browser return, webhook processed, webhook duplicate, and webhook rejected diagnostics.
- Store event type, provider mode, related app entity id, processing status, processed timestamp, and a sanitized summary.
- Store provider customer id and subscription id on subscription records when subscription billing is enabled.
- Store provider invoice, payment intent, charge, or payment link ids on invoice/payment records when invoice payment collection is enabled.

## Authorization Rules

- Public browser return endpoints may display only safe confirmation or retry information.
- Webhook endpoints validate provider signatures before processing.
- Independent Home Owners can start or resume checkout only for their own subscription.
- Business Owners can start or manage payment flows only for records they are authorized to manage.
- System Admins can view provider integration status and sanitized payment event logs.
- No user can directly set trusted payment or subscription status from the client UI.

## Acceptance Criteria

- Implemented: The app can create a provider-hosted checkout session from an app-owned homeowner subscription context.
- Implemented: The app can create a provider-hosted Stripe Customer Portal session for linked homeowner customers.
- Implemented: The app model stores provider customer, subscription, checkout, invoice, and payment identifiers as references only.
- Implemented: Browser checkout return does not activate paid access.
- Implemented: Signed Stripe webhooks can update subscription status through `PaymentIntegrationService`.
- Implemented: Duplicate provider events are idempotent.
- Implemented: Stripe subscription and invoice events are mapped to app-level statuses.
- Implemented: Payment event logs are persisted for support/operations review.
- Implemented: Payment API operation logs are persisted separately from provider event/idempotency rows.
- Implemented: The app does not store card data.

## Tests

- `OnboardingTests.Payment_provider_events_are_idempotent_and_drive_subscription_status`
- `OnboardingTests.Homeowner_checkout_session_updates_subscription_provider_references`
- `OnboardingTests.Homeowner_customer_portal_requires_provider_customer`
- `OnboardingTests.Webhook_processing_updates_subscription_once_for_duplicate_provider_event`
- `OnboardingTests.Homeowner_checkout_session_writes_payment_api_logs`
- `OnboardingTests.Webhook_processing_writes_provider_event_and_payment_api_log`
- `OnboardingTests.Payment_logs_require_system_admin`
- `StripePaymentProviderGatewayTests.Stripe_webhook_parser_validates_signature_and_extracts_checkout_session`
- `StripePaymentProviderGatewayTests.Stripe_webhook_parser_rejects_invalid_signature`
- Missing: end-to-end tests against Stripe test mode.

## User Documentation Impact

- Updated subscription user-guide notes for Stripe checkout, webhook-confirmed activation, and customer portal behavior.
- Added `docs/operations/stripe-payment-setup.md` for provider keys, price ids, webhook secrets, provider mode, and webhook endpoint setup.

## Current Implementation

- `PaymentProviderEvent`, `PaymentOperationLog`, `PaymentEventProcessingStatus`, and `PaymentOperationStatus` are defined in the domain model.
- `PaymentIntegrationService` creates homeowner checkout sessions, creates customer portal sessions, records provider events, records sanitized operation logs, and returns duplicate status for repeated provider event ids.
- `PaymentIntegrationService.ProcessWebhookAsync` parses signed Stripe webhooks and updates homeowner subscription status only through a trusted event processing path.
- `StripePaymentProviderGateway` creates Stripe Checkout Sessions, creates Stripe Customer Portal Sessions, validates webhook signatures, and maps supported Stripe events to app-level subscription updates.
- `/billing/homeowner/checkout` redirects authorized homeowners to Stripe Checkout.
- `/billing/homeowner/portal` redirects linked homeowners to the Stripe Customer Portal.
- `/billing/stripe/checkout-return` redirects back to Profile without activating access.
- `/billing/stripe/webhook` validates and processes Stripe webhook payloads.
- In-memory and Azure Table stores persist payment provider events and payment operation logs.
- Logs / Payment Events and Logs / Payment API expose System Administrator-only operational views.
- The app does not expose any browser-return endpoint that can activate access by itself.

## Outstanding Tasks

- Add end-to-end validation with Stripe test mode after account products, prices, portal settings, and webhook endpoint are configured.
- Extend Stripe payment links to application-managed invoices when invoice payment collection is prioritized.

## Feature Dependencies

- Subscriptions defines app-level subscription statuses and entitlement behavior.
- Registration and Authentication can initiate subscription checkout for Independent Home Owner onboarding.
- Invoicing can later reuse payment-provider records for invoice payment collection.
- Observability and Logs provide operational visibility into payment processing failures.

## Implementation Notes

- Keep provider-specific code behind an integration boundary so Stripe can be replaced or mocked in tests.
- Prefer provider-hosted checkout and customer portal pages to reduce payment-data handling risk.
- Store only required provider identifiers and sanitized event summaries.
- Webhook event idempotency is mandatory before state-changing processing.

## Change Log

- 2026-08-06: Expanded planned payment integration spec to focus on provider-hosted checkout, webhook-confirmed state, idempotency, and payment event logging.
- 2026-08-06: Implemented provider-neutral payment event persistence, idempotent event processing, trusted subscription-status update path, storage, and tests.
- 2026-08-06: Implemented Stripe Checkout Session creation, Customer Portal redirects, signed webhook parsing, Stripe event-to-subscription status mapping, billing routes, setup documentation, and tests.
- 2026-08-06: Implemented Payment Observability with separate provider-event and payment API operation logging plus System Administrator log pages.
