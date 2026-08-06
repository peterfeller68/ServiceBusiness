using System.Security.Claims;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using ServiceBusiness.Web.Components;
using ServiceBusiness.Application;
using ServiceBusiness.Domain;
using ServiceBusiness.Infrastructure.AzureStorage;
using ServiceBusiness.Infrastructure.Integrations;
using ServiceBusiness.Web;

var builder = WebApplication.CreateBuilder(args);
var configuredPathBase = NormalizePathBase(builder.Configuration["Hosting:PathBase"]);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var applicationInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
{
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing => tracing.AddSource(ServiceBusinessTelemetry.SourceName))
        .WithMetrics(metrics => metrics.AddMeter(ServiceBusinessTelemetry.SourceName))
        .UseAzureMonitor(options =>
        {
            options.ConnectionString = applicationInsightsConnectionString;
        });
}

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var googleAuthConfigured = !string.IsNullOrWhiteSpace(googleClientId) &&
    !string.IsNullOrWhiteSpace(googleClientSecret);

var authenticationBuilder = builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = googleAuthConfigured
            ? GoogleDefaults.AuthenticationScheme
            : CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/signin";
        options.LogoutPath = "/auth/signout";
    });

if (googleAuthConfigured)
{
    authenticationBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
        options.ClaimActions.MapJsonKey("urn:google:picture", "picture", "url");
    });
}

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
if (bool.TryParse(builder.Configuration["AzureStorage:UseAzureStorage"], out var useAzureStorage) && useAzureStorage)
{
    builder.Services.AddSingleton<IServiceBusinessStore, AzureTableServiceBusinessStore>();
}
else
{
    builder.Services.AddSingleton<IServiceBusinessStore>(_ =>
        new InMemoryServiceBusinessStore(SystemSettingsConfiguration.GetConfiguredDefaults(builder.Configuration)));
}
builder.Services.AddHostedService<AzureStorageTableInitializer>();
builder.Services.AddSingleton<DemoCurrentUserContext>();
builder.Services.AddSingleton<ApplicationModeService>();
builder.Services.AddScoped<ICurrentUserContext, AuthenticatedCurrentUserContext>();
builder.Services.AddScoped<CurrentCompanyContext>();
builder.Services.AddSingleton<INotificationQueue, AzureCommunicationEmailNotificationQueue>();
builder.Services.AddSingleton<IEmailSender, AzureCommunicationEmailSender>();
builder.Services.AddScoped<TenantAuthorizationService>();
builder.Services.AddScoped<PlatformAdminService>();
builder.Services.AddScoped<CompanyAdminService>();
builder.Services.AddScoped<FieldWorkService>();
builder.Services.AddScoped<ClientPortalService>();
builder.Services.AddScoped<OnboardingService>();
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<IndependentHomeOwnerService>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<PaymentIntegrationService>();
builder.Services.AddScoped<PaymentLogService>();
builder.Services.AddHttpClient<IPaymentProviderGateway, StripePaymentProviderGateway>();
builder.Services.AddScoped<InvoicingJobService>();
builder.Services.AddScoped<EmailJobService>();
builder.Services.AddScoped<ScheduledJobRunner>();
builder.Services.AddScoped<EmailLogService>();
builder.Services.AddSingleton<UserGuideContentService>();
builder.Services.Configure<JobSchedulerOptions>(builder.Configuration.GetSection("Jobs:Scheduler"));
builder.Services.AddHostedService<ServiceBusinessJobScheduler>();

var app = builder.Build();

if (!string.IsNullOrWhiteSpace(configuredPathBase))
{
    app.UsePathBase(configuredPathBase);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapGet("/auth/google", async (HttpContext httpContext, IConfiguration configuration, string? returnUrl) =>
{
    if (string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"]) ||
        string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]))
    {
        return Results.BadRequest("Google Auth is not configured. Set Authentication:Google:ClientId and Authentication:Google:ClientSecret.");
    }

    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    var safeReturnUrl = GetSafeReturnUrl(returnUrl);
    return Results.Challenge(
        new AuthenticationProperties
        {
            RedirectUri = GetAppRedirectUrl(httpContext, $"/auth/google-complete?returnUrl={Uri.EscapeDataString(safeReturnUrl)}")
        },
        [GoogleDefaults.AuthenticationScheme]);
});

