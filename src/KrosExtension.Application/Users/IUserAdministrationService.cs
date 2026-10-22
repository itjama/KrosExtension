using KrosExtension.Domain.Users;

namespace KrosExtension.Application.Users;

public interface IUserAdministrationService
{
    Task<IReadOnlyList<AppUser>> GetUsersAsync(CancellationToken ct = default);

    Task<UserAdminResult> CreateUserAsync(string userName, UserRole role, CancellationToken ct = default);

    Task<UserAdminResult> ResetPasswordAsync(Guid actingUserId, Guid userId, CancellationToken ct = default);

    Task<UserAdminResult> BlockAsync(Guid actingUserId, Guid userId, CancellationToken ct = default);

    Task<UserAdminResult> UnblockAsync(Guid userId, CancellationToken ct = default);

    Task<UserAdminResult> ChangeRoleAsync(Guid actingUserId, Guid userId, UserRole role, CancellationToken ct = default);
}
