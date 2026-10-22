using KrosExtension.Application.Users;
using KrosExtension.Domain.Users;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KrosExtension.Infrastructure.Users;

public sealed class AdminBootstrapper(
    ApplicationDbContext db,
    IPasswordHasher<AppUser> hasher,
    IOptions<BootstrapOptions> options,
    TimeProvider timeProvider,
    ILogger<AdminBootstrapper> logger) : IAdminBootstrapper
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var hasActiveAdmin = await db.Users
            .AnyAsync(user => user.Role == UserRole.Admin && !user.IsBlocked, cancellationToken);

        if (hasActiveAdmin)
        {
            if (!string.IsNullOrEmpty(settings.AdminPassword))
            {
                logger.LogWarning(
                    "Bootstrap:AdminPassword is set but an active admin already exists. Remove it from the configuration.");
            }

            return;
        }

        if (string.IsNullOrEmpty(settings.AdminPassword))
        {
            logger.LogError(
                "No active admin exists and Bootstrap:AdminPassword is empty. Set it in the configuration and restart.");
            return;
        }

        var now = timeProvider.GetUtcNow();
        var normalized = AppUser.Normalize(settings.AdminUserName);
        var existing = await db.Users
            .FirstOrDefaultAsync(user => user.NormalizedUserName == normalized, cancellationToken);

        if (existing is null)
        {
            var user = new AppUser(settings.AdminUserName, string.Empty, UserRole.Admin, now);
            user.SetPasswordHash(hasher.HashPassword(user, settings.AdminPassword));
            db.Users.Add(user);
            logger.LogInformation("Created bootstrap admin '{UserName}'.", user.UserName);
        }
        else
        {
            existing.RestoreAsBootstrapAdmin(hasher.HashPassword(existing, settings.AdminPassword), now);
            logger.LogWarning("Restored bootstrap admin '{UserName}'.", existing.UserName);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
