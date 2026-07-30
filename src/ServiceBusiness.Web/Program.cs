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
builder.Services.AddScoped<TenantAuthorizationService>();
builder.Services.AddScoped<PlatformAdminService>();
builder.Services.AddScoped<CompanyAdminService>();
builder.Services.AddScoped<FieldWorkService>();
builder.Services.AddScoped<ClientPortalService>();
builder.Services.AddScoped<OnboardingService>();
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<IndependentHomeOwnerService>();

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

app.MapGet("/auth/google", (HttpContext httpContext, IConfiguration configuration, string? returnUrl) =>
{
    if (string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"]) ||
        string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]))
    {
        return Results.BadRequest("Google Auth is not configured. Set Authentication:Google:ClientId and Authentication:Google:ClientSecret.");
    }

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
    IServiceBusinessStore store,
    OnboardingService onboardingService,
    string email,
    string? returnUrl,
    CancellationToken cancellationToken) =>
{
    if (!await SystemSettingsConfiguration.IsDevTestEnabledAsync(configuration, store, cancellationToken))
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
