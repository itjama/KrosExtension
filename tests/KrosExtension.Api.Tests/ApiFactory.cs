using System.Net.Http.Json;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KrosExtension.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string BootstrapAdminPassword = "Bootstrap-Pass-1";
    public const string AdminPassword = "Admin-Pass-123";

    private readonly bool _ownsDatabase;
    private readonly SemaphoreSlim _adminSetupLock = new(1, 1);
    private bool _adminPasswordChanged;
    private readonly string _keysPath = Path.Combine(Path.GetTempPath(), "kros-tests-keys-" + Guid.NewGuid().ToString("N"));

    // Only one public constructor: xUnit requires it for class fixtures (xUnit1056).
    public ApiFactory()
        : this($"KrosExtension_Tests_{Guid.NewGuid():N}", ownsDatabase: true)
    {
    }

    protected ApiFactory(string databaseName, bool ownsDatabase)
    {
        DatabaseName = databaseName;
        _ownsDatabase = ownsDatabase;
    }

    /// <summary>Creates a factory bound to a specific database (e.g. to restart the app on the same data).</summary>
    public static ApiFactory ForDatabase(string databaseName, bool ownsDatabase) => new(databaseName, ownsDatabase);

    public string DatabaseName { get; }

    public HttpClient CreateCookieClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false });

    /// <summary>
    /// Logs in as the bootstrap admin. The first call per factory changes the bootstrap password to
    /// <see cref="AdminPassword"/>; later calls log in with it directly.
    /// </summary>
    public async Task<HttpClient> LoginAsAdminAsync(CancellationToken cancellationToken = default)
    {
        var client = CreateCookieClient();
        try
        {
            await _adminSetupLock.WaitAsync(cancellationToken);
            try
            {
                if (!_adminPasswordChanged)
                {
                    await PostOkAsync(client, "/api/auth/login",
                        new { userName = "admin", password = BootstrapAdminPassword }, cancellationToken);
                    await PostOkAsync(client, "/api/auth/change-password",
                        new { currentPassword = BootstrapAdminPassword, newPassword = AdminPassword }, cancellationToken);
                    _adminPasswordChanged = true;
                    return client;
                }
            }
            finally
            {
                _adminSetupLock.Release();
            }

            await PostOkAsync(client, "/api/auth/login",
                new { userName = "admin", password = AdminPassword }, cancellationToken);
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private static async Task PostOkAsync(HttpClient client, string url, object body, CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(url, body, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"POST {url} returned {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(cancellationToken)}");
        }
    }

    public virtual void ConfigureSettings(IDictionary<string, string?> settings)
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                $@"Server=(localdb)\MSSQLLocalDB;Database={DatabaseName};Trusted_Connection=True;TrustServerCertificate=True",
            ["Bootstrap:AdminUserName"] = "admin",
            ["Bootstrap:AdminPassword"] = BootstrapAdminPassword,
            ["Auth:CookieSecurePolicy"] = "SameAsRequest",
            ["Auth:LoginRateLimitPerMinute"] = "1000",
            ["DataProtection:KeysPath"] = _keysPath,
        };
        ConfigureSettings(settings);

        foreach (var (key, value) in settings)
        {
            builder.UseSetting(key, value);
        }

        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var hosted = services
                .Where(d => d.ServiceType == typeof(IHostedService)
                    && d.ImplementationType?.Name == "DataRefreshBackgroundService")
                .ToList();
            foreach (var descriptor in hosted)
            {
                services.Remove(descriptor);
            }
        });
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public override async ValueTask DisposeAsync()
    {
        if (_ownsDatabase)
        {
            await using var scope = Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureDeletedAsync();
        }

        await base.DisposeAsync();
        _adminSetupLock.Dispose();
        GC.SuppressFinalize(this);

        try
        {
            if (Directory.Exists(_keysPath))
            {
                Directory.Delete(_keysPath, recursive: true);
            }
        }
        catch (IOException)
        {
        }
    }
}
