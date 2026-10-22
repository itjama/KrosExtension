using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KrosExtension.Domain.Users;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace KrosExtension.Api.Tests;

public class AdminUserTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Password = "Correct-Pass-1";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static string UniqueName(string prefix = "user") => $"{prefix}{Guid.NewGuid():N}"[..20];

    private async Task<AppUser> SeedUserAsync(UserRole role = UserRole.User)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
        var now = DateTimeOffset.UtcNow;

        var user = new AppUser(UniqueName(), string.Empty, role, now);
        user.ChangePassword(hasher.HashPassword(user, Password), now);
        db.Users.Add(user);
        await db.SaveChangesAsync(Ct);
        return user;
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

    private static async Task<string?> ReadMessageAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<string>(Ct);

    private static async Task<Guid> CurrentUserIdAsync(HttpClient client)
    {
        var me = await ReadJsonAsync(await client.GetAsync("/api/auth/me", Ct));
        return me.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task User_CannotAccessAdminUsers_Returns403()
    {
        var user = await SeedUserAsync();
        using var client = await LoggedInClientAsync(user);

        var response = await client.GetAsync("/api/admin/users", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_ListsUsers_WithoutPasswordHash()
    {
        var user = await SeedUserAsync();
        using var admin = await factory.LoginAsAdminAsync(Ct);

        var response = await admin.GetAsync("/api/admin/users", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(Ct);
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Contains(json.EnumerateArray(), u => u.GetProperty("userName").GetString() == user.UserName);
    }

    [Fact]
    public async Task CreateUser_ReturnsTemporaryPassword_ThatForcesChange()
    {
        using var admin = await factory.LoginAsAdminAsync(Ct);
        var name = UniqueName();

        var response = await admin.PostAsJsonAsync("/api/admin/users", new { userName = name, role = "User" }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJsonAsync(response);
        var temp = json.GetProperty("temporaryPassword").GetString();
        Assert.Equal(12, temp!.Length);
        Assert.Equal(name, json.GetProperty("user").GetProperty("userName").GetString());

        using var client = factory.CreateCookieClient();
        var login = await LoginAsync(client, name, temp);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var me = await ReadJsonAsync(await client.GetAsync("/api/auth/me", Ct));
        Assert.True(me.GetProperty("mustChangePassword").GetBoolean());
    }

    [Fact]
    public async Task CreateUser_DuplicateDifferentCase_Returns409()
    {
        using var admin = await factory.LoginAsAdminAsync(Ct);
        var name = "pk" + Guid.NewGuid().ToString("N")[..8];

        var first = await admin.PostAsJsonAsync("/api/admin/users", new { userName = name, role = "User" }, Ct);
        var second = await admin.PostAsJsonAsync("/api/admin/users", new { userName = name.ToUpperInvariant(), role = "User" }, Ct);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("Používateľ s týmto menom už existuje.", await ReadMessageAsync(second));
    }

    [Fact]
    public async Task CreateUser_EmptyName_Returns400()
    {
        using var admin = await factory.LoginAsAdminAsync(Ct);

        var response = await admin.PostAsJsonAsync("/api/admin/users", new { userName = "   ", role = "User" }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Meno je povinné.", await ReadMessageAsync(response));
    }

    [Fact]
    public async Task ResetPassword_OldPasswordFails_TempWorks_AndOldSessionEnds()
    {
        var user = await SeedUserAsync();
        using var userClient = await LoggedInClientAsync(user);
        using var admin = await factory.LoginAsAdminAsync(Ct);

        var response = await admin.PostAsync($"/api/admin/users/{user.Id}/reset-password", null, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var temp = (await ReadJsonAsync(response)).GetProperty("temporaryPassword").GetString()!;

        Assert.Equal(HttpStatusCode.Unauthorized, (await userClient.GetAsync("/api/auth/me", Ct)).StatusCode);

        using var fresh = factory.CreateCookieClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(fresh, user.UserName, Password)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(fresh, user.UserName, temp)).StatusCode);
    }

    [Fact]
    public async Task ResetPassword_UnknownUser_Returns404()
    {
        using var admin = await factory.LoginAsAdminAsync(Ct);

        var response = await admin.PostAsync($"/api/admin/users/{Guid.NewGuid()}/reset-password", null, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Block_InvalidatesAllExistingSessions()
    {
        var user = await SeedUserAsync();
        using var first = await LoggedInClientAsync(user);
        using var second = await LoggedInClientAsync(user);
        using var admin = await factory.LoginAsAdminAsync(Ct);

        var response = await admin.PostAsync($"/api/admin/users/{user.Id}/block", null, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await first.GetAsync("/api/auth/me", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await second.GetAsync("/api/auth/me", Ct)).StatusCode);
    }

    [Fact]
    public async Task Block_Self_Returns400()
    {
        using var admin = await factory.LoginAsAdminAsync(Ct);
        var adminId = await CurrentUserIdAsync(admin);

        var response = await admin.PostAsync($"/api/admin/users/{adminId}/block", null, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Nemôžeš zablokovať ani zmeniť rolu sám sebe.", await ReadMessageAsync(response));
    }

    [Fact]
    public async Task ChangeRole_Self_Returns400()
    {
        using var admin = await factory.LoginAsAdminAsync(Ct);
        var adminId = await CurrentUserIdAsync(admin);

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{adminId}/role", new { role = "User" }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Nemôžeš zablokovať ani zmeniť rolu sám sebe.", await ReadMessageAsync(response));
    }

    [Fact]
    public async Task ChangeRole_MissingRole_Returns400()
    {
        var user = await SeedUserAsync();
        using var admin = await factory.LoginAsAdminAsync(Ct);

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{user.Id}/role", new { }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Rola je povinná.", await ReadMessageAsync(response));
    }

    [Fact]
    public async Task ChangeRole_UndefinedRoleZero_Returns400()
    {
        var user = await SeedUserAsync();
        using var admin = await factory.LoginAsAdminAsync(Ct);

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{user.Id}/role", new { role = 0 }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_UndefinedRole_Returns400_AndDoesNotCreate()
    {
        using var admin = await factory.LoginAsAdminAsync(Ct);
        var name = UniqueName();

        var response = await admin.PostAsJsonAsync("/api/admin/users", new { userName = name, role = 99 }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var list = await ReadJsonAsync(await admin.GetAsync("/api/admin/users", Ct));
        Assert.DoesNotContain(list.EnumerateArray(), u => u.GetProperty("userName").GetString() == name);
    }

    [Fact]
    public async Task Unblock_AllowsLoginAgain()
    {
        var user = await SeedUserAsync();
        using var admin = await factory.LoginAsAdminAsync(Ct);
        await admin.PostAsync($"/api/admin/users/{user.Id}/block", null, Ct);
        using var client = factory.CreateCookieClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(client, user.UserName, Password)).StatusCode);

        var response = await admin.PostAsync($"/api/admin/users/{user.Id}/unblock", null, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, user.UserName, Password)).StatusCode);
    }

    [Fact]
    public async Task ChangeRole_UserToAdmin_GrantsAdminAccess()
    {
        var user = await SeedUserAsync();
        using var admin = await factory.LoginAsAdminAsync(Ct);

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{user.Id}/role", new { role = "Admin" }, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var client = await LoggedInClientAsync(user);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/admin/users", Ct)).StatusCode);
    }
}
