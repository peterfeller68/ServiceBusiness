# Subscriptions

Status: In Progress
Owner: Product
Last reviewed: 2026-08-06

## Problem

Independent Home Owners need a clear subscription model so they can choose a plan during onboarding, receive a configurable trial, and have access controlled by the subscription lifecycle. The product also needs room to extend subscriptions to Business Owners later without mixing plan rules directly into payment-provider code.

## Personas

- System Admin
- Independent Home Owner
- Business Owner

## Requirements

- Independent Home Owners can choose a monthly or annual subscription plan during onboarding.
- Independent Home Owners receive a configurable trial period before paid billing begins.
- Trial length is configured through persisted System Settings and defaults from `SystemSettings:HomeOwnerTrialDays` only when no saved settings row exists.
- Trial end date is editable by a System Admin in user management.
- Subscription plan, billing interval, status, and trial end date are visible in the Independent Home Owner profile.
- Subscription status determines whether Independent Home Owner paid features are active, pending, past due, canceled, or expired.
- Subscription state is stored as a dedicated domain record instead of only adding billing fields to `AppUser`.
- The subscription domain remains provider-neutral; payment-provider customer, checkout, and subscription identifiers are stored as references only.
- Business Owner subscriptions are a future extension and should reuse the same subscription model when that scope is implemented.
- Subscription plans are maintained by the System Administrator.

## User Flows

### Independent Home Owner Subscription Onboarding

1. The user registers as an Independent Home Owner.
2. The app creates or resumes the homeowner registration context.
3. The user chooses a monthly or annual subscription plan.
4. The app creates a pending subscription record.
5. If payment integration is enabled, the user continues to provider-hosted checkout.
6. The subscription becomes `Trialing` or `Active` only after trusted provider confirmation or an approved non-payment trial path.
7. The user lands in the Independent Home Owner workspace with access based on subscription status.

### Resume Pending Checkout

1. A registered Independent Home Owner signs in with an incomplete subscription.
2. The app shows a pending subscription state.
3. The user can resume checkout or choose a different available plan.
4. The app updates the subscription after payment-provider confirmation.

### System Admin Trial Adjustment

1. A System Admin opens user management for an Independent Home Owner.
2. The System Admin reviews subscription plan, status, and trial end date.
3. The System Admin updates the trial end date when a support or sales exception is needed.
4. The app records the updated trial end date and reflects it in profile/subscription status.

### System Admin Subscription Plan Management

1. A System Admin opens System Administrator / Subscriptions.
2. The System Admin reviews active and inactive subscription plans.
3. The System Admin adds or edits plan name, description, billing interval, price, display order, active status, and Stripe price id.
4. The app saves the provider-neutral subscription plan and immediately uses active plans for new Independent Home Owner registration choices.

## UI Expectations

- Independent Home Owner registration includes a plan-selection step when subscription onboarding is enabled.
- Monthly and annual options show plan name, billing interval, trial behavior, and price once pricing is configured.
- Pending checkout state is clear and resumable after sign-in.
- The Independent Home Owner profile shows subscription plan, billing interval, status, trial end date, and current period end date when available.
- System Admin user management shows subscription status and allows trial end date changes.
- Subscription UI does not collect or display card details; payment method changes are handled through the payment integration.
- System Admin can edit Subscriptions via the System Administrator nav bar menu.

## Data Model Impact

- Add `SubscriptionPlan` for app-defined plans and billing intervals.
- Add `HomeOwnerSubscription` or a provider-neutral `Subscription` entity for Independent Home Owner subscription state.
- Store canonical subscription statuses such as `PendingCheckout`, `Trialing`, `Active`, `PastDue`, `Canceled`, `Expired`, and `PaymentFailed`.
- Store `PlanId`, `OwnerUserId`, `Status`, `TrialEndsAt`, `CurrentPeriodStartsAt`, `CurrentPeriodEndsAt`, `CancelAtPeriodEnd`, `CreatedUtc`, and `UpdatedUtc`.
- Store payment-provider references on the subscription record only as external IDs, such as provider customer id, provider subscription id, checkout session id, and price id.
- Store Stripe price id on subscription plans so System Admins can align app display pricing with Stripe Checkout pricing.
- Keep subscription state separate from `AppUser`; `AppUser` remains the identity and authentication record.

## Authorization Rules

- Independent Home Owners can view their own subscription status.
- Independent Home Owners can choose or resume their own subscription flow.
- Independent Home Owners cannot edit trusted subscription status, trial dates, or provider identifiers directly.
- System Admins can view subscription state and edit trial end dates.
- System Admins can create, edit, activate, and deactivate subscription plans.
- Business Owners cannot view or manage Independent Home Owner subscriptions.
- Paid Independent Home Owner features require an active entitlement state defined by subscription status.

## Acceptance Criteria

- Implemented: Independent Home Owners can select monthly or annual subscription during onboarding.
- Implemented: The app creates a homeowner subscription record during registration.
- Implemented: Trial length is read from persisted `SystemSettings.HomeOwnerTrialDays`, with configuration used as the first-run default.
- Implemented: System Admins can edit trial end date for an Independent Home Owner.
- Implemented: Independent Home Owner profile displays subscription plan, billing interval, status, and trial end date.
- Implemented: Subscription status controls paid Independent Home Owner entitlement decisions in application service logic.
- Implemented: Subscription records are provider-neutral and store provider IDs only as references.
- Implemented: System Admins can create and edit subscription plans.
- Implemented: System Admins can activate and deactivate subscription plans without deleting existing plan records.
- Implemented: Browser return from checkout does not activate subscription access without trusted confirmation from the payment integration.

