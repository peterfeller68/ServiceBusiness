# Storage Entity List

This document describes the recommended Azure Storage-backed entity model. Entity names are logical; implementation may use Azure Table entities with `PartitionKey`, `RowKey`, `Timestamp`, and `ETag`.

## 1. Cross-Cutting Entity Rules

Current implementation note:

- `AzureStorageTableInitializer` provisions the currently modeled Azure Table names when `AzureStorage:UseAzureStorage` is enabled.
- `AzureTableServiceBusinessStore` is used when `AzureStorage:UseAzureStorage` is enabled.
- The Table-backed store seeds current MVP data when the `Users` table is empty.
- The Table-backed store persists current MVP records as JSON payloads in Azure Table entities and keeps `UserByEmail` and `UserByGoogleSubject` lookup rows in sync.
- `InMemoryServiceBusinessStore` remains available for local development when Azure Storage is disabled.

Common fields:

- `CreatedUtc`
- `CreatedByUserId`
- `UpdatedUtc`
- `UpdatedByUserId`
- `Status`
- `ETag`

Company-scoped entities must include:

- `CompanyId`

Recommended key style:

- Use stable GUID or ULID identifiers for public entity IDs.
- Use date-prefixed row keys where date range queries matter.
- Use denormalized lookup tables for alternate access patterns.

## 2. Platform Tables

### 2.1 CompanyTypes

Purpose:

- Stores platform-managed company type options.

Suggested keys:

- `PartitionKey`: `COMPANY_TYPE`
- `RowKey`: `{CompanyTypeId}`

Fields:

- `CompanyTypeId`
- `Name`
- `Description`
- `IsActive`
- `SortOrder`
- `CreatedUtc`
- `UpdatedUtc`

### 2.2 Companies

Purpose:

- Stores SaaS tenant companies.

Suggested keys:

- `PartitionKey`: `COMPANY`
- `RowKey`: `{CompanyId}`

Fields:

- `CompanyId`
- `CompanyTypeId`
- `Name`
- `LegalName`
- `BusinessEmail`
- `BusinessPhone`
- `Website`
- `AddressLine1`
- `AddressLine2`
- `City`
- `State`
- `PostalCode`
- `Country`
- `TimeZone`
- `LogoBlobPath`
- `Status`
- `StripeAccountId`
- `StripeConnectionStatus`
- `CreatedUtc`
- `UpdatedUtc`

### 2.3 Users

Purpose:

- Stores application user profiles linked to Google Authentication.

Suggested keys:

- `PartitionKey`: `USER`
- `RowKey`: `{UserId}`

Fields:

- `UserId`
- `GoogleSubjectId`
- `Email`
- `NormalizedEmail`
- `NotificationEmail`
- `DisplayName`
- `Phone`
- `ProfileImageUrl`
- `GlobalStatus`
- `IsSystemAdmin`
- `IsTestUser`
- `Status`
- `CreatedUtc`
- `LastLoginUtc`

Statuses:

- `Active`
- `Disabled`

### 2.4 UserByGoogleSubject

Purpose:

- Lookup table for sign-in.

Suggested keys:

- `PartitionKey`: `GOOGLE_SUBJECT`
- `RowKey`: `{GoogleSubjectId}`

Fields:

- `UserId`
- `Email`
- `CreatedUtc`

### 2.5 UserByEmail

Purpose:

- Lookup table for invitation and duplicate detection.

Suggested keys:

- `PartitionKey`: `EMAIL`
- `RowKey`: `{NormalizedEmail}`

Fields:

- `UserId`
- `Email`
- `CreatedUtc`

## 3. Membership Tables

### 3.0 RoleDefinitions

Purpose:

- Stores role metadata used by registration, authorization, and approval workflows.

Suggested keys:

- `PartitionKey`: `ROLE`
- `RowKey`: `{Role}`

Fields:

- `Role`
- `DisplayName`
- `Description`
- `RequiresOwnerApproval`
- `Permissions`

### 3.1 CompanyMemberships

Purpose:

- Stores employee/admin membership records.

Suggested keys:

- `PartitionKey`: `COMPANY#{CompanyId}`
- `RowKey`: `USER#{UserId}`

Fields:

- `CompanyId`
- `UserId`
- `Role`
- `Status`
- `InvitedByUserId`
- `ApprovedByUserId`
- `RequestedUtc`
- `DecidedUtc`
- `DecidedByUserId`
- `ApprovedUtc`
- `RejectedUtc`
- `RemovedUtc`

