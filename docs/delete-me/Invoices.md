# 16. Invoices
The Invoices Menu will be at the Level of the Settings or Visit Scheduling Menu in the Nav bar and located right after Visit Scheduling.

Invoice Status (New, Invoiced, Paid)
Status Flow
New -> Invoiced -> Paid
  - Keep the visit Status up-to-date based on these rules.
  - When a new invoice is created -> New
  - When the invoice has been assigned to the client -> Invoiced
  - When the invoice has been paid -> Paid

Invoice Entity
- Invoice GUID - unique value across all Service Clients
- Invoice ID - RowKey - This will be an incrementing value by Service Client
- Invoice Date
- Paid Date
- Service Client ID
- Business Client ID
- Service Visit ID
- If this invoice was part of a service package service, the Service Package ID
- List of additional services, not covered by the Service Package, and the amount
- List of Materials, not covered by the Service Package, the unit and amounts
- The total Cost
- Status
- InvoiceHtml

Columns: Invoice GUID, Invoice ID, Service Client, Business Client, Invoice Date, Paid Date, Cost

Features: 
- The invoices shall be shown in three collapsible Panels.
  - New, Invoiced, and Paid Invoices.
- Provide the ability to add an ad-hoc invoice by adding a Create button on the Top Panel
  - A new invoice can only be created from closed visits that do not have an associated Invoice ID

- Actions: Edit, Delete, View, Info - shown as icons 
  - The info button will show all information of the invoice  
  - The View icon will show the Html
  - Delete Invoice - when an invoice is deleted, also clear the Invoice ID from the Visits table.

## 16.1 Sys Admin
Access to all invoices
Show only invoices for the service (Pool or Landscape)
Allow all actions for all entities. 

## 16.2 Business Owner
Show only invoices for the service client
Allow edits and deletes for accessible  entities.
Columns: Invoice ID, Business Client, Invoice Date, Paid Date, Cost

## 16.3 Business Employee
No Access

## 16.4 Business Client
Actions: View - shown as icons 
Columns: Invoice ID, Invoice Date, Paid Date, Cost

## 16.5 Independent Home Owner
No Access

Current implementation:

- `Invoice` records are modeled with a unique `InvoiceGuid`, service-client-scoped incrementing `InvoiceId`, invoice date, paid date, service client id, business client id, service visit id, optional service package id, additional service lines, material lines, total cost, `New/Invoiced/Paid` status, generated invoice HTML, and created timestamp.
- Invoices are persisted through `IServiceBusinessStore` in memory and Azure Tables using the `Invoices` table, partitioned by service client and keyed by `InvoiceId`. Deleting an invoice also clears the matching visit's `InvoiceId`.
- The Invoices menu is under Visit Scheduling. Business Owners can manage their invoices at `/invoices`; System Administrators can manage service-type-filtered invoices at `/admin/invoices`; Business Clients can view their own invoices from `/invoices`; Business Employees and Independent Home Owners have no invoice access.
- The invoice page shows three collapsible panels for `New`, `Invoiced`, and `Paid` invoices. The columns follow the role-specific requirements: System Administrators see Invoice GUID, Invoice ID, Service Client, Business Client, Invoice Date, Paid Date, and Cost; Business Owners omit Service Client and GUID; Business Clients see Invoice ID, Invoice Date, Paid Date, and Cost.
- Authorized System Administrators and Business Owners can create invoices from closed visits that do not already have an invoice, move invoice status forward from `New` to `Invoiced` to `Paid`, delete invoices, view invoice HTML, and inspect the full invoice detail set. Business Clients have a view-only HTML action.