app.MapGet("/auth/google-complete", async (
    HttpContext httpContext,
    OnboardingService onboardingService,
    string? returnUrl,
    CancellationToken cancellationToken) =>
{
    if (httpContext.User.Identity?.IsAuthenticated != true)
    {
        return Results.Challenge();
    }

    var googleSubject = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var email = httpContext.User.FindFirstValue(ClaimTypes.Email);
    var displayName = httpContext.User.FindFirstValue(ClaimTypes.Name) ?? email ?? "";
    var profileImageUrl = httpContext.User.FindFirstValue("urn:google:picture");

    var overview = await onboardingService.CompleteGoogleSignInAsync(
        new GoogleUserProfile(googleSubject ?? "", email ?? "", displayName, profileImageUrl),
        cancellationToken);

    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        BuildAppPrincipal(overview.User, overview.AuthenticationSkipped));

    return Results.Redirect(GetAppRedirectUrl(httpContext, returnUrl));
});

app.MapGet("/auth/test-signin", async (
    HttpContext httpContext,
    IConfiguration configuration,
    OnboardingService onboardingService,
    string email,
    string? returnUrl,
    CancellationToken cancellationToken) =>
{
    if (!SystemSettingsConfiguration.IsDevTestEnabled(configuration))
    {
        return Results.BadRequest("Test mode is not enabled. Set SystemSettings:DevTest to true to skip Google authentication.");
    }

    var overview = await onboardingService.SignInAsync(email, cancellationToken);
    if (overview is null)
    {
        return Results.NotFound("Test user was not found.");
    }

    if (!overview.User.IsTestUser)
    {
        return Results.BadRequest("Only test users can skip Google authentication.");
    }

    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        BuildAppPrincipal(overview.User, overview.AuthenticationSkipped));

    return Results.Redirect(GetAppRedirectUrl(httpContext, returnUrl));
});

app.MapGet("/auth/registration-signin", async (
    HttpContext httpContext,
    IConfiguration configuration,
    OnboardingService onboardingService,
    string userId,
    string? returnUrl,
    CancellationToken cancellationToken) =>
{
    var overview = await onboardingService.GetAccessOverviewAsync(userId, cancellationToken);
    var authenticatedUserId = httpContext.User.FindFirstValue(ServiceBusinessClaimTypes.AppUserId);
    var canCompleteRegistration =
        string.Equals(authenticatedUserId, overview.User.Id, StringComparison.OrdinalIgnoreCase) ||
        (SystemSettingsConfiguration.IsDevTestEnabled(configuration) && overview.User.IsTestUser);

    if (!canCompleteRegistration)
    {
        return Results.Unauthorized();
    }

    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        BuildAppPrincipal(overview.User, overview.AuthenticationSkipped));

    return Results.Redirect(GetAppRedirectUrl(httpContext, returnUrl));
});

app.MapGet("/billing/homeowner/checkout", async (
    HttpContext httpContext,
    ICurrentUserContext currentUser,
    PaymentIntegrationService paymentIntegrationService,
    CancellationToken cancellationToken) =>
{
    var baseUrl = GetBaseUrl(httpContext);
    try
    {
        var session = await paymentIntegrationService.CreateHomeOwnerCheckoutSessionAsync(
            currentUser.UserId,
            $"{baseUrl}/billing/stripe/checkout-return?status=success&session_id={{CHECKOUT_SESSION_ID}}",
            $"{baseUrl}/billing/stripe/checkout-return?status=cancel",
            cancellationToken);
        return Results.Redirect(session.Url);
    }
    catch (InvalidOperationException)
    {
        return Results.Redirect(GetAppRedirectUrl(httpContext, "/profile?billing=checkout-unavailable"));
    }
}).RequireAuthorization();

