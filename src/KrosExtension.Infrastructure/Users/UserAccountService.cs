using KrosExtension.Application.Users;
using KrosExtension.Domain.Users;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KrosExtension.Infrastructure.Users;

public sealed class UserAccountService(
    ApplicationDbContext db,
    IPasswordHasher<AppUser> hasher,
    TimeProvider timeProvider) : IUserAccountService
{
    // Hash verified when the user does not exist, so unknown and known users take comparable time.
    private static string? s_dummyHash;

    public async Task<LoginResult> LoginAsync(string userName, string password, CancellationToken ct = default)
    {
        userName ??= string.Empty;
        password ??= string.Empty;

        var normalized = AppUser.Normalize(userName);
        var user = await db.Users.FirstOrDefaultAsync(item => item.NormalizedUserName == normalized, ct);

        if (user is null)
        {
            var dummyUser = new AppUser("dummy", string.Empty, UserRole.User, DateTimeOffset.UnixEpoch);
            s_dummyHash ??= hasher.HashPassword(dummyUser, Guid.NewGuid().ToString("N"));
            hasher.VerifyHashedPassword(dummyUser, s_dummyHash, password);
            return new LoginResult(LoginStatus.InvalidCredentials, null);
        }

        var verification = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        var passwordOk = verification != PasswordVerificationResult.Failed;

        if (user.IsBlocked)
        {
            return new LoginResult(LoginStatus.InvalidCredentials, null);
        }

        var now = timeProvider.GetUtcNow();
        if (user.IsLockedOut(now))
        {
            // Failures while locked are not counted, so the lockout window is not extended.
            return new LoginResult(passwordOk ? LoginStatus.LockedOut : LoginStatus.InvalidCredentials, null);
        }

        if (!passwordOk)
        {
            user.RegisterFailedLogin(now);
            await db.SaveChangesAsync(ct);
            return new LoginResult(LoginStatus.InvalidCredentials, null);
        }

        user.RegisterSuccessfulLogin(now);
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.SetPasswordHash(hasher.HashPassword(user, password));
        }

        await db.SaveChangesAsync(ct);
        return new LoginResult(LoginStatus.Success, user);
    }

    public async Task<(PasswordChangeStatus Status, AppUser? User)> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default)
    {
        currentPassword ??= string.Empty;
        newPassword ??= string.Empty;

        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == userId, ct);
        if (user is null
            || hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
        {
            return (PasswordChangeStatus.WrongCurrentPassword, null);
        }

        if (!PasswordPolicy.IsLongEnough(newPassword))
        {
            return (PasswordChangeStatus.TooShort, null);
        }

        if (string.Equals(newPassword, currentPassword, StringComparison.Ordinal))
        {
            return (PasswordChangeStatus.SameAsCurrent, null);
        }

        user.ChangePassword(hasher.HashPassword(user, newPassword), timeProvider.GetUtcNow());
        await db.SaveChangesAsync(ct);
        return (PasswordChangeStatus.Success, user);
    }

    public Task<AppUser?> FindByIdAsync(Guid userId, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(item => item.Id == userId, ct);
}
