# Stripe Payment Setup

Last reviewed: 2026-08-06

Use Stripe for Independent Home Owner subscription checkout, customer billing management, and webhook-confirmed subscription status.

## Stripe Account Setup

1. Create or open a Stripe account.
2. Create the Independent Home Owner monthly product price.
3. Create the Independent Home Owner annual product price.
4. Copy each Stripe price id. Price ids usually start with `price_`.
5. Configure the Stripe Customer Portal for subscription and payment-method management.

## Application Settings

Set these values in local user secrets, local appsettings, or Azure App Service configuration:

- `Payment__Stripe__SecretKey`
- `Payment__Stripe__WebhookSecret`
- `Payment__Stripe__Mode`
- `Payment__Stripe__HomeOwnerMonthlyPriceId`
- `Payment__Stripe__HomeOwnerAnnualPriceId`

Local configuration uses colon-separated names:

- `Payment:Stripe:SecretKey`
- `Payment:Stripe:WebhookSecret`
- `Payment:Stripe:Mode`
- `Payment:Stripe:HomeOwnerMonthlyPriceId`
- `Payment:Stripe:HomeOwnerAnnualPriceId`

`Payment:Stripe:Mode` should be `test` or `live`.

## Webhook Endpoint

Configure this endpoint in Stripe:

```text
https://{host}/billing/stripe/webhook
```

Subscribe to these events for the current implementation:

- `checkout.session.completed`
- `customer.subscription.created`
- `customer.subscription.updated`
- `customer.subscription.deleted`
- `invoice.paid`
- `invoice.payment_failed`

Copy the endpoint signing secret into `Payment__Stripe__WebhookSecret`.

## Runtime Flow

1. Independent Home Owner registration creates a homeowner subscription.
2. The Profile page shows Start Checkout when a Stripe price id is configured for the selected plan.
3. The app creates a Stripe Checkout Session and redirects the user to Stripe.
4. The browser return shows pending confirmation; it does not activate access by itself.
5. Stripe sends a signed webhook.
6. The app validates the signature, records the event idempotently, and updates the homeowner subscription.
7. After a Stripe customer id is linked, the Profile page shows Manage Billing and redirects to the Stripe Customer Portal.

## Safety Rules

- Do not store card data in the application.
- Do not activate subscription access from the browser return URL.
- Keep Stripe test and live keys separate.
- Keep webhook secrets out of source control.
- Rotate Stripe keys if a secret is exposed.

## References

- Stripe Checkout Sessions API: https://docs.stripe.com/api/checkout/sessions
- Stripe Customer Portal Sessions API: https://docs.stripe.com/api/customer_portal/sessions
- Stripe webhook signature verification: https://docs.stripe.com/webhooks/signature
