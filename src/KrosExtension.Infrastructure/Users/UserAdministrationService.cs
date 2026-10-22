using KrosExtension.Application.Users;
using KrosExtension.Domain.Users;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KrosExtension.Infrastructure.Users;

public sealed class UserAdministrationService(
    ApplicationDbContext db,
    IPasswordHasher<AppUser> hasher,
    TimeProvider timeProvider) : IUserAdministrationService
{
    public async Task<IReadOnlyList<AppUser>> GetUsersAsync(CancellationToken ct = default) =>
        await db.Users.AsNoTracking().OrderBy(user => user.UserName).ToListAsync(ct);

    public async Task<UserAdminResult> CreateUserAsync(string userName, UserRole role, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return new UserAdminResult(UserAdminError.EmptyUserName);
        }

        var normalized = AppUser.Normalize(userName);
        if (await db.Users.AnyAsync(user => user.NormalizedUserName == normalized, ct))
        {
            return new UserAdminResult(UserAdminError.DuplicateUserName);
        }

        var now = timeProvider.GetUtcNow();
        var user = new AppUser(userName, string.Empty, role, now);
        var temporaryPassword = TemporaryPasswordGenerator.Generate();
        user.SetPasswordHash(hasher.HashPassword(user, temporaryPassword));

        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // A concurrent request won the race on the unique NormalizedUserName index.
            db.Entry(user).State = EntityState.Detached;
            return new UserAdminResult(UserAdminError.DuplicateUserName);
        }

        return new UserAdminResult(UserAdminError.None, user, temporaryPassword);
    }

    public async Task<UserAdminResult> ResetPasswordAsync(Guid actingUserId, Guid userId, CancellationToken ct = default)
    {
        if (actingUserId == userId)
        {
            // Resetting your own password rotates your security stamp and ends your session
            // before you can see the temporary password; use "Zmeniť heslo" instead.
            return new UserAdminResult(UserAdminError.CannotModifySelf);
        }

        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == userId, ct);
        if (user is null)
        {
            return new UserAdminResult(UserAdminError.NotFound);
        }

        var temporaryPassword = TemporaryPasswordGenerator.Generate();
        user.ResetPassword(hasher.HashPassword(user, temporaryPassword), timeProvider.GetUtcNow());
        await db.SaveChangesAsync(ct);
        return new UserAdminResult(UserAdminError.None, user, temporaryPassword);
    }

    public async Task<UserAdminResult> BlockAsync(Guid actingUserId, Guid userId, CancellationToken ct = default)
    {
        if (actingUserId == userId)
        {
            return new UserAdminResult(UserAdminError.CannotModifySelf);
        }

        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == userId, ct);
        if (user is null)
        {
            return new UserAdminResult(UserAdminError.NotFound);
        }

        if (user.Role == UserRole.Admin && !user.IsBlocked && await IsLastActiveAdminAsync(ct))
        {
            return new UserAdminResult(UserAdminError.LastActiveAdmin);
        }

        user.Block();
        await db.SaveChangesAsync(ct);
        return new UserAdminResult(UserAdminError.None, user);
    }

    public async Task<UserAdminResult> UnblockAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == userId, ct);
        if (user is null)
        {
            return new UserAdminResult(UserAdminError.NotFound);
        }

        user.Unblock();
        await db.SaveChangesAsync(ct);
        return new UserAdminResult(UserAdminError.None, user);
    }

    public async Task<UserAdminResult> ChangeRoleAsync(
        Guid actingUserId,
        Guid userId,
        UserRole role,
        CancellationToken ct = default)
    {
        if (actingUserId == userId)
        {
            return new UserAdminResult(UserAdminError.CannotModifySelf);
        }

        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == userId, ct);
        if (user is null)
        {
            return new UserAdminResult(UserAdminError.NotFound);
        }

        if (role != UserRole.Admin
            && user.Role == UserRole.Admin
            && !user.IsBlocked
            && await IsLastActiveAdminAsync(ct))
        {
            return new UserAdminResult(UserAdminError.LastActiveAdmin);
        }

        user.ChangeRole(role);
        await db.SaveChangesAsync(ct);
        return new UserAdminResult(UserAdminError.None, user);
    }

    private async Task<bool> IsLastActiveAdminAsync(CancellationToken ct) =>
        await db.Users.CountAsync(user => user.Role == UserRole.Admin && !user.IsBlocked, ct) <= 1;
}
