using KrosExtension.Domain.Users;

namespace KrosExtension.Application.Users;

public enum LoginStatus
{
    Success,
    InvalidCredentials,
    LockedOut,
}

public sealed record LoginResult(LoginStatus Status, AppUser? User);

public enum PasswordChangeStatus
{
    Success,
    WrongCurrentPassword,
    TooShort,
    SameAsCurrent,
}
