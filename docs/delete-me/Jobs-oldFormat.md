# 1. Jobs
This section describes the background jobs that will be running as part of the service

## 1.1 Invoicing Service
This section will desribe the functionality of the invoicing service.

### 1.1.1 Invoice Creation
Any service that was closed and does not have an Invoice ID will be picked up and an invoice will be created.
An invoice should be nicely formatted in HTML 
Once the invoice has been completed save it to the Email Log table, with a status of New.

Current implementation:

- `InvoicingJobService.CreateInvoicesForCompletedVisitsAsync` scans active service clients for closed visits with no `InvoiceId`. For each matching visit, it creates an invoice, stores it, updates the visit with the generated incrementing invoice id, and writes an `EmailLogEntry` with `EmailType` `Invoice`, HTML body, service-client sender, recipient, and `New` status.
- Ad-hoc visit planned services are billed on the invoice. Service package visit planned services are treated as included, while out-of-scope services and out-of-scope materials are billed separately. Invoice HTML includes the service client, business client, visit, line items, and total.

## 1.2 Emailing Service
This section will describe the functionality of the emailing service.

A table will contain emails to be sent with the following fields
Service Client, From, To, cc, Subject, Body, Status, SendDate, FailedMessage

The status will be either (New, Sent, Failed)

### 1.2.1 Send Email
The service will pick up emails from the EmailLog table in a New status and send them out. Once successfully sent out, 
update the status to Sent. If the send failed, set the status to Failed and record the FailedMessage.

Never send emails to a test user. The email addresses are not valid, and so the email will bounce.
For test users, simply update the Status to Sent.
For non-test users, send the email and update the status based on the result.

Current implementation:

- `EmailLogEntry` includes from, recipient/to, cc, subject, body, status, sent date, and failure reason fields; existing notification logging continues to use the same table.
- `EmailJobService.ProcessNewEmailLogsAsync` processes `EmailLogEntry` rows with `New` status. In DevTest mode, or when the recipient resolves to a test user, it marks the row `Sent` without external delivery so test addresses are never sent to the email provider. Outside DevTest, non-test recipients are sent through the configured `IEmailSender`; successful provider responses update the row to `Sent` with the provider message id and sent timestamp, while invalid recipients or provider failures update the row to `Failed` with the failure message.
- The web app registers `AzureCommunicationEmailSender` for `IEmailSender`, using the Azure Communication Services email connection string and sender address from configuration. `EmailJobService` remains the callable Web Job boundary for the emailing workflow.


