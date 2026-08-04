# Invoices

Last reviewed: 2026-08-04

Invoices turn closed service visits into customer-facing billing records.

## Who Can Use This Page

- System Administrators can manage invoices for service clients matching the current Pool or Landscape app mode.
- Business Owners can manage invoices for their own service client.
- Business Clients can view their own invoices.
- Business Employees and Independent Home Owners do not have invoice access.

## Opening Invoices

- System Administrators use the Invoices link in the admin navigation.
- Business Owners use the Invoices link in the business navigation.
- Business Clients use the Invoices link in the client navigation.

## Invoice Panels

Invoices are grouped by status:

- New Invoices
- Invoiced Invoices
- Paid Invoices

New and Invoiced panels are open by default. Paid invoices are collapsed by default.

## Creating an Invoice

System Administrators and Business Owners can create an invoice from a closed visit that does not already have a valid invoice.

1. Open Invoices.
2. In Create Invoice, choose a closed visit.
3. Select Create.
4. The invoice appears in New Invoices.

The system also updates the related visit with the new invoice id and queues an invoice email.

If a closed visit shows an old invoice id but the invoice record no longer exists, the visit is treated as not invoiced. Creating an invoice replaces the stale visit invoice id with the new invoice id.

## Invoice Actions

Available actions depend on your role and the invoice status:

- Mark invoiced: moves a New invoice to Invoiced.
- Mark paid: moves an Invoiced invoice to Paid and records the paid date.
- View invoice HTML: opens the generated customer-facing invoice as a rendered preview.
- Show invoice info: shows the full invoice record, including a rendered invoice preview.
- Delete invoice: removes the invoice and clears the invoice id from the related visit.

Business Clients can only view invoice HTML.

## What Is Billed

Ad-hoc visits bill planned services, out-of-scope services, and out-of-scope materials.

Service package visits treat planned services as included in the package. They bill only out-of-scope services and out-of-scope materials.

## Current Limitations

- Stripe payment links are not yet connected.
- PDF invoice downloads are not yet available.
- Invoice generation is available through the application service, page action, and hosted scheduler.
