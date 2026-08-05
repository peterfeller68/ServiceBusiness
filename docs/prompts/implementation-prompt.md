# Codex Implementation Prompt

Use this prompt to start generating the application from the planning documents in this repository.

## Prompt

Build a multi-tenant SaaS application for pool cleaning and landscaping businesses using Blazor, ASP.NET Core, Azure Storage, Google Authentication, and Stripe.

Use these documents as the source of truth:

- `docs/requirements.md`
- `docs/architecture.md`
- `docs/storage-entities.md`
- `docs/ui-specifications.md`

Implementation priorities:

1. Create the solution and project structure described in `docs/architecture.md`.
2. Implement Google Authentication and the application user profile model.
3. Implement tenant-aware authorization and role-aware navigation.
4. Implement Azure Table Storage repositories for the core entities in `docs/storage-entities.md`.
5. Implement System Admin company type and company management screens.
6. Implement Company Admin setup screens for company settings, users, clients, client types, services, and materials.
7. Implement scheduling, recurring schedules, visit assignment, and visit status changes.
8. Implement Standard Company User daily visits, route list, and visit completion.
9. Implement service completion email queueing.
10. Implement Company Client User service history, billing history, and messaging.
11. Implement Stripe references, webhook idempotency, invoice/payment display, and payment links.
12. Implement reports with date range, user, client, service, material, and status filters.

Technical constraints:

- Use Blazor for all UI.
- Host on Azure App Service.
- Persist application data in Azure Storage.
- Use Azure Table Storage for structured records.
- Use Azure Blob Storage for logos, attachments, and generated exports.
- Use Azure Queue Storage for background jobs.
- Use Google Authentication only; do not implement local password authentication.
- Use Stripe for payments; do not store card data.
- Enforce tenant isolation in backend services, not only in UI.
- Include tests for tenant authorization, recurrence generation, and Stripe webhook idempotency.

Initial vertical slice:

Build the smallest working end-to-end slice first:

- Google sign-in.
- User profile creation.
- System Admin company type management.
- System Admin company creation.
- Company Admin dashboard.
- Company Admin client, service, and material management.
- Create a scheduled visit.
- Assign the visit to a Company User.
- Company User views the assigned visit.
- Company User completes the visit with service, material, and notes.
- Completed visit appears in client service history.

After the vertical slice works, continue screen-by-screen following `docs/ui-specifications.md`.
