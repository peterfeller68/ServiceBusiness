# Reports

Status: Partial
Owner: Product
Last reviewed: 2026-08-05

## Problem

Users need a stable reporting destination for operational reports, even though report generation, filters, and exports are not yet implemented.

## Personas

- System Administrator
- Business Owner
- Business Employee
- Business Client
- Independent Home Owner

## Requirements

- Authenticated navigation exposes Reports.
- `/reports` is a stable route.
- The page communicates that operational reports are being prepared.
- Future reporting should build on completed visits, catalog items, users, clients, and billing records.

## User Flows

### Open Reports

1. An authenticated user opens Reports.
2. The page loads `/reports`.
3. The user sees a placeholder message that reporting is being prepared.

## UI Expectations

- Page heading is Operational reports.
- Page eyebrow is Reports.
- A callout explains that reports are being prepared.
- No filters, exports, report tables, charts, or scheduled reports are shown.

## Data Model Impact

- No current data model changes.
- Future reports should query existing operational tables by partition-friendly dimensions and date ranges.

## Authorization Rules

- Reports navigation is shown to authenticated, non-pending-only users.
- No report-specific role filtering exists because no report data is exposed yet.

## Acceptance Criteria

- [x] `/reports` route exists.
- [x] Reports navigation group links to `/reports`.
- [x] Page clearly states reports are being prepared.
- [ ] No report filters are implemented.
- [ ] No report data tables/charts are implemented.
- [ ] No export workflows are implemented.

## Tests

- No dedicated Reports tests currently exist.

## User Documentation Impact

- User-facing placeholder behavior is documented in [Reports](../user-guide/reports.md).

## Current Implementation

- `ReportsPage.razor` renders a static placeholder page.
- `NavMenu.razor` renders the Reports navigation group for authenticated users after visit scheduling and before logs/help/test sections.

## Outstanding Tasks

- Define first report set and persona-specific visibility rules.
- Add report filters, tables/charts, and exports.
- Add tests once report data is implemented.

## Change Log

- 2026-08-05: Created partial reports feature spec documenting the implemented placeholder route and outstanding report generation work.
