using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KrosExtension.Domain.Users;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KrosExtension.Api.Tests;

public class AuthTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Password = "Correct-Pass-1";
    private const string InvalidCredentialsMessage = "Nesprávne meno alebo heslo";
    private const string LockedOutMessage = "Účet je dočasne zamknutý. Skús to neskôr.";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static string UniqueName(string prefix = "user") => $"{prefix}{Guid.NewGuid():N}"[..20];

    private async Task<AppUser> CreateUserAsync(
        string? userName = null,
        bool mustChangePassword = false,
        Action<AppUser>? configure = null)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
        var now = DateTimeOffset.UtcNow;

        var user = new AppUser(userName ?? UniqueName(), string.Empty, UserRole.User, now);
        var hash = hasher.HashPassword(user, Password);
        if (mustChangePassword)
        {
            user.SetPasswordHash(hash);
        }
        else
        {
            user.ChangePassword(hash, now);
        }

        configure?.Invoke(user);
        db.Users.Add(user);
        await db.SaveChangesAsync(Ct);
        return user;
    }

    private async Task UpdateUserAsync(Guid id, Action<AppUser> update)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.SingleAsync(u => u.Id == id, Ct);
        update(user);
        await db.SaveChangesAsync(Ct);
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string userName, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { userName, password }, Ct);

    private async Task<HttpClient> LoggedInClientAsync(AppUser user)
    {
        var client = factory.CreateCookieClient();
        var response = await LoginAsync(client, user.UserName, Password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return client;
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Ct);

    // Results.BadRequest("text") (the project convention) serializes the message as a JSON string.
    private static async Task<string?> ReadBadRequestMessageAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<string>(Ct);

    [Fact]
    public async Task Anonymous_ProtectedEndpoint_Returns401()
    {
        using var client = factory.CreateCookieClient();

        var response = await client.GetAsync("/api/system/info", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task Anonymous_Health_Returns200()
    {
        using var client = factory.CreateCookieClient();

        var response = await client.GetAsync("/health", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401WithGenericMessage()
    {
        var user = await CreateUserAsync();
        using var client = factory.CreateCookieClient();

        var response = await LoginAsync(client, user.UserName, "Wrong-Pass-99");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(InvalidCredentialsMessage, await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task Login_UnknownUser_Returns401WithSameMessage()
    {
        using var client = factory.CreateCookieClient();

        var response = await LoginAsync(client, UniqueName("ghost"), Password);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(InvalidCredentialsMessage, await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task Login_MissingFields_Returns401()
    {
        using var client = factory.CreateCookieClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { }, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(InvalidCredentialsMessage, await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task Login_BlockedUser_Returns401()
    {
        var user = await CreateUserAsync(configure: u => u.Block());
        using var client = factory.CreateCookieClient();

        var response = await LoginAsync(client, user.UserName, Password);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(InvalidCredentialsMessage, await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task Login_IsCaseAndWhitespaceInsensitive()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await CreateUserAsync("jnovak" + suffix);
        using var client = factory.CreateCookieClient();

        var response = await LoginAsync(client, $" JNovak{suffix.ToUpperInvariant()} ", Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_FiveWrongPasswords_ThenCorrect_Returns423()
    {
        var user = await CreateUserAsync();
        using var client = factory.CreateCookieClient();

        for (var i = 0; i < AppUser.MaxFailedLogins; i++)
        {
            var wrong = await LoginAsync(client, user.UserName, "Wrong-Pass-99");
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        }

        var response = await LoginAsync(client, user.UserName, Password);

        Assert.Equal((HttpStatusCode)423, response.StatusCode);
        Assert.Equal(LockedOutMessage, await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task Login_LockedOut_WrongPassword_Returns401()
    {
        var user = await CreateUserAsync(configure: u =>
        {
            for (var i = 0; i < AppUser.MaxFailedLogins; i++)
            {
                u.RegisterFailedLogin(DateTimeOffset.UtcNow);
            }
        });
        using var client = factory.CreateCookieClient();

        var response = await LoginAsync(client, user.UserName, "Wrong-Pass-99");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(InvalidCredentialsMessage, await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task Login_Success_ReturnsUserAndSetsHardenedCookie()
    {
        var user = await CreateUserAsync();
        using var client = factory.CreateCookieClient();

        var response = await LoginAsync(client, user.UserName, Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal(user.Id, json.GetProperty("id").GetGuid());
        Assert.Equal(user.UserName, json.GetProperty("userName").GetString());
        Assert.Equal("User", json.GetProperty("role").GetString());
        Assert.False(json.GetProperty("mustChangePassword").GetBoolean());

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("LedgerPulse.Auth="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Me_AfterLogin_ReturnsUser()
    {
        var user = await CreateUserAsync();
        using var client = await LoggedInClientAsync(user);

        var response = await client.GetAsync("/api/auth/me", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal(user.Id, json.GetProperty("id").GetGuid());
        Assert.Equal(user.UserName, json.GetProperty("userName").GetString());
        Assert.Equal("User", json.GetProperty("role").GetString());
        Assert.False(json.GetProperty("mustChangePassword").GetBoolean());
    }

    [Fact]
    public async Task Logout_ThenMe_Returns401()
    {
        var user = await CreateUserAsync();
        using var client = await LoggedInClientAsync(user);

        var logout = await client.PostAsync("/api/auth/logout", null, Ct);
        var me = await client.GetAsync("/api/auth/me", Ct);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task MustChangePassword_BlocksOtherEndpoints()
    {
        var user = await CreateUserAsync(mustChangePassword: true);
        using var client = await LoggedInClientAsync(user);

        var info = await client.GetAsync("/api/system/info", Ct);
        var me = await client.GetAsync("/api/auth/me", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, info.StatusCode);
        Assert.Null(info.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.True((await ReadJsonAsync(me)).GetProperty("mustChangePassword").GetBoolean());
    }

    [Fact]
    public async Task ChangePassword_TooShort_Returns400()
    {
        var user = await CreateUserAsync(mustChangePassword: true);
        using var client = await LoggedInClientAsync(user);

        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password", new { currentPassword = Password, newPassword = "short" }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Heslo musí mať aspoň 10 znakov.", await ReadBadRequestMessageAsync(response));
    }

    [Fact]
    public async Task ChangePassword_SameAsCurrent_Returns400()
    {
        var user = await CreateUserAsync(mustChangePassword: true);
        using var client = await LoggedInClientAsync(user);

        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password", new { currentPassword = Password, newPassword = Password }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Nové heslo sa musí líšiť od súčasného.", await ReadBadRequestMessageAsync(response));
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_Returns400()
    {
        var user = await CreateUserAsync(mustChangePassword: true);
        using var client = await LoggedInClientAsync(user);

        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password", new { currentPassword = "Wrong-Pass-99", newPassword = "Brand-New-Pass-1" }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Súčasné heslo nie je správne.", await ReadBadRequestMessageAsync(response));
    }

    [Fact]
    public async Task ChangePassword_ReissuesCookie_SameClientCanContinue()
    {
        var user = await CreateUserAsync(mustChangePassword: true);
        using var client = await LoggedInClientAsync(user);

        var change = await client.PostAsJsonAsync(
            "/api/auth/change-password", new { currentPassword = Password, newPassword = "Brand-New-Pass-1" }, Ct);
        var info = await client.GetAsync("/api/system/info", Ct);
        var me = await client.GetAsync("/api/auth/me", Ct);

        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        Assert.False((await ReadJsonAsync(change)).GetProperty("mustChangePassword").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, info.StatusCode);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.False((await ReadJsonAsync(me)).GetProperty("mustChangePassword").GetBoolean());
    }

    [Fact]
    public async Task StaleSecurityStamp_Returns401()
    {
        var user = await CreateUserAsync();
        using var client = await LoggedInClientAsync(user);

        await UpdateUserAsync(user.Id, u =>
        {
            u.Block();
            u.Unblock();
        });
        var me = await client.GetAsync("/api/auth/me", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task BlockedAfterLogin_Returns401()
    {
        var user = await CreateUserAsync();
        using var client = await LoggedInClientAsync(user);

        await UpdateUserAsync(user.Id, u => u.Block());
        var me = await client.GetAsync("/api/auth/me", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task LoginAsAdminAsync_ReturnsClientWithFullAccess()
    {
        using var first = await factory.LoginAsAdminAsync(Ct);
        using var second = await factory.LoginAsAdminAsync(Ct);

        var me = await second.GetAsync("/api/auth/me", Ct);
        var info = await first.GetAsync("/api/system/info", Ct);

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var json = await ReadJsonAsync(me);
        Assert.Equal("Admin", json.GetProperty("role").GetString());
        Assert.False(json.GetProperty("mustChangePassword").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, info.StatusCode);
    }
}
