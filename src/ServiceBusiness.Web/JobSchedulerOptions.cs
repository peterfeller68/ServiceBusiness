namespace ServiceBusiness.Web;

public sealed class JobSchedulerOptions
{
    public bool Enabled { get; set; } = true;

    public int InitialDelaySeconds { get; set; } = 60;

    public int IntervalMinutes { get; set; } = 5;
}
