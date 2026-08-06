# Storage Entity List

This document describes the recommended Azure Storage-backed entity model. Entity names are logical; implementation may use Azure Table entities with `PartitionKey`, `RowKey`, `Timestamp`, and `ETag`.

## 1. Cross-Cutting Entity Rules

Current implementation note:

- `AzureStorageTableInitializer` provisions the currently modeled Azure Table names when `AzureStorage:UseAzureStorage` is enabled.
- `AzureTableServiceBusinessStore` is used when `AzureStorage:UseAzureStorage` is enabled.
- The Table-backed store seeds current MVP data when the `Users` table is empty.
- Seed data includes Clearwater plus the requested test companies `Pool1Clean1`, `PoolClean2`, `Landscape1`, and `Landscape2`, with richer users, memberships, service catalogs, material catalogs, and pool-equipment catalogs for each requested company.
- Seed data includes three independent homeowner test users using `homeowner-{n}@independent.com` emails; each homeowner has an `IndependentHomeOwnerProfiles` row and owner-scoped pool-equipment starter records.
- Seed data includes ten general test users using `other-{n}@gmail.com` emails; these users are flagged as test users and intentionally have no company memberships.
- The Table-backed store persists current MVP records as JSON payloads in Azure Table entities and keeps `UserByEmail` and `UserByGoogleSubject` lookup rows in sync.
- Pool equipment is persisted in `PoolEquipmentCategories` and `PoolEquipmentItems` using `EQUIPMENT_{Scope}_{ScopeOwnerId}` partitions.
- Independent Homeowner users are stored as `Users` rows without company membership rows; their owner profile is stored in `IndependentHomeOwnerProfiles`, and their equipment uses `EquipmentScope.HomeOwner` and `ScopeOwnerId = UserId`.
- `SystemSettings` stores singleton platform settings in Azure Table Storage using partition `SYSTEM_SETTINGS` and row `current`; `SystemMode` supports `Pool` and `Landscape`, `DevTest` controls test sign-in, `HomeOwnerTrialDays` controls new homeowner subscription trials, and configuration only supplies defaults when this row is missing.
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

Current implementation:

- Table name: `Companies`.
- Current persisted model fields are `Id`, `CompanyTypeId`, `Name`, `BusinessEmail`, `BusinessPhone`, `TimeZone`, `Status`, and optional `ServicePackageId`.
- Service client ids are normalized slugs.

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
- `EmailNotificationsEnabled`
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

Current implementation:

- Table name: `RoleDefinitions`.
- Role identities are fixed to `CompanyAdmin`, `CompanyUser`, and `CompanyClientUser`.
- Permission lists are normalized and stored with role metadata.

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
- `CompanyClientId`
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
- `CompanyClientUser`

Current implementation:

- Table name: `CompanyMemberships`.
- Partition key format: `COMPANY_{CompanyId}`.
- Row key format: `USER_{UserId}_ROLE_{Role}`.
- Company-admin user deactivation/reactivation updates membership `Status` between `Inactive` and `Active`.
- Company-scoped role reassignment marks the previous membership role `Removed` and creates or reactivates the replacement membership role with the previous active/inactive status.

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

Current implementation:

- Table name: `UserCompanyMemberships`.
- Partition key format: `USER#{UserId}`.
- Row key format: `COMPANY#{CompanyId}#ROLE#{Role}` so multiple role histories for the same user and company can coexist.
- The Azure Table store writes this lookup whenever a `CompanyMembership` is upserted.
- Startup hydration backfills this lookup from `CompanyMemberships` when the lookup table is empty but company membership rows already exist.

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

### 4.3 ServiceCategories

Purpose:

- Stores company-scoped service category groupings.

Suggested keys:

- `PartitionKey`: `COMPANY#{CompanyId}`
- `RowKey`: `SERVICE_CATEGORY#{ServiceCategoryId}`

Fields:

- `CompanyId`
- `ServiceCategoryId`
- `Name`
- `Description`
- `IsSystemManaged`
- `IsActive`
- `CreatedUtc`
- `UpdatedUtc`

Current implementation:

- Copy-as-custom creates a new service category row with a unique `-custom` ID and `IsSystemManaged = false`.

### 4.4 MaterialCategories

Purpose:

- Stores company-scoped material category groupings.