app.MapGet("/billing/homeowner/portal", async (
    HttpContext httpContext,
    ICurrentUserContext currentUser,
    PaymentIntegrationService paymentIntegrationService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var session = await paymentIntegrationService.CreateHomeOwnerPortalSessionAsync(
            currentUser.UserId,
            $"{GetBaseUrl(httpContext)}/profile",
            cancellationToken);
        return Results.Redirect(session.Url);
    }
    catch (InvalidOperationException)
    {
        return Results.Redirect(GetAppRedirectUrl(httpContext, "/profile?billing=portal-unavailable"));
    }
}).RequireAuthorization();

app.MapGet("/billing/stripe/checkout-return", async (
    HttpContext httpContext,
    PaymentIntegrationService paymentIntegrationService,
    string? status,
    string? session_id,
    CancellationToken cancellationToken) =>
{
    await paymentIntegrationService.RecordCheckoutReturnAsync(status, session_id, cancellationToken);
    var billingStatus = string.Equals(status, "success", StringComparison.OrdinalIgnoreCase)
        ? "checkout-returned"
        : "checkout-canceled";
    return Results.Redirect(GetAppRedirectUrl(httpContext, $"/profile?billing={billingStatus}"));
});

app.MapPost("/billing/stripe/webhook", async (
    HttpContext httpContext,
    PaymentIntegrationService paymentIntegrationService,
    CancellationToken cancellationToken) =>
{
    using var reader = new StreamReader(httpContext.Request.Body);
    var payload = await reader.ReadToEndAsync(cancellationToken);
    var signature = httpContext.Request.Headers["Stripe-Signature"].ToString();
    try
    {
        await paymentIntegrationService.ProcessWebhookAsync(payload, signature, cancellationToken);
        return Results.Ok();
    }
    catch (InvalidOperationException ex)
    {
        await paymentIntegrationService.RecordRejectedWebhookAsync(ex.Message, cancellationToken);
        return Results.BadRequest(ex.Message);
    }
}).DisableAntiforgery();

app.MapGet("/auth/signout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect(GetAppRedirectUrl(httpContext, "/"));
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static ClaimsPrincipal BuildAppPrincipal(AppUser user, bool authenticationSkipped)
{
    var claims = new List<Claim>
    {
        new(ServiceBusinessClaimTypes.AppUserId, user.Id),
        new(ClaimTypes.Email, user.Email),
        new(ClaimTypes.Name, user.DisplayName),
        new("authentication_skipped", authenticationSkipped.ToString())
    };

    if (!string.IsNullOrWhiteSpace(user.GoogleSubjectId))
    {
        claims.Add(new(ClaimTypes.NameIdentifier, user.GoogleSubjectId));
    }

    if (!string.IsNullOrWhiteSpace(user.ProfileImageUrl))
    {
        claims.Add(new("urn:google:picture", user.ProfileImageUrl));
    }

    return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
}

static string GetSafeReturnUrl(string? returnUrl)
{
    if (string.IsNullOrWhiteSpace(returnUrl) ||
        !Uri.TryCreate(returnUrl, UriKind.Relative, out _) ||
        returnUrl.StartsWith("//", StringComparison.Ordinal))
    {
        return "/dashboard";
    }

    return returnUrl.StartsWith('/') ? returnUrl : $"/{returnUrl}";
}

static string GetAppRedirectUrl(HttpContext httpContext, string? returnUrl)
{
    var safeReturnUrl = GetSafeReturnUrl(returnUrl);
    var pathBase = httpContext.Request.PathBase.Value?.TrimEnd('/');
    if (string.IsNullOrWhiteSpace(pathBase) ||
        safeReturnUrl.Equals(pathBase, StringComparison.OrdinalIgnoreCase) ||
        safeReturnUrl.StartsWith($"{pathBase}/", StringComparison.OrdinalIgnoreCase))
    {
        return safeReturnUrl;
    }

    return $"{pathBase}{safeReturnUrl}";
}

static string GetBaseUrl(HttpContext httpContext) =>
    $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.PathBase}";

static string NormalizePathBase(string? pathBase)
{
    if (string.IsNullOrWhiteSpace(pathBase) || pathBase == "/")
    {
        return "";
    }

    var normalized = pathBase.Trim();
    if (!normalized.StartsWith('/'))
    {
        normalized = $"/{normalized}";
    }

    return normalized.TrimEnd('/');
}
