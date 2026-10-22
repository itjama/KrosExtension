using KrosExtension.Api.Auth;
using KrosExtension.Api.Endpoints;
using KrosExtension.Api.Services;
using KrosExtension.Application;
using KrosExtension.Application.Configuration;
using KrosExtension.Application.Users;
using KrosExtension.Infrastructure;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;



var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "Kros Extension Backend";
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAppAuthentication(builder.Configuration);
builder.Services.AddHostedService<DataRefreshBackgroundService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>()
        .Database.Migrate();

    await scope.ServiceProvider
        .GetRequiredService<IAdminBootstrapper>()
        .RunAsync();

    var secretMigrator = scope.ServiceProvider.GetRequiredService<IDataReadServerSecretMigrator>();
    var encryptedServers = await secretMigrator.EncryptPlaintextSecretsAsync();

    if (encryptedServers > 0)
    {
        app.Logger.LogInformation("Encrypted SQL passwords of {Count} data read server(s).", encryptedServers);
    }

    foreach (var secret in await secretMigrator.FindUndecryptableSecretsAsync())
    {
        // Only the server id and column are logged, never the stored value.
        app.Logger.LogWarning(
            "Stored SQL password {Column} of data read server {ServerId} cannot be decrypted with the current data protection keys; it is treated as empty. Delete and re-add the server with the password.",
            secret.Column,
            secret.ServerId);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAppAuthentication();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapAuthEndpoints();
app.MapAdminUserEndpoints();
app.MapDatabaseEndpoints();
app.MapDataReadServerEndpoints();
app.MapCompanyEndpoints();
app.MapProductCompanyEndpoints();
app.MapSystemEndpoints();

app.Run();

public partial class Program;