Suggested keys:

- `PartitionKey`: `COMPANY#{CompanyId}`
- `RowKey`: `MATERIAL_CATEGORY#{MaterialCategoryId}`

Fields:

- `CompanyId`
- `MaterialCategoryId`
- `Name`
- `Description`
- `IsSystemManaged`
- `IsActive`
- `CreatedUtc`
- `UpdatedUtc`

Current implementation:

- Table name: `MaterialCategories`.
- Global starter material categories use service-type catalog company ids such as `Pool_Global` and `LandScape_Global`; company records keep the real company id.
- Copy-as-custom creates a new material category row with a unique `-custom` ID and `IsSystemManaged = false`.
- Records are JSON payloads in Azure Table entities.

### 4.5 Services

Purpose:

- Stores company-offered services.

Suggested keys:

- `PartitionKey`: `COMPANY#{CompanyId}`
- `RowKey`: `SERVICE#{ServiceId}`

Fields:

- `CompanyId`
- `ServiceId`
- `CategoryId`
- `Name`
- `Description`
- `DefaultDurationMinutes`
- `DefaultPrice`
- `IsTaxable`
- `IsActive`
- `SortOrder`
- `CreatedUtc`
- `UpdatedUtc`

Current implementation:

- Table name: `Services`.
- Global starter services use partition keys such as `SERVICES_Pool_Global` and `SERVICES_LandScape_Global`; company services use `SERVICES_Company_{CompanyId}`.
- Copy-as-custom creates a new service row with a unique `-custom` ID in the same company scope.
- Records are JSON payloads in Azure Table entities.

### 4.6 Materials

Purpose:

- Stores company-used materials.

Suggested keys:

- `PartitionKey`: `COMPANY#{CompanyId}`
- `RowKey`: `MATERIAL#{MaterialId}`

Fields:

- `CompanyId`
- `MaterialId`
- `CategoryId`
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

Current implementation:

- Table name: `Materials`.
- Global starter materials use partition keys such as `MATERIALS_Pool_Global` and `MATERIALS_LandScape_Global`; company materials use `COMPANY_{CompanyId}`.
- Copy-as-custom creates a new material row with a unique `-custom` ID in the same company scope.
- Records are JSON payloads in Azure Table entities.

### 4.7 ServicePackages

Purpose:

- Stores global starter and company-scoped service packages.

Suggested keys:

- `PartitionKey`: `SERVICEPACKAGES#{Scope}#{ScopeOwnerId}`
- `RowKey`: `SERVICE_PACKAGE#{ServicePackageId}`

Fields:

- `CompanyId`
- `ServicePackageId`
- `Name`
- `Recurrence`
- `Description`
- `Cost`
- `IsActive`
- `Services`
- `CreatedUtc`
- `UpdatedUtc`

Current implementation:

- Table name: `ServicePackages`.
- Global starter service packages use partition keys such as `SERVICEPACKAGES_Pool_Global` and `SERVICEPACKAGES_LandScape_Global`.
- Company-scoped service packages use partition keys such as `SERVICEPACKAGES_Company_{CompanyId}`.
- Row key is the service package id.
- `Services` is persisted as part of the JSON payload and contains `ServicePackageService` entries with service id and recurrence.
- Delete removes the package row; active-state changes update `IsActive`.
- Records are JSON payloads in Azure Table entities.

### 4.8 PoolEquipmentCategories

Purpose:

- Stores global, company-scoped, and homeowner-scoped pool equipment categories.

Suggested keys:

- `PartitionKey`: `EQUIPMENT#{Scope}#{ScopeOwnerId}`
- `RowKey`: `EQUIPMENT_CATEGORY#{EquipmentCategoryId}`

Fields:

- `EquipmentCategoryId`
- `Scope`
- `ScopeOwnerId`
- `Manufacturer`
- `Name`
- `Description`
- `IsSystemManaged`
- `IsActive`
- `CreatedUtc`
- `UpdatedUtc`

Current implementation:

- Table name: `PoolEquipmentCategories`.
- Partition key format: `EQUIPMENT_{Scope}_{ScopeOwnerId}`.
- Row key: category ID.
- Copy-as-custom creates a new category row with a unique `-custom` ID and `IsSystemManaged = false`.
- Records are JSON payloads in Azure Table entities.

