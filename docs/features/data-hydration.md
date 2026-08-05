# Data Hydration

Status: Implemented
Owner: Product
Last reviewed: 2026-08-05

## Problem

Local development and new Azure Table deployments need enough representative data to exercise roles, companies, catalogs, equipment, service packages, visits, test users, and homeowner scenarios.

## Personas

- Developer/Tester
- System Administrator

## Requirements

- In-memory storage seeds representative data at construction time.
- Azure Table storage initialization creates required tables and hydrates missing seed rows.
- Seed data includes system admin, demo users, company memberships, service clients, business clients, catalogs, service packages, pool equipment, visits, independent homeowners, and general test users.
- Global Pool and Landscape catalog scopes are seeded separately.
- Richer test companies include Pool1Clean1, PoolClean2, Landscape1, and Landscape2.

## User Flows

### Local Development Starts

1. The app uses `InMemoryServiceBusinessStore` when Azure storage is disabled.
2. The store seeds users, roles, companies, catalogs, service packages, equipment, visits, and email logs.

### Azure Storage Initializes

1. `AzureStorageTableInitializer` runs as a hosted service.
2. Required Azure Tables are created.
3. `AzureTableServiceBusinessStore` hydrates missing seed records from the in-memory seed source.

## UI Expectations

- There is no dedicated data-hydration UI.
- Seeded records appear throughout dashboards, settings pages, catalogs, scheduling, invoices, logs, and test-user sign-in.

## Data Model Impact

- Hydration touches most tables: users, roles, companies, memberships, client types, clients, catalogs, service packages, materials, equipment, visits, homeowner profiles/photos/history, invoices, email logs, and system settings.
- Global catalog records use service-type-specific scopes for Pool and Landscape.
- Test data uses `AppUser.IsTestUser = true`.

## Authorization Rules

- Hydration is an infrastructure startup concern, not an end-user action.
- Users still see or manage hydrated data only through normal role-based authorization.

## Acceptance Criteria

- [x] In-memory store seeds representative default data.
- [x] Azure Table initializer creates required tables.
- [x] Azure Table storage hydrates global catalog rows for Pool and Landscape.
- [x] Azure Table storage hydrates company-scoped records and memberships.
- [x] Seed data includes at least four richer companies across Pool and Landscape.
- [x] Seed data includes independent homeowner test users and owner-scoped equipment.
- [x] Seed data includes general test users not associated with any company.
- [ ] Hydration is seed-oriented and not a migration/versioning framework.

## Tests

- `AuthorizationTests.Seed_data_includes_requested_test_companies_catalogs_and_users`
- `OnboardingTests.Independent_homeowner_registration_creates_active_owner_workspace_without_company_membership`
- Catalog and service package tests also rely on hydrated seed rows.

## User Documentation Impact

- Data hydration is operational/development behavior and does not require a user guide page.
- Related setup remains in operations documentation.

## Current Implementation

- `InMemoryServiceBusinessStore.Seed` creates the default in-memory data set.
- `AzureStorageTableInitializer` is registered as a hosted service.
- `AzureTableServiceBusinessStore` creates/hydrates Azure Table records by table and partition.
- Seeded data includes demo users, pending users, rich companies, global catalogs, company catalogs, service packages, pool equipment, independent homeowners, and general test users.

## Outstanding Tasks

- Add explicit documentation for seed-data ownership and when seed records may be changed.
- Add migration/version tracking if production schema evolution requires ordered migrations.
- Add tests for Azure hydration idempotency beyond partition-key coverage.

## Change Log

- 2026-08-05: Created implemented data-hydration spec from in-memory seed, Azure initialization, and tests.
