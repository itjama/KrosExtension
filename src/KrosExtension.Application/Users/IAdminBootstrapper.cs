namespace KrosExtension.Application.Users;

public interface IAdminBootstrapper
{
    Task RunAsync(CancellationToken cancellationToken = default);
}
