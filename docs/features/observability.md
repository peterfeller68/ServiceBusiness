# Observability

Status: Implemented
Owner: Product
Last reviewed: 2026-08-05

## Problem

Operators need telemetry for request health, dependencies, and business workflow counters so failures can be diagnosed in Azure Application Insights.

## Personas

- Developer/Operator
- System Administrator

## Requirements

- The web app uses Azure Monitor OpenTelemetry when `ApplicationInsights:ConnectionString` is configured.
- Application telemetry uses the `ServiceBusiness` activity source and meter.
- Important business workflows emit activities and counters.
- Operational setup is documented under operations docs.

## User Flows

### Enable Telemetry

1. An operator configures `ApplicationInsights:ConnectionString`.
2. The web app registers Azure Monitor OpenTelemetry.
3. ASP.NET Core request/dependency telemetry and application meters/traces are exported.

### Business Workflow Emits Telemetry

1. A workflow such as account approval, visit completion, or email notification runs.
2. The application emits activity/counter telemetry.
3. Operators inspect Application Insights outside the app.

## UI Expectations

- There is no in-app observability dashboard.
- Dashboard telemetry widgets remain future work.

## Data Model Impact

- No business table changes.
- Telemetry names are defined in `ServiceBusinessTelemetry`.

## Authorization Rules

- Application Insights access is managed outside the application in Azure.
- No app role grants telemetry access inside the WebApp.

## Acceptance Criteria

- [x] Web app references Azure Monitor OpenTelemetry.
- [x] Telemetry is enabled only when `ApplicationInsights:ConnectionString` is configured.
- [x] Custom ActivitySource and Meter are named `ServiceBusiness`.
- [x] Counters exist for account approval decisions, completed visits, and email notifications.
- [x] Workflow code emits telemetry for account approvals, visit completion, and email notification attempts.
- [ ] No in-app telemetry dashboard exists.
- [ ] No automated tests directly assert telemetry emission.

## Tests

- No direct telemetry tests currently exist.
- Related workflow tests cover business behavior for approval, visit completion, and email notifications.

## User Documentation Impact

- Observability is operational behavior and does not require a user guide page.
- Operational setup is documented in [Observability Setup](../operations/observability-setup.md).

## Current Implementation

- `Program.cs` registers OpenTelemetry tracing and metrics with Azure Monitor when the connection string is present.
- `ServiceBusinessTelemetry` defines `ActivitySource`, `Meter`, and counters.
- `CompanyAdminService.DecideAccessRequestAsync` emits approval activity/counter telemetry.
- `FieldWorkService.CompleteVisitAsync` emits completed-visit activity/counter telemetry.
- `AzureCommunicationEmailNotificationQueue` emits send-notification activity tags and email notification counters.

## Outstanding Tasks

- Add tests or smoke checks for telemetry registration.
- Add in-app health/telemetry dashboards only if product requirements call for them.
- Expand telemetry around invoice job, scheduler, and report generation as those workflows mature.

## Change Log

- 2026-08-05: Created implemented observability spec from OpenTelemetry registration, telemetry helpers, and operations docs.
