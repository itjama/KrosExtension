using System.Net.Http.Json;
using KrosExtension.Application.Configuration;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KrosExtension.Api.Tests;

public class SecretEncryptionTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static string UniqueServer() => $"srv-{Guid.NewGuid():N}";

    private async Task<string> ReadRawOlympPasswordAsync(string serverName)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Database
            .SqlQuery<string>($"SELECT OlympPassword AS Value FROM DataReadServers WHERE ServerName = {serverName}")
            .SingleAsync(Ct);
    }

    private async Task<string> ReadRawOmegaPasswordAsync(string serverName)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Database
            .SqlQuery<string>($"SELECT OmegaPassword AS Value FROM DataReadServers WHERE ServerName = {serverName}")
            .SingleAsync(Ct);
    }

    private async Task<string> AddServerAsync(string olympPassword)
    {
        var serverName = UniqueServer();
        using var client = await factory.LoginAsAdminAsync(Ct);
        using var response = await client.PostAsJsonAsync("/api/data-read-servers", new
        {
            serverName,
            displayName = serverName,
            olymp = new { authenticationMode = "SqlLogin", userName = "sa", password = olympPassword },
            omega = (object?)null,
            trustServerCertificate = true,
        }, Ct);
        response.EnsureSuccessStatusCode();
        return serverName;
    }

    [Fact]
    public async Task AddServer_StoresPasswordsEncrypted()
    {
        const string password = "Plain-Olymp-Secret-1";
        var serverName = await AddServerAsync(password);

        var raw = await ReadRawOlympPasswordAsync(serverName);

        Assert.StartsWith("enc:", raw);
        Assert.DoesNotContain(password, raw);
    }

    [Fact]
    public async Task EncryptedPassword_RoundTripsThroughEntity()
    {
        const string password = "Round-Trip-Secret-1";
        var serverName = await AddServerAsync(password);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var server = await db.DataReadServers.SingleAsync(s => s.ServerName == serverName, Ct);

        Assert.Equal(password, server.OlympPassword);
    }

    [Fact]
    public async Task UndecryptableSecret_ReadsAsEmpty_AndDoesNotBreakStartupOrEndpoints()
    {
        const string legacy = "mixed-plaintext";
        var serverName = UniqueServer();
        var id = Guid.NewGuid();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO DataReadServers
                    (Id, DisplayName, ServerName, IsActive, TrustServerCertificate,
                     OlympAuthenticationMode, OlympUserName, OlympPassword,
                     OmegaAuthenticationMode, OmegaUserName, OmegaPassword)
                VALUES ({id}, {serverName}, {serverName}, 1, 1, 2, 'sa', 'enc:not-a-valid-payload', 2, 'sa', {legacy})
                """, Ct);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var migrator = scope.ServiceProvider.GetRequiredService<IDataReadServerSecretMigrator>();
            await migrator.EncryptPlaintextSecretsAsync(Ct);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var server = await db.DataReadServers.SingleAsync(s => s.Id == id, Ct);
            Assert.Equal(string.Empty, server.OlympPassword);
            Assert.Equal(legacy, server.OmegaPassword);
        }

        using var client = await factory.LoginAsAdminAsync(Ct);
        using var response = await client.GetAsync("/api/data-read-servers", Ct);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Migrator_EncryptsLegacyPlaintextRows()
    {
        const string secret = "legacy-secret";
        var serverName = UniqueServer();
        var id = Guid.NewGuid();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO DataReadServers
                    (Id, DisplayName, ServerName, IsActive, TrustServerCertificate,
                     OlympAuthenticationMode, OlympUserName, OlympPassword,
                     OmegaAuthenticationMode, OmegaUserName, OmegaPassword)
                VALUES ({id}, {serverName}, {serverName}, 1, 1, 1, '', '', 2, 'sa', {secret})
                """, Ct);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var migrator = scope.ServiceProvider.GetRequiredService<IDataReadServerSecretMigrator>();
            Assert.Equal(1, await migrator.EncryptPlaintextSecretsAsync(Ct));
        }

        var raw = await ReadRawOmegaPasswordAsync(serverName);
        Assert.StartsWith("enc:", raw);
        Assert.DoesNotContain(secret, raw);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var server = await db.DataReadServers.SingleAsync(s => s.Id == id, Ct);
            Assert.Equal(secret, server.OmegaPassword);

            var migrator = scope.ServiceProvider.GetRequiredService<IDataReadServerSecretMigrator>();
            Assert.Equal(0, await migrator.EncryptPlaintextSecretsAsync(Ct));
        }
    }
}
