# Invoicing

Status: Implemented
Owner: Product
Last reviewed: 2026-08-04

## Problem

Service businesses need a way to turn completed work into customer-facing invoices, track invoice progress, and let business clients view invoices without giving them access to service-client administrative tools.

## Personas

- System Administrator
- Business Owner
- Business Client

Business Employees and Independent Home Owners do not currently have invoice access.

## Requirements

- The Invoices menu appears at the same navigation level as Visit Scheduling.
- Invoice status values are `New`, `Invoiced`, and `Paid`.
- Invoice status can move forward from `New` to `Invoiced` to `Paid`.
- A new invoice can be created only from a closed visit that does not already have a valid invoice.
- A visit invoice id is valid only when an invoice record exists for the same service client and invoice id.
- Closed visits with stale invoice ids that do not resolve to invoice records can be invoiced again through the normal invoice creation path.
- Deleting an invoice clears the invoice id on the related visit.
- Invoice ids increment per service client and are stored as six-digit strings.
- Invoice records include:
  - Unique invoice GUID
  - Service-client-scoped invoice id
  - Invoice date
  - Paid date
  - Service client id
  - Business client id
  - Service visit id
  - Optional service package id
  - Additional billable services
  - Billable materials
  - Total cost
  - Status
  - Invoice HTML
  - Created timestamp
- Ad-hoc visits bill planned services and out-of-scope services.
- Service package visits treat planned services as included and bill only out-of-scope services and materials.
- Invoice creation queues an invoice email log entry.
- The Create Invoice panel remains available to System Administrators and Business Owners even when no invoice records exist yet.

## User Flows

### Create Invoice

1. A System Administrator opens `/admin/invoices` or a Business Owner opens `/invoices`.
2. The user selects a closed visit without a valid invoice.
3. The user clicks Create.
4. The system creates the next service-client-scoped invoice id, stores the invoice, updates the visit with the invoice id, and queues an invoice email.
5. The invoice appears in the New Invoices panel.

### Advance Invoice Status

1. A System Administrator or Business Owner opens the New or Invoiced panel.
2. The user marks a New invoice as Invoiced or an Invoiced invoice as Paid.
3. The system saves the updated status and sets `PaidDate` when the invoice is marked Paid.

### View Invoice

1. A System Administrator, Business Owner, or Business Client opens the invoice page.
2. The user chooses the view HTML action.
3. The system displays the stored invoice as rendered HTML.

### Delete Invoice

1. A System Administrator or Business Owner chooses the delete action.
2. The system deletes the invoice record.
3. If the related visit still references that invoice id, the system clears the visit invoice id.

## UI Expectations

- Invoices are grouped into collapsible panels for New, Invoiced, and Paid invoices.
- New and Invoiced panels are expanded by default; Paid is collapsed by default.
- System Administrators see Invoice GUID, Invoice ID, Service Client, Business Client, Invoice Date, Paid Date, and Cost.
- Business Owners see Invoice ID, Business Client, Invoice Date, Paid Date, and Cost.
- Business Clients see Invoice ID, Invoice Date, Paid Date, and Cost.
- Management actions are icon-only controls:
  - Mark invoiced
  - Mark paid
  - View invoice HTML
  - Show invoice info
  - Delete invoice
- Business Clients only receive the view invoice HTML action.

## Data Model Impact

- `InvoiceStatus` has `New`, `Invoiced`, and `Paid`.
- `Invoice` stores the invoice snapshot, service and material lines, total, status, generated HTML, and timestamps.
- `InvoiceServiceLine` stores billable service id, name, and amount.
- `InvoiceMaterialLine` stores billable material id, name, unit, quantity, unit amount, and line amount.
- `ServiceVisit.InvoiceId` links a closed visit to the generated invoice.
- `ServiceVisit.InvoiceId` must reference an existing `Invoice.InvoiceId` in the same service-client partition to be considered a valid invoice link.
- `IServiceBusinessStore` exposes invoice read, upsert, and delete operations.
- Azure Table storage stores invoices in the `Invoices` table by service-client partition and invoice id row key.

## Authorization Rules

- System Administrators can view and manage invoices through `/admin/invoices`.
- The System Administrator view filters service clients to the current app mode, Pool or Landscape.
- Business Owners can view and manage invoices for their active service client through `/invoices`.
- Business Clients can view only invoices for their linked business client through `/invoices`.
- Business Employees have no invoice access.
- Independent Home Owners have no invoice access.

