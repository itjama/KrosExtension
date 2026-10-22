using KrosExtension.Domain.Common;

namespace KrosExtension.Domain.Users;

public sealed class AppUser : Entity
{
    public const int MaxFailedLogins = 5;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private AppUser()
    {
        UserName = string.Empty;
        NormalizedUserName = string.Empty;
        PasswordHash = string.Empty;
        SecurityStamp = string.Empty;
    }

    public AppUser(string userName, string passwordHash, UserRole role, DateTimeOffset now)
    {
        UserName = userName.Trim();
        NormalizedUserName = Normalize(userName);
        PasswordHash = passwordHash;
        Role = role;
        MustChangePassword = true;
        SecurityStamp = NewStamp();
        CreatedAt = now;
    }

    public string UserName { get; private set; }

    public string NormalizedUserName { get; private set; }

    public string PasswordHash { get; private set; }

    public UserRole Role { get; private set; }

    public bool IsBlocked { get; private set; }

    public bool MustChangePassword { get; private set; }

    public string SecurityStamp { get; private set; }

    public int FailedLoginCount { get; private set; }

    public DateTimeOffset? LockoutEndsAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public DateTimeOffset? PasswordChangedAt { get; private set; }

    public static string Normalize(string userName) => userName.Trim().ToUpperInvariant();

    public bool IsLockedOut(DateTimeOffset now) => LockoutEndsAt is { } ends && ends > now;

    public void RegisterFailedLogin(DateTimeOffset now)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= MaxFailedLogins)
        {
            LockoutEndsAt = now + LockoutDuration;
            FailedLoginCount = 0;
        }
    }

    public void RegisterSuccessfulLogin(DateTimeOffset now)
    {
        FailedLoginCount = 0;
        LockoutEndsAt = null;
        LastLoginAt = now;
    }

    public void SetPasswordHash(string hash) => PasswordHash = hash;

    public void ChangePassword(string newHash, DateTimeOffset now)
    {
        PasswordHash = newHash;
        MustChangePassword = false;
        PasswordChangedAt = now;
        SecurityStamp = NewStamp();
    }

    public void ResetPassword(string temporaryHash, DateTimeOffset now)
    {
        PasswordHash = temporaryHash;
        MustChangePassword = true;
        PasswordChangedAt = now;
        FailedLoginCount = 0;
        LockoutEndsAt = null;
        SecurityStamp = NewStamp();
    }

    public void Block()
    {
        IsBlocked = true;
        SecurityStamp = NewStamp();
    }

    public void Unblock() => IsBlocked = false;

    public void ChangeRole(UserRole role)
    {
        if (Role == role)
        {
            return;
        }

        Role = role;
        SecurityStamp = NewStamp();
    }

    public void RestoreAsBootstrapAdmin(string temporaryHash, DateTimeOffset now)
    {
        Unblock();
        Role = UserRole.Admin;
        ResetPassword(temporaryHash, now);
    }

    private static string NewStamp() => Guid.NewGuid().ToString("N");
}
