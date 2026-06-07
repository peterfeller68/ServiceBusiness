using System.Security.Claims;
using ServiceBusiness.Application;

namespace ServiceBusiness.Web;

public sealed class AuthenticatedCurrentUserContext(
    IHttpContextAccessor httpContextAccessor,
    DemoCurrentUserContext demoCurrentUserContext) : ICurrentUserContext
{
    public string UserId =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ServiceBusinessClaimTypes.AppUserId)
        ?? demoCurrentUserContext.UserId;
}

public static class ServiceBusinessClaimTypes
{
    public const string AppUserId = "service_business_user_id";
}
