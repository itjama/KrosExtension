using KrosExtension.Domain.Users;

namespace KrosExtension.Application.Users;

public enum UserAdminError
{
    None,
    NotFound,
    EmptyUserName,
    DuplicateUserName,
    CannotModifySelf,
    LastActiveAdmin,
}

public sealed record UserAdminResult(UserAdminError Error, AppUser? User = null, string? TemporaryPassword = null);