## Tests

- `OnboardingTests.Independent_homeowner_registration_creates_active_owner_workspace_without_company_membership`
- `OnboardingTests.Independent_homeowner_registration_uses_selected_subscription_plan_and_trial_days`
- `OnboardingTests.System_admin_can_update_homeowner_subscription_trial_end`
- `OnboardingTests.Subscription_entitlement_requires_active_or_current_trial_status`
- `OnboardingTests.Homeowner_checkout_session_updates_subscription_provider_references`
- `OnboardingTests.Webhook_processing_updates_subscription_once_for_duplicate_provider_event`
- `OnboardingTests.System_admin_can_update_subscription_plan_price_and_provider_price_id`
- `OnboardingTests.System_admin_can_deactivate_subscription_plan_without_deleting_it`
- `OnboardingTests.Subscription_plan_management_requires_system_admin`
- `OnboardingTests.Subscription_plan_price_cannot_be_negative`
- `OnboardingTests.System_admin_can_update_persisted_homeowner_trial_days`
- `OnboardingTests.Updated_system_settings_drive_new_homeowner_subscription_trial`
- Missing: browser-level registration plan-selection coverage.
- Missing: route-level entitlement gating tests after paid feature gates are defined.

## User Documentation Impact

- Created `docs/user-guide/subscriptions.md`.
- Added `docs/operations/stripe-payment-setup.md`.
- Updated `docs/user-guide/registration-and-authentication.md`.
- Updated `docs/user-guide/getting-started.md`.
- Updated `docs/user-guide/index.md`.

## Current Implementation

- `SubscriptionPlan`, `HomeOwnerSubscription`, `SubscriptionStatus`, and `SubscriptionBillingInterval` are defined in the domain model.
- `SystemSettings.HomeOwnerTrialDays` defaults to 14, can be seeded from `SystemSettings:HomeOwnerTrialDays`, and is editable by System Administrators from Settings / System Settings.
- `SubscriptionReferenceData` seeds monthly and annual homeowner plans.
- `SubscriptionService` creates or updates homeowner subscriptions, calculates trial end dates, and evaluates active entitlement state.
- `OnboardingService.RegisterIndependentHomeOwnerAsync` creates the homeowner profile/equipment records and a homeowner subscription from the selected plan.
- `RegisterPage.razor` shows monthly and annual subscription choices for Independent Home Owner registration.
- `ProfilePage.razor` shows homeowner subscription plan, status, trial end, and access state.
- `AdminUsersPage.razor` shows subscription and trial end in user management and lets System Admins edit trial end date.
- `AdminSubscriptionPlansPage.razor` exposes System Admin subscription plan management at `/admin/subscriptions`.
- `PlatformAdminService.GetSubscriptionPlansAsync`, `UpsertSubscriptionPlanAsync`, and `SetSubscriptionPlanActiveAsync` provide authorized plan management.
- `NavMenu.razor` links System Administrator / Subscriptions for System Admins.
- `/billing/homeowner/checkout` starts Stripe Checkout for configured homeowner plans.
- `/billing/homeowner/portal` opens Stripe Customer Portal when the homeowner subscription has a provider customer id.
- Stripe webhooks update homeowner subscription status through trusted, idempotent event processing.
- In-memory and Azure Table stores persist subscription plans and homeowner subscriptions.

## Outstanding Tasks

- Decide exact paid-feature gates for `PastDue`, `Canceled`, `Expired`, and `PaymentFailed`.
- Add browser-level plan-selection coverage.
- Extend the model to Business Owner subscriptions when that product scope is ready.
- Add end-to-end validation with Stripe test mode after Stripe products, prices, portal, and webhook endpoint are configured.

## Feature Dependencies

- Registration and Authentication creates the Independent Home Owner account and hands off to subscription onboarding when enabled.
- Payment Integration confirms provider checkout, payment, trial, failed, canceled, and resumed states.
- System Settings provides configurable trial duration.
- Roles and Permissions defines System Admin access to subscription support controls.

## Implementation Notes

- Treat subscriptions as the app-level entitlement model and payment integration as the external provider boundary.
- Keep pricing and billing interval configuration on plans so future Business Owner subscriptions can reuse the same shape.
- Prefer webhook-confirmed subscription activation when payment integration is enabled.

## Change Log

- 2026-08-06: Expanded planned subscription spec to separate product subscription rules from payment-provider integration and registration behavior.
- 2026-08-06: Implemented provider-neutral homeowner subscription plans, registration plan selection, configurable trial days, profile/admin visibility, trial-end support edits, storage, seed data, tests, and user documentation.
- 2026-08-06: Connected homeowner subscriptions to Stripe Checkout, Stripe Customer Portal, and signed webhook-confirmed subscription status updates.
- 2026-08-06: Implemented System Admin subscription plan management for plan name, description, price, billing interval, active status, sort order, and Stripe price id.
- 2026-08-06: Connected homeowner trial duration to persisted editable System Settings instead of requiring app configuration changes after startup.
