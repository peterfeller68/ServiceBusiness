using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ServiceBusiness.Application;

public static class ServiceBusinessTelemetry
{
    public const string SourceName = "ServiceBusiness";
    public static readonly ActivitySource ActivitySource = new(SourceName);
    public static readonly Meter Meter = new(SourceName);
    public static readonly Counter<long> AccountApprovalDecisions = Meter.CreateCounter<long>("servicebusiness.account_approval_decisions");
    public static readonly Counter<long> VisitCompletions = Meter.CreateCounter<long>("servicebusiness.visit_completions");
    public static readonly Counter<long> EmailNotifications = Meter.CreateCounter<long>("servicebusiness.email_notifications");
}