Statuses:

- `Pending`
- `Active`
- `Rejected`
- `Inactive`
- `Removed`

Roles:

- `CompanyAdmin`
- `CompanyUser`

### 3.2 UserCompanyMemberships

Purpose:

- Lookup table for companies available to a user after sign-in.

Suggested keys:

- `PartitionKey`: `USER#{UserId}`
- `RowKey`: `COMPANY#{CompanyId}`

Fields:

- `UserId`
- `CompanyId`
- `CompanyName`
- `Role`
- `Status`

### 3.3 CompanyClientUserMemberships

Purpose:

- Links homeowner users to company client records.

Suggested keys:

- `PartitionKey`: `CLIENT#{CompanyClientId}`
- `RowKey`: `USER#{UserId}`

Fields:

- `CompanyId`
- `CompanyClientId`
- `UserId`
- `Role`
- `Status`
- `RelationshipLabel`
- `InvitedByUserId`
- `ApprovedByUserId`
- `RequestedUtc`
- `ApprovedUtc`
- `RejectedUtc`
- `RemovedUtc`

### 3.4 UserClientMemberships

Purpose:

- Lookup table for client accounts accessible to a homeowner user.

Suggested keys:

- `PartitionKey`: `USER#{UserId}`
- `RowKey`: `CLIENT#{CompanyClientId}`

Fields:

- `UserId`
- `CompanyId`
- `CompanyClientId`
- `CompanyName`
- `ClientDisplayName`
- `Status`

## 4. Company Configuration Tables

### 4.1 CompanySettings

Purpose:

- Stores tenant-level settings.

Suggested keys:

- `PartitionKey`: `COMPANY#{CompanyId}`
- `RowKey`: `SETTINGS`

Fields:

- `CompanyId`
- `ServiceAreaDescription`
- `DefaultEmailReplyTo`
- `ServiceCompletionEmailEnabled`
- `ServiceCompletionEmailSubject`
- `ServiceCompletionEmailTemplate`
- `DefaultCurrency`
- `DefaultTaxRate`
- `ClientSelfServiceEnabled`
- `EmployeeSelfServiceEnabled`
- `UpdatedUtc`

### 4.2 ClientTypes

Purpose:

- Stores company-specific billing/client type definitions.

Suggested keys:

- `PartitionKey`: `COMPANY#{CompanyId}`
- `RowKey`: `CLIENT_TYPE#{ClientTypeId}`

Fields:

- `CompanyId`
- `ClientTypeId`
- `Name`
- `Description`
- `BillingFrequency`
- `DefaultRate`
- `Currency`
- `IsActive`
- `CreatedUtc`
- `UpdatedUtc`

Billing frequencies:

- `FeeForService`
- `Weekly`
- `BiWeekly`
- `Monthly`

### 4.3 Services

Purpose:

- Stores company-offered services.

Suggested keys:

- `PartitionKey`: `COMPANY#{CompanyId}`
- `RowKey`: `SERVICE#{ServiceId}`

Fields:

- `CompanyId`
- `ServiceId`
- `Name`
- `Description`
- `DefaultDurationMinutes`
- `DefaultPrice`
- `IsTaxable`
- `IsActive`
- `SortOrder`
- `CreatedUtc`
- `UpdatedUtc`

### 4.4 Materials

Purpose:

- Stores company-used materials.

Suggested keys:

- `PartitionKey`: `COMPANY#{CompanyId}`
- `RowKey`: `MATERIAL#{MaterialId}`

Fields:

- `CompanyId`
- `MaterialId`
- `Name`
- `Description`
- `UnitOfMeasure`
- `DefaultUnitCost`
- `DefaultBillableUnitPrice`
- `IsTaxable`
- `IsActive`
- `SortOrder`
- `CreatedUtc`
- `UpdatedUtc`

## 5. Client Tables

### 5.1 CompanyClients

Purpose:

- Stores customers/properties serviced by a company.

Suggested keys:

- `PartitionKey`: `COMPANY#{CompanyId}`
- `RowKey`: `CLIENT#{CompanyClientId}`

Fields:

- `CompanyId`
- `CompanyClientId`
- `ClientDisplayName`
- `PrimaryContactName`
- `Email`
- `Phone`
- `BillingAddressLine1`
- `BillingAddressLine2`
- `BillingCity`
- `BillingState`
- `BillingPostalCode`
- `BillingCountry`
- `ServiceAddressLine1`
- `ServiceAddressLine2`
- `ServiceCity`
- `ServiceState`
- `ServicePostalCode`
- `ServiceCountry`
- `Latitude`
- `Longitude`
- `PropertyNotes`
- `AccessNotes`
- `PreferredServiceDays`
- `ClientTypeId`
- `RateOverride`
- `StripeCustomerId`
- `NotificationEmailEnabled`
- `IsTaxable`
- `Status`
- `CreatedUtc`
- `UpdatedUtc`

### 5.2 CompanyClientsByEmail

Purpose:

- Lookup table for client self-service request matching.

Suggested keys:

- `PartitionKey`: `COMPANY#{CompanyId}#EMAIL`
- `RowKey`: `{NormalizedEmail}#{CompanyClientId}`

Fields:

- `CompanyId`
- `CompanyClientId`
- `Email`
- `ClientDisplayName`
- `Status`

## 6. Scheduling Tables

### 6.1 RecurringSchedules

Purpose:

- Stores recurring service templates.

Suggested keys:

- `PartitionKey`: `COMPANY#{CompanyId}`
- `RowKey`: `RECURRING#{RecurringScheduleId}`

Fields:

- `CompanyId`
- `RecurringScheduleId`
- `CompanyClientId`
- `AssignedUserId`
- `StartDate`
- `EndDate`
- `RecurrenceType`
- `Interval`
- `DaysOfWeek`
- `DayOfMonth`
- `OrdinalWeek`
- `OrdinalWeekday`
- `ServiceWindowStart`
- `ServiceWindowEnd`
- `PlannedServiceIds`
- `Notes`
- `Status`
- `LastGeneratedThroughDate`
- `CreatedUtc`
- `UpdatedUtc`

### 6.2 ServiceVisits

Purpose:

- Stores actual scheduled visits.

Suggested keys:

- `PartitionKey`: `COMPANY#{CompanyId}#DATE#{yyyyMMdd}`
- `RowKey`: `VISIT#{ServiceVisitId}`

Fields:

- `CompanyId`
- `ServiceVisitId`
- `RecurringScheduleId`
- `CompanyClientId`
- `AssignedUserId`
- `ScheduledDate`
- `ServiceWindowStart`
- `ServiceWindowEnd`
- `Status`
- `PlannedServiceIds`
- `RouteOrder`
- `StartedUtc`
- `ArrivedUtc`
- `CompletedUtc`
- `CanceledUtc`
- `SkippedUtc`
- `CancelReason`
- `Notes`
- `CreatedUtc`
- `UpdatedUtc`

### 6.3 ServiceVisitsByUserDate

Purpose:

- Lookup table for field-user daily assignments.

Suggested keys:

- `PartitionKey`: `USER#{UserId}#DATE#{yyyyMMdd}`
- `RowKey`: `VISIT#{ServiceVisitId}`

Fields:

- `CompanyId`
- `ServiceVisitId`
- `CompanyClientId`
- `ClientDisplayName`
- `ServiceAddressSummary`
- `ScheduledDate`
- `ServiceWindowStart`
- `ServiceWindowEnd`
- `Status`
- `RouteOrder`

### 6.4 ServiceVisitsByClient

Purpose:

- Lookup table for client service history.

Suggested keys:

- `PartitionKey`: `CLIENT#{CompanyClientId}`
- `RowKey`: `DATE#{yyyyMMdd}#VISIT#{ServiceVisitId}`

Fields:

- `CompanyId`
- `CompanyClientId`
- `ServiceVisitId`
- `AssignedUserId`
- `ScheduledDate`
- `CompletedUtc`
- `Status`
- `CustomerVisibleSummary`

## 7. Visit Completion Tables

### 7.1 VisitServicesPerformed

Purpose:

- Stores services selected during visit completion.

Suggested keys:

- `PartitionKey`: `VISIT#{ServiceVisitId}`
- `RowKey`: `SERVICE#{ServiceId}`

Fields:

- `CompanyId`
- `ServiceVisitId`
- `ServiceId`
- `ServiceNameSnapshot`
- `Quantity`
- `UnitPriceSnapshot`
- `Notes`
- `CreatedUtc`

### 7.2 VisitMaterialsUsed

Purpose:

- Stores materials used during visit completion.

Suggested keys:

- `PartitionKey`: `VISIT#{ServiceVisitId}`
- `RowKey`: `MATERIAL#{MaterialId}`

