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
    private readonly string _keysPath = Path.Combine(Path.GetTempPath(), "kros-tests-keys-" + Guid.NewGuid().ToString("N"));

    public ApiFactory()
        : this($"KrosExtension_Tests_{Guid.NewGuid():N}", ownsDatabase: true)
    {
    }

    public ApiFactory(string databaseName, bool ownsDatabase)
    {
        DatabaseName = databaseName;
        _ownsDatabase = ownsDatabase;
    }

    public string DatabaseName { get; }

    public HttpClient CreateCookieClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false });

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
