# Google Authentication Setup

The web app uses ASP.NET Core cookie authentication with the Google OAuth handler.

## Local Configuration

Create a Google OAuth client for a web application and configure this redirect URI:

```text
https://localhost:7191/signin-google
```

If you run the app on a different HTTPS port, add that port's `/signin-google` redirect URI in the Google Cloud Console too.

Store the client values with user secrets or environment variables instead of committing real secrets:

```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "<client-id>" --project src\ServiceBusiness.Web\ServiceBusiness.Web.csproj
dotnet user-secrets set "Authentication:Google:ClientSecret" "<client-secret>" --project src\ServiceBusiness.Web\ServiceBusiness.Web.csproj
```

The checked-in `appsettings.json` only contains empty placeholders.

## Runtime Flow

1. `/signin` sends real users to `/auth/google`.
2. `/auth/google` challenges the Google authentication handler.
3. Google redirects back to `/signin-google`, the default callback path used by the ASP.NET Core Google handler.
4. The app redirects to `/auth/google-complete`.
5. `/auth/google-complete` creates or updates the application user profile using the Google subject identifier, email, display name, and profile image claim.
6. The app signs in with an application cookie containing the app user ID.

## Test Users

Seeded users marked with `IsTestUser` can skip Google authentication through `/auth/test-signin`.

Current seeded test accounts:

```text
owner@clearwater.example
morgan@clearwater.example
homeowner-1@independent.com
pending.tech@gmail.com
```

Only users marked as test users can use the bypass endpoint. Non-test users must authenticate through Google.
