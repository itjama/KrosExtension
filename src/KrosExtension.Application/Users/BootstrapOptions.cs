namespace KrosExtension.Application.Users;

public sealed class BootstrapOptions
{
    public const string SectionName = "Bootstrap";

    public string AdminUserName { get; set; } = "admin";

    public string AdminPassword { get; set; } = string.Empty;
}