### 4.9 PoolEquipmentItems

Purpose:

- Stores global, company-scoped, and homeowner-scoped pool equipment item records.

Suggested keys:

- `PartitionKey`: `EQUIPMENT#{Scope}#{ScopeOwnerId}`
- `RowKey`: `EQUIPMENT_ITEM#{EquipmentItemId}`

Fields:

- `EquipmentItemId`
- `Scope`
- `ScopeOwnerId`
- `CategoryId`
- `Name`
- `Description`
- `ImageUrl`
- `IsActive`
- `CreatedUtc`
- `UpdatedUtc`

Current implementation:

- Table name: `PoolEquipmentItems`.
- Partition key format: `EQUIPMENT_{Scope}_{ScopeOwnerId}`.
- Row key: item ID.
- Copy-as-custom creates a new item row with a unique `-custom` ID in the same scope.
- `ImageUrl` stores a URL or blob reference string; direct blob upload remains a future UI slice.
- Pool configuration image uploads are stored separately as homeowner equipment photo records with data URL payloads.
- Records are JSON payloads in Azure Table entities.

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

Current implementation:

- Table name: `CompanyClients`.
- Current persisted model fields are `Id`, `CompanyId`, `DisplayName`, `PrimaryContactName`, `Email`, `Phone`, `ServiceAddress`, `AccessNotes`, `ClientTypeId`, optional `RateOverride`, `IsActive`, and optional `ServicePackageId`.
- The current model stores one service address string and does not yet split billing/service address components.

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

### 5.3 IndependentHomeOwnerProfiles

Purpose:

- Stores profile details for Independent Home Owner users who are not associated with a company tenant.

Suggested keys:

- `PartitionKey`: `HOMEOWNER_PROFILE`
- `RowKey`: `{UserId}`

Fields:

- `UserId`
- `HomeAddress`
- `AccessNotes`
- `CreatedUtc`
- `UpdatedUtc`

Current implementation:

- Table name: `IndependentHomeOwnerProfiles`.
- The row key is the homeowner `UserId`.
- Independent Homeowner registration creates or updates this profile before seeding owner-scoped equipment records.

### 5.4 HomeOwnerPoolEquipmentPhotos

Purpose:

- Stores uploaded pool configuration pictures for homeowner-scoped equipment configurations.

Suggested keys:

- `PartitionKey`: `HOMEOWNER_EQUIPMENT_PHOTOS#{ScopeOwnerId}`
- `RowKey`: `{PhotoId}`

Fields:

- `Id`
- `FileName`
- `ContentType`
- `DataUrl`
- `UploadedUtc`

Current implementation:

- Table name: `HomeOwnerPoolEquipmentPhotos`.
- The scope owner id is an Independent Home Owner user id or a `CompanyClient.Id` for a business-client pool configuration.
- Uploaded image payloads are stored as data URLs in table-backed records.
- Pool Configuration photo upload and delete use `CompanyAdminService.AddPoolConfigurationPhotosAsync` and `DeletePoolConfigurationPhotoAsync`.

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

- Stores actual service visits and the current visit completion snapshot.

Suggested keys:

- `PartitionKey`: `COMPANY_{CompanyId}`
- `RowKey`: `{VisitId}`

Fields:

- `Id`
- `CompanyId`
- `CompanyClientId`
- `AssignedUserId`
- `ScheduledDate`
- `ServiceWindowStart`
- `ServiceWindowEnd`
- `Status`
- `PlannedServiceIds`
- `RouteOrder`
- `Notes`
- `StartedUtc`
- `CompletedUtc`
- `VisitType`
- `VisitName`
- `NotesToBusinessClient`
- `NotesToServiceClient`
- `InternalNotes`
- `InvoiceId`
- `OutOfScopeServiceIds`
- `OutOfScopeMaterials`
- `CompletedByUserId`
- `CompletedServiceIds`
- `MaterialsUsed`

Current implementation notes:

- Completion details are stored directly on `ServiceVisit`.
- `InvoiceId` is a denormalized link to the `Invoices` table and is valid only when a matching invoice row exists for the same company.
- `ArrivedUtc`, `CanceledUtc`, `SkippedUtc`, `CancelReason`, `CreatedUtc`, and `UpdatedUtc` are not stored on the current `ServiceVisit` record.
- Date/user/client lookup tables are not separate physical tables in the current Azure Table implementation; queries read the service-client partition and filter in application code.