## Acceptance Criteria
- Implemented: Closed visits without invoice ids can produce invoices.
- Implemented: Closed visits with stale invoice ids and no matching invoice record can produce invoices.
- Implemented: Existing invoiced visits are not reinvoiced.
- Implemented: Invoice ids increment per service client.
- Implemented: Invoice creation updates the visit invoice id.
- Implemented: Invoice creation stores the invoice entity before updating the visit invoice id.
- Implemented: A visit `InvoiceId` is treated as valid only when it references an existing invoice record for the same service client.
- Implemented: Invoice creation queues an invoice email log.
- Implemented: Invoice status moves only from `New` to `Invoiced` to `Paid`, with idempotent same-status saves.
- Implemented: Marking an invoice paid sets the paid date.
- Implemented: Deleting an invoice clears the visit invoice id.
- Implemented: The Create Invoice panel is available even when there are no existing invoices.
- Implemented: Business clients can read only their own invoices.
- Implemented: System Administrators, Business Owners, and Business Clients get role-appropriate invoice columns and actions.
- Implemented: When an invoice is displayed, stored invoice HTML is rendered as HTML instead of shown as raw markup.
- Not implemented: Stripe-hosted invoice/payment link integration.
- Not implemented: PDF invoice generation.

## Tests

- `InvoiceJobTests.Invoicing_service_creates_invoice_for_closed_visit_without_invoice`
- `InvoiceJobTests.Invoicing_service_does_not_reinvoice_visits`
- `InvoiceJobTests.Invoicing_service_recreates_invoice_when_visit_has_stale_invoice_id`
- `InvoiceJobTests.Invoicing_service_blocks_creation_when_visit_invoice_id_has_matching_invoice`
- `InvoiceJobTests.Scheduled_job_runner_creates_invoices_and_processes_new_email_logs`
- `InvoiceJobTests.Invoice_status_moves_forward_through_workflow`
- `InvoiceJobTests.Deleting_invoice_clears_visit_invoice_id`
- `InvoiceJobTests.Business_client_can_read_only_their_invoices`
- `InvoiceHtmlDisplayTests.Render_preserves_invoice_html_markup`
- `InvoiceHtmlDisplayTests.Render_shows_empty_state_when_invoice_html_is_missing`

## User Documentation Impact

- Created `docs/user-guide/invoices.md` for role-specific invoice use.
- Updated `docs/user-guide/invoices.md` to explain stale invoice-id handling and first-invoice creation.
- Updated `docs/user-guide/invoices.md` to clarify that the invoice HTML action opens a rendered invoice preview.
- User documentation should be updated when Stripe payment links or PDF downloads are added.

## Current Implementation

- `Invoice` records are modeled with a unique `InvoiceGuid`, service-client-scoped incrementing `InvoiceId`, invoice date, paid date, service client id, business client id, service visit id, optional service package id, additional service lines, material lines, total cost, `New/Invoiced/Paid` status, generated invoice HTML, and created timestamp.
- Invoices are persisted through `IServiceBusinessStore` in memory and Azure Tables using the `Invoices` table, partitioned by service client and keyed by `InvoiceId`.
- `InvoicingJobService.CreateInvoicesForCompletedVisitsAsync` scans active service clients for closed visits and creates invoices when there is no valid invoice link.
- `InvoicingJobService.CreateInvoiceForVisitAsync` supports manual invoice creation for one closed visit.
- `InvoicingJobService` considers a visit invoice id valid only when `IServiceBusinessStore.GetInvoiceAsync` returns a matching invoice record for the same service client and invoice id.
- Closed visits with stale invoice ids are repaired by normal invoice creation: the invoice entity is stored, the visit invoice id is replaced with the generated invoice id, and invoice email is queued.
- Invoice line items are calculated from the service client's catalog plus the app-mode global catalog. Ad-hoc visits bill planned services and out-of-scope services; service package visits bill out-of-scope services only. Out-of-scope materials are billable for both visit types.
- Invoice HTML is generated and stored on the invoice record.
- The Blazor invoice page renders stored invoice HTML through `InvoiceHtmlDisplay.Render` in the invoice HTML and invoice info dialogs.
- Invoice creation writes an `EmailLogEntry` with `EmailType` `Invoice`, `New` delivery status, service-client sender email, business-client recipient email, and the generated HTML body.
- `CompanyAdminService` exposes invoice listing, status updates, and delete operations for System Administrators and Business Owners.
- `ClientPortalService.GetCurrentUserInvoicesAsync` scopes Business Client invoice reads to the signed-in user's linked business client.
- The Blazor invoice page is implemented at `/invoices` and `/admin/invoices`.

## Outstanding Tasks

- Integrate Stripe invoice/payment links when payment processing is ready.
- Add PDF invoice rendering/download if customers need printable invoices.
- Add audit events for invoice creation, status changes, deletion, and payment events.
- Add UI confirmation for destructive invoice deletion.
- Consider showing invoice email delivery status directly on the invoice page.
- Decide whether `InvoiceDate` should be set at creation time or only when status changes to `Invoiced`.
- Add component or browser-level coverage for the rendered invoice HTML dialog.

## Change Log

- 2026-08-04: Documented the implemented invoicing feature, including current code behavior, role access, acceptance criteria status, tests, and outstanding tasks.
- 2026-08-04: Implemented invoice-link integrity so visit invoice ids must resolve to invoice records, stale visit invoice ids can be repaired through invoice creation, and the first invoice can be created from the Invoices page.
- 2026-08-04: Implemented rendered invoice HTML display in invoice dialogs and added unit coverage for the rendering helper.
