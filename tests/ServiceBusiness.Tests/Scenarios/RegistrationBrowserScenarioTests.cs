using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Microsoft.Playwright;

namespace ServiceBusiness.Tests.Scenarios;

public sealed class RegistrationBrowserScenarioTests
{
    [Fact]
    public async Task Independent_homeowner_can_register_and_open_pool_equipment()
    {
        await using var app = await TestBlazorApp.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await LaunchBrowserAsync(playwright);
        var page = await browser.NewPageAsync();

        await page.GotoAsync($"{app.BaseUrl}/register");
        await page.WaitForTimeoutAsync(1500);
        await page.GetByText("Independent homeowner").ClickAsync();
        await page.GetByLabel("Gmail email").FillAsync("scenario.homeowner@gmail.com");
        await page.GetByLabel("Display name").FillAsync("Scenario Homeowner");
        await page.GetByLabel("Phone").FillAsync("555-0999");
        await page.GetByLabel("Home address").FillAsync("500 Browser Scenario Way, Phoenix, AZ");
        await page.GetByLabel("Access notes").FillAsync("Equipment pad is behind the side gate.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Continue with Gmail" }).ClickAsync();

        await Expect(page.GetByText("Your homeowner account is ready")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Manage Pool Equipment" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Pool Equipment" })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Save Equipment" })).ToBeVisibleAsync();
    }

    private static ILocatorAssertions Expect(ILocator locator) => Assertions.Expect(locator);

    private static async Task<IBrowser> LaunchBrowserAsync(IPlaywright playwright)
    {
        foreach (var channel in new string?[] { null, "msedge", "chrome" })
        {
            try
            {
                return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Channel = channel,
                    Headless = true
                });
            }
            catch (PlaywrightException) when (channel is not "chrome")
            {
            }
        }

        return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "chrome",
            Headless = true
        });
    }

    private sealed class TestBlazorApp : IAsyncDisposable
    {
        private readonly Process process;

        private TestBlazorApp(Process process, string baseUrl)
        {
            this.process = process;
            BaseUrl = baseUrl;
        }

        public string BaseUrl { get; }

        public static async Task<TestBlazorApp> StartAsync()
        {
            var port = GetFreePort();
            var baseUrl = $"http://127.0.0.1:{port}";
            var repoRoot = FindRepoRoot();
            var artifactsPath = Path.Combine(Path.GetTempPath(), $"ServiceBusinessScenarioArtifacts-{Guid.NewGuid():N}");
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                ArgumentList =
                {
                    "run",
                    "--project",
                    Path.Combine(repoRoot, "src", "ServiceBusiness.Web", "ServiceBusiness.Web.csproj"),
                    "--artifacts-path",
                    artifactsPath,
                    "--no-launch-profile",
                    "--urls",
                    baseUrl
                },
                WorkingDirectory = repoRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }) ?? throw new InvalidOperationException("Could not start the Blazor test app.");

            var app = new TestBlazorApp(process, baseUrl);
            await app.WaitUntilReadyAsync();
            return app;
        }

        public async ValueTask DisposeAsync()
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            process.Dispose();
        }

        private async Task WaitUntilReadyAsync()
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(2)
            };
            var deadline = DateTimeOffset.UtcNow.AddSeconds(45);

            while (DateTimeOffset.UtcNow < deadline)
            {
                if (process.HasExited)
                {
                    var stdout = await process.StandardOutput.ReadToEndAsync();
                    var stderr = await process.StandardError.ReadToEndAsync();
                    throw new InvalidOperationException($"Blazor test app exited early.{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}");
                }

                try
                {
                    using var response = await httpClient.GetAsync(BaseUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        return;
                    }
                }
                catch (HttpRequestException)
                {
                }
                catch (TaskCanceledException)
                {
                }

                await Task.Delay(500);
            }

            throw new TimeoutException("Blazor test app did not become ready in time.");
        }

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static string FindRepoRoot([CallerFilePath] string sourceFilePath = "")
        {
            foreach (var startPath in new[] { Path.GetDirectoryName(sourceFilePath) ?? "", Environment.CurrentDirectory, AppContext.BaseDirectory })
            {
                var directory = new DirectoryInfo(startPath);
                while (directory is not null)
                {
                    if (File.Exists(Path.Combine(directory.FullName, "ServiceBusiness.slnx")))
                    {
                        return directory.FullName;
                    }

                    directory = directory.Parent;
                }
            }

            throw new InvalidOperationException("Could not locate the repository root.");
        }
    }
}