### 6.3 ServiceVisitsByUserDate

Purpose:

- Future lookup table for field-user daily assignments.

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

- Future lookup table for client visit history.

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

Current implementation:

- Visit completion details are stored directly on `ServiceVisit`.
- Separate visit completion tables are reserved for a future normalized storage model.

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

- Stores invoice snapshots generated from closed service visits.

Suggested keys:

- `PartitionKey`: `COMPANY_{CompanyId}`
- `RowKey`: `{InvoiceId}`

Fields:

- `InvoiceGuid`
- `CompanyId`
- `InvoiceId`
- `InvoiceDate`
- `PaidDate`
- `CompanyClientId`
- `VisitId`
- `ServicePackageId`
- `AdditionalServices`
- `Materials`
- `TotalCost`
- `Status`
- `InvoiceHtml`
- `CreatedUtc`

Status values:

- `New`
- `Invoiced`
- `Paid`

Future payment-provider fields:

- `StripeInvoiceId`
- `HostedInvoiceUrl`
- `PdfUrl`
- `AmountPaid`
- `Currency`

### 9.3 SubscriptionPlans

Purpose:

- Stores provider-neutral subscription plans for Independent Home Owner onboarding.

Implemented keys:

- `PartitionKey`: `SUBSCRIPTION_PLAN`
- `RowKey`: `{PlanId}`

Fields:

- `Id`
- `Name`
- `Description`
- `BillingInterval`
- `Price`
- `IsActive`
- `SortOrder`
- `ProviderPriceId`

### 9.4 HomeOwnerSubscriptions

Purpose:

- Stores provider-neutral Independent Home Owner subscription state.

Implemented keys:

- `PartitionKey`: `HOMEOWNER_SUBSCRIPTION`
- `RowKey`: `{OwnerUserId}`

Fields:

- `Id`
- `OwnerUserId`
- `PlanId`
- `Status`
- `TrialEndsAt`
- `CurrentPeriodStartsAt`
- `CurrentPeriodEndsAt`
- `CancelAtPeriodEnd`
- `ProviderCustomerId`
- `ProviderSubscriptionId`
- `ProviderCheckoutSessionId`
- `ProviderPriceId`
- `CreatedUtc`
- `UpdatedUtc`

### 9.5 Payments

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

### 9.6 PaymentProviderEvents

Purpose:

- Stores payment-provider event idempotency and processing status.

Implemented keys:

- `PartitionKey`: `PAYMENT_PROVIDER_{Provider}`
- `RowKey`: `{Provider}:{ProviderEventId}`

Fields:

- `Id`
- `Provider`
- `ProviderMode`
- `EventType`
- `RelatedEntityId`
- `Status`
- `Summary`
- `ReceivedUtc`
- `ProcessedUtc`

### 9.7 PaymentOperationLogs

Purpose:

- Stores sanitized payment API operation diagnostics separate from provider webhook idempotency rows.

Implemented keys:

- `PartitionKey`: `PAYMENT_OPERATION_{Operation}`
- `RowKey`: `{PaymentOperationLogId}`

Fields:

- `Id`
- `Operation`
- `Status`
- `Provider`
- `ProviderMode`
- `UserId`
- `SubscriptionId`
- `ProviderEventId`
- `ProviderCustomerId`
- `ProviderSubscriptionId`
- `ProviderCheckoutSessionId`
- `HttpStatusCode`
- `Summary`
- `FailureReason`
- `CreatedUtc`

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

- Future normalized queue/job table for email send attempts and status. The current implementation uses `EmailLogs` rows with `New` status as the email job backlog.

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
- `RowKey`: `{EmailLogId}`

Fields:

- `EmailLogId`
- `CompanyId`
- `EmailType`
- `RecipientUserId`
- `OriginalRecipientEmail`
- `RecipientEmail`
- `FromEmail`
- `CcEmail`
- `Subject`
- `Body`
- `Status`
- `ProviderMessageId`
- `FailureReason`
- `CreatedUtc`
- `SentUtc`

Statuses:

- `New`
- `Queued`
- `Sent`
- `Failed`
- `TestRerouted`
- `Suppressed`

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
