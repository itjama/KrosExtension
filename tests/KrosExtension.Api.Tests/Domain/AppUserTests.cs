using KrosExtension.Domain.Users;

namespace KrosExtension.Api.Tests.Domain;

public class AppUserTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static AppUser NewUser() => new("JNovak", "h", UserRole.User, T0);

    [Fact]
    public void Ctor_TrimsNameAndRequiresPasswordChange()
    {
        var user = new AppUser("  JNovak ", "h", UserRole.User, T0);

        Assert.Equal("JNovak", user.UserName);
        Assert.Equal("JNOVAK", user.NormalizedUserName);
        Assert.True(user.MustChangePassword);
        Assert.False(string.IsNullOrEmpty(user.SecurityStamp));
        Assert.Equal(T0, user.CreatedAt);
    }

    [Fact]
    public void FifthFailedLogin_LocksForFifteenMinutes()
    {
        var user = NewUser();

        for (var i = 0; i < 4; i++)
        {
            user.RegisterFailedLogin(T0);
        }

        Assert.False(user.IsLockedOut(T0));

        user.RegisterFailedLogin(T0);

        Assert.True(user.IsLockedOut(T0));
        Assert.True(user.IsLockedOut(T0.AddMinutes(14)));
        Assert.False(user.IsLockedOut(T0.AddMinutes(15)));
        Assert.Equal(0, user.FailedLoginCount);
    }

    [Fact]
    public void SuccessfulLogin_ClearsFailures()
    {
        var user = NewUser();
        for (var i = 0; i < 3; i++)
        {
            user.RegisterFailedLogin(T0);
        }

        user.RegisterSuccessfulLogin(T0);

        Assert.Equal(0, user.FailedLoginCount);
        Assert.Null(user.LockoutEndsAt);
        Assert.Equal(T0, user.LastLoginAt);
    }

    [Fact]
    public void ChangePassword_ClearsMustChangeAndRotatesStamp()
    {
        var user = NewUser();
        var stamp = user.SecurityStamp;

        user.ChangePassword("new", T0);

        Assert.False(user.MustChangePassword);
        Assert.Equal("new", user.PasswordHash);
        Assert.Equal(T0, user.PasswordChangedAt);
        Assert.NotEqual(stamp, user.SecurityStamp);
    }

    [Fact]
    public void ResetPassword_SetsMustChange_ClearsLockout_RotatesStamp()
    {
        var user = NewUser();
        user.ChangePassword("new", T0);
        for (var i = 0; i < 5; i++)
        {
            user.RegisterFailedLogin(T0);
        }

        var stamp = user.SecurityStamp;

        user.ResetPassword("tmp", T0);

        Assert.True(user.MustChangePassword);
        Assert.Equal("tmp", user.PasswordHash);
        Assert.Null(user.LockoutEndsAt);
        Assert.Equal(0, user.FailedLoginCount);
        Assert.False(user.IsLockedOut(T0));
        Assert.NotEqual(stamp, user.SecurityStamp);
    }

    [Fact]
    public void Block_RotatesStamp()
    {
        var user = NewUser();
        var stamp = user.SecurityStamp;

        user.Block();

        Assert.True(user.IsBlocked);
        Assert.NotEqual(stamp, user.SecurityStamp);
    }

    [Fact]
    public void ChangeRole_SameRole_KeepsStamp()
    {
        var user = NewUser();
        var stamp = user.SecurityStamp;

        user.ChangeRole(UserRole.User);
        Assert.Equal(stamp, user.SecurityStamp);

        user.ChangeRole(UserRole.Admin);
        Assert.Equal(UserRole.Admin, user.Role);
        Assert.NotEqual(stamp, user.SecurityStamp);
    }

    [Fact]
    public void RestoreAsBootstrapAdmin_UnblocksPromotesAndForcesChange()
    {
        var user = NewUser();
        user.ChangePassword("new", T0);
        user.Block();

        user.RestoreAsBootstrapAdmin("tmp", T0);

        Assert.False(user.IsBlocked);
        Assert.Equal(UserRole.Admin, user.Role);
        Assert.True(user.MustChangePassword);
        Assert.Equal("tmp", user.PasswordHash);
    }
}
