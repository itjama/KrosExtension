using KrosExtension.Domain.Users;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KrosExtension.Api.Tests;

public class BootstrapTests
{
    private sealed class NoPasswordFactory : ApiFactory
    {
        public override void ConfigureSettings(IDictionary<string, string?> settings) =>
            settings["Bootstrap:AdminPassword"] = "";
    }

    private static async Task<T> WithDbAsync<T>(ApiFactory factory, Func<ApplicationDbContext, Task<T>> action)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    private static string NewDbName() => $"KrosExtension_Tests_{Guid.NewGuid():N}";

    [Fact]
    public async Task Bootstrap_NoAdmin_CreatesAdminThatMustChangePassword()
    {
        await using var factory = new ApiFactory();

        var user = await WithDbAsync(factory, db => db.Users.SingleAsync());

        Assert.Equal("ADMIN", user.NormalizedUserName);
        Assert.Equal(UserRole.Admin, user.Role);
        Assert.True(user.MustChangePassword);
    }

    [Fact]
    public async Task Bootstrap_ActiveAdminExists_DoesNothing()
    {
        var name = NewDbName();
        string changedHash;
        await using (var first = ApiFactory.ForDatabase(name, ownsDatabase: false))
        {
            changedHash = await WithDbAsync(first, async db =>
            {
                var admin = await db.Users.SingleAsync();
                admin.ChangePassword("changed-hash-marker", DateTimeOffset.UtcNow);
                await db.SaveChangesAsync();
                return admin.PasswordHash;
            });
        }

        await using var second = ApiFactory.ForDatabase(name, ownsDatabase: true);
        var user = await WithDbAsync(second, db => db.Users.SingleAsync());

        Assert.Equal(changedHash, user.PasswordHash);
        Assert.False(user.MustChangePassword);
    }

    [Fact]
    public async Task Bootstrap_AdminBlocked_RestoresAndUnlocks()
    {
        var name = NewDbName();
        await using (var first = ApiFactory.ForDatabase(name, ownsDatabase: false))
        {
            await WithDbAsync(first, async db =>
            {
                var admin = await db.Users.SingleAsync();
                admin.ChangePassword("changed-hash-marker", DateTimeOffset.UtcNow);
                admin.Block();
                for (var i = 0; i < AppUser.MaxFailedLogins; i++)
                {
                    admin.RegisterFailedLogin(DateTimeOffset.UtcNow);
                }

                await db.SaveChangesAsync();
                return 0;
            });
        }

        await using var second = ApiFactory.ForDatabase(name, ownsDatabase: true);
        var user = await WithDbAsync(second, db => db.Users.SingleAsync());

        Assert.False(user.IsBlocked);
        Assert.False(user.IsLockedOut(DateTimeOffset.UtcNow));
        Assert.True(user.MustChangePassword);
        Assert.Equal(UserRole.Admin, user.Role);
        Assert.NotEqual("changed-hash-marker", user.PasswordHash);
    }

    [Fact]
    public async Task Bootstrap_NoAdminAndNoPassword_AppStartsAndLogsError()
    {
        await using var factory = new NoPasswordFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(await WithDbAsync(factory, db => db.Users.ToListAsync()));
    }
}
