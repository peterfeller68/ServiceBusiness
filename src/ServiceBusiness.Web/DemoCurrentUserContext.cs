using ServiceBusiness.Application;

namespace ServiceBusiness.Web;

public sealed class DemoCurrentUserContext : ICurrentUserContext
{
    public string UserId { get; set; } = "clearwater-owner-1";
}
