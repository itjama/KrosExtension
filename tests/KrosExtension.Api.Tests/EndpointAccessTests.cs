using System.Net;
using System.Net.Http.Json;
using KrosExtension.Domain.Users;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace KrosExtension.Api.Tests;

public class EndpointAccessTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Password = "Correct-Pass-1";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static TheoryData<string, string, bool> AdminEndpoints => new()
    {
        { "GET", "/api/data-read-servers", true },
        { "GET", "/api/omega/refresh-settings", true },
        { "GET", "/api/olymp/refresh-settings", true },
        { "PUT", "/api/omega/refresh-settings", false },
        { "GET", "/api/omega/companies/tracked", true },
        { "GET", "/api/omega/companies/discover", false },
        { "GET", "/api/databases/configured", true },
        { "GET", "/api/companies/data-sources", true },
    };

    private async Task<HttpClient> UserClientAsync()
    {
        var name = $"user{Guid.NewGuid():N}"[..20];
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
            var now = DateTimeOffset.UtcNow;
            var user = new AppUser(name, string.Empty, UserRole.User, now);
            user.ChangePassword(hasher.HashPassword(user, Password), now);
            db.Users.Add(user);
            await db.SaveChangesAsync(Ct);
        }

        var client = factory.CreateCookieClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName = name, password = Password }, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return client;
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "PUT")
        {
            request.Content = JsonContent.Create(new { refreshTime = "02:00" });
        }

        return client.SendAsync(request, Ct);
    }

    [Theory]
    [MemberData(nameof(AdminEndpoints))]
    public async Task User_IsForbiddenOnAdminEndpoints(string method, string path, bool expectOkForAdmin)
    {
        _ = expectOkForAdmin;
        using var client = await UserClientAsync();

        using var response = await SendAsync(client, method, path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AdminEndpoints))]
    public async Task Admin_IsAllowedOnAdminEndpoints(string method, string path, bool expectOk)
    {
        using var client = await factory.LoginAsAdminAsync(Ct);

        using var response = await SendAsync(client, method, path);

        if (expectOk)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        else
        {
            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Theory]
    [InlineData("/api/omega/dashboard/companies")]
    [InlineData("/api/olymp/dashboard/companies")]
    [InlineData("/api/system/info")]
    public async Task User_CanReadDashboardsAndSystemInfo(string path)
    {
        using var client = await UserClientAsync();

        using var response = await client.GetAsync(path, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task User_CanTriggerManualUpdate_NotForbidden()
    {
        using var client = await UserClientAsync();

        using var response = await client.PostAsJsonAsync(
            "/api/omega/companies/update-data", new { databaseName = (string?)null }, Ct);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
