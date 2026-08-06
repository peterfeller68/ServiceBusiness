# Subscriptions

Last reviewed: 2026-08-06

Subscriptions control Independent Home Owner access after registration.

## Home Owner Registration

Independent Home Owners choose a subscription plan while completing registration.

1. Choose Home owner.
2. Enter account and home details.
3. Choose Monthly or Annual subscription.
4. Complete registration.

The account starts with the trial length configured by a System Administrator in Settings / System Settings. Payment card details are not collected directly in the application.

## Profile

Independent Home Owners can open Profile to review:

- Subscription plan
- Subscription status
- Trial end date
- Current access state

## System Admin Support

System Administrators can open Users, edit an Independent Home Owner, and adjust the trial end date when a support exception is needed.

System Administrators can open Settings / System Settings to change the default trial length used for future Independent Home Owner registrations.

## System Admin Plan Management

System Administrators can open System Administrator / Subscriptions to manage subscription plans.

Editable plan fields include:

- Plan ID for new plans
- Name
- Description
- Billing interval
- Price
- Display order
- Active status
- Stripe price ID

Deactivate a plan to remove it from new registration choices without deleting existing subscriptions that already reference it.

## Payment Processing

When Stripe price ids are configured, the Profile page shows Start Checkout for subscriptions that do not yet have a Stripe customer.

After checkout returns, subscription access still waits for Stripe webhook confirmation. This protects the account from browser-only redirects.

After a Stripe customer is linked, the Profile page shows Manage Billing and opens the Stripe Customer Portal for payment-method and subscription management.