Fields:

- `CompanyId`
- `ServiceVisitId`
- `MaterialId`
- `MaterialNameSnapshot`
- `UnitOfMeasureSnapshot`
- `Quantity`
- `UnitCostSnapshot`
- `BillableUnitPriceSnapshot`
- `Notes`
- `CreatedUtc`

### 7.3 VisitNotes

Purpose:

- Stores internal and customer-visible visit notes.

Suggested keys:

- `PartitionKey`: `VISIT#{ServiceVisitId}`
- `RowKey`: `NOTE#{VisitNoteId}`

Fields:

- `CompanyId`
- `ServiceVisitId`
- `VisitNoteId`
- `AuthorUserId`
- `Visibility`
- `NoteText`
- `CreatedUtc`

Visibility values:

- `Internal`
- `CustomerVisible`

## 8. Route Tables

### 8.1 DailyRoutes

Purpose:

- Stores optimized or manually ordered daily routes.

Suggested keys:

- `PartitionKey`: `USER#{UserId}#DATE#{yyyyMMdd}`
- `RowKey`: `ROUTE`

Fields:

- `CompanyId`
- `UserId`
- `RouteDate`
- `OrderedVisitIds`
- `OptimizationProvider`
- `EstimatedDistanceMiles`
- `EstimatedDurationMinutes`
- `LastOptimizedUtc`
- `UpdatedUtc`

## 9. Billing Tables

### 9.1 BillingAccounts

Purpose:

- Stores company-client billing configuration.

Suggested keys:

- `PartitionKey`: `CLIENT#{CompanyClientId}`
- `RowKey`: `BILLING`

Fields:

- `CompanyId`
- `CompanyClientId`
- `ClientTypeId`
- `Rate`
- `Currency`
- `StripeCustomerId`
- `BillingEmail`
- `BillingStatus`
- `UpdatedUtc`

### 9.2 Invoices

Purpose:

- Stores invoice snapshots and Stripe references.

Suggested keys:

- `PartitionKey`: `COMPANY#{CompanyId}#DATE#{yyyyMM}`
- `RowKey`: `INVOICE#{InvoiceId}`

Fields:

- `CompanyId`
- `InvoiceId`
- `CompanyClientId`
- `StripeInvoiceId`
- `InvoiceNumber`
- `InvoiceDate`
- `DueDate`
- `AmountSubtotal`
- `AmountTax`
- `AmountTotal`
- `AmountPaid`
- `Currency`
- `Status`
- `HostedInvoiceUrl`
- `PdfUrl`
- `CreatedUtc`
- `UpdatedUtc`

### 9.3 Payments

Purpose:

- Stores payment snapshots and Stripe references.

Suggested keys:

- `PartitionKey`: `CLIENT#{CompanyClientId}`
- `RowKey`: `PAYMENT#{PaymentDateUtcTicks}#{PaymentId}`

Fields:

- `CompanyId`
- `CompanyClientId`
- `PaymentId`
- `StripePaymentIntentId`
- `StripeChargeId`
- `StripeInvoiceId`
- `Amount`
- `Currency`
- `Status`
- `PaidUtc`
- `CreatedUtc`

### 9.4 StripeWebhookEvents

Purpose:

- Stores webhook idempotency and processing status.

Suggested keys:

- `PartitionKey`: `STRIPE_WEBHOOK`
- `RowKey`: `{StripeEventId}`

Fields:

- `StripeEventId`
- `EventType`
- `CompanyId`
- `ProcessingStatus`
- `ReceivedUtc`
- `ProcessedUtc`
- `FailureReason`
- `RetryCount`

## 10. Messaging Tables

### 10.1 MessageThreads

Purpose:

- Stores client-company message thread headers.

Suggested keys:

- `PartitionKey`: `COMPANY#{CompanyId}`
- `RowKey`: `THREAD#{MessageThreadId}`

Fields:

- `CompanyId`
- `MessageThreadId`
- `CompanyClientId`
- `Subject`
- `Status`
- `LastMessageUtc`
- `LastMessagePreview`
- `CreatedByUserId`
- `CreatedUtc`
- `ClosedUtc`

### 10.2 Messages

Purpose:

- Stores individual messages.

Suggested keys:

- `PartitionKey`: `THREAD#{MessageThreadId}`
- `RowKey`: `MSG#{CreatedUtcTicks}#{MessageId}`

Fields:

- `CompanyId`
- `MessageThreadId`
- `MessageId`
- `CompanyClientId`
- `SenderUserId`
- `SenderRole`
- `Body`
- `CreatedUtc`
- `ReadByCompanyUtc`
- `ReadByClientUtc`

## 11. Notification Tables

### 11.1 EmailJobs

Purpose:

- Stores email send attempts and status.

Suggested keys:

- `PartitionKey`: `COMPANY#{CompanyId}`
- `RowKey`: `EMAIL#{EmailJobId}`

Fields:

- `CompanyId`
- `EmailJobId`
- `EmailType`
- `RecipientEmail`
- `Subject`
- `TemplateId`
- `RelatedEntityType`
- `RelatedEntityId`
- `Status`
- `AttemptCount`
- `LastAttemptUtc`
- `SentUtc`
- `FailureReason`
- `CreatedUtc`

### 11.2 EmailLogs

Purpose:

- Stores every attempted email send for auditing, troubleshooting, and test-user reroute verification.

Suggested keys:

- `PartitionKey`: `COMPANY#{CompanyId}` when company-scoped, otherwise `PLATFORM`
- `RowKey`: `EMAILLOG#{CreatedUtcTicks}#{EmailLogId}`

Fields:

- `EmailLogId`
- `CompanyId`
- `EmailType`
- `RecipientUserId`
- `OriginalRecipientEmail`
- `RecipientEmail`
- `Subject`
- `Body`
- `Status`
- `ProviderMessageId`
- `FailureReason`
- `CreatedUtc`
- `SentUtc`

Statuses:

- `Queued`
- `Sent`
- `Failed`
- `TestRerouted`

## 12. Reporting Tables

### 12.1 ReportExports

Purpose:

- Stores generated report export metadata.

Suggested keys:

- `PartitionKey`: `COMPANY#{CompanyId}`
- `RowKey`: `REPORT#{ReportExportId}`

Fields:

- `CompanyId`
- `ReportExportId`
- `ReportType`
- `RequestedByUserId`
- `StartDate`
- `EndDate`
- `FilterJson`
- `BlobPath`
- `Status`
- `CreatedUtc`
- `CompletedUtc`
- `FailureReason`

## 13. Audit Tables

### 13.1 AuditEvents

Purpose:

- Stores security and business audit trail.

Suggested keys:

- `PartitionKey`: `COMPANY#{CompanyId}` for company events, or `PLATFORM` for platform events.
- `RowKey`: `AUDIT#{CreatedUtcTicks}#{AuditEventId}`

Fields:

- `AuditEventId`
- `CompanyId`
- `ActorUserId`
- `EventType`
- `TargetEntityType`
- `TargetEntityId`
- `Summary`
- `MetadataJson`
- `CreatedUtc`

## 14. Blob Storage Artifacts

### 14.1 Company Logo

Path:

- `company-assets/{CompanyId}/logo/{FileName}`

Metadata:

- `CompanyId`
- `UploadedByUserId`
- `UploadedUtc`

### 14.2 Visit Attachments

Path:

- `visit-attachments/{CompanyId}/{ServiceVisitId}/{FileName}`

Metadata:

- `CompanyId`
- `ServiceVisitId`
- `UploadedByUserId`
- `UploadedUtc`
- `Visibility`

### 14.3 Report Exports

Path:

- `report-exports/{CompanyId}/{ReportExportId}/{FileName}`

Metadata:

- `CompanyId`
- `ReportExportId`
- `RequestedByUserId`
- `CreatedUtc`

## 15. Queue Message Types

### 15.1 SendEmail

Fields:

- `JobId`
- `CompanyId`
- `EmailJobId`
- `IdempotencyKey`
- `CreatedUtc`

### 15.2 ProcessStripeWebhook

Fields:

- `JobId`
- `StripeEventId`
- `IdempotencyKey`
- `CreatedUtc`

### 15.3 GenerateRecurringVisits

Fields:

- `JobId`
- `CompanyId`
- `RecurringScheduleId`
- `GenerateThroughDate`
- `IdempotencyKey`
- `CreatedUtc`

### 15.4 OptimizeRoute

Fields:

- `JobId`
- `CompanyId`
- `UserId`
- `RouteDate`
- `IdempotencyKey`
- `CreatedUtc`

### 15.5 GenerateReportExport

Fields:

- `JobId`
- `CompanyId`
- `ReportExportId`
- `IdempotencyKey`
- `CreatedUtc`
