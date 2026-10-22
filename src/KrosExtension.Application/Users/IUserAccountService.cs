using KrosExtension.Domain.Users;

namespace KrosExtension.Application.Users;

public interface IUserAccountService
{
    Task<LoginResult> LoginAsync(string userName, string password, CancellationToken ct = default);

    Task<(PasswordChangeStatus Status, AppUser? User)> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default);

    Task<AppUser?> FindByIdAsync(Guid userId, CancellationToken ct = default);
}
