using System.Net;
using System.Net.Http.Json;

namespace KrosExtension.Api.Tests;

public class RateLimitTests(RateLimitTests.LimitedFactory factory) : IClassFixture<RateLimitTests.LimitedFactory>
{
    public sealed class LimitedFactory : ApiFactory
    {
        public override void ConfigureSettings(IDictionary<string, string?> settings) =>
            settings["Auth:LoginRateLimitPerMinute"] = "2";
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string url, string ip, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-Forwarded-For", ip);
        return client.SendAsync(request, Ct);
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string ip) =>
        PostAsync(client, "/api/auth/login", ip, new { userName = "nobody", password = "wrong-password" });

    [Fact]
    public async Task LoginRateLimit_ExceededFromSameIp_Returns429()
    {
        using var client = factory.CreateCookieClient();

        Assert.NotEqual(HttpStatusCode.TooManyRequests, (await LoginAsync(client, "10.0.0.5")).StatusCode);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, (await LoginAsync(client, "10.0.0.5")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await LoginAsync(client, "10.0.0.5")).StatusCode);
    }

    [Fact]
    public async Task LoginRateLimit_IsPartitionedByForwardedClientIp()
    {
        using var client = factory.CreateCookieClient();

        await LoginAsync(client, "10.0.1.5");
        await LoginAsync(client, "10.0.1.5");
        var other = await LoginAsync(client, "10.0.1.6");

        Assert.NotEqual(HttpStatusCode.TooManyRequests, other.StatusCode);
    }

    [Fact]
    public async Task ChangePasswordRateLimit_ExceededFromSameIp_Returns429()
    {
        using var client = factory.CreateCookieClient();
        var body = new { currentPassword = "x", newPassword = "y" };

        // Unauthenticated calls are rejected by authorization (401) but still consume permits.
        await PostAsync(client, "/api/auth/change-password", "10.0.2.5", body);
        await PostAsync(client, "/api/auth/change-password", "10.0.2.5", body);
        var third = await PostAsync(client, "/api/auth/change-password", "10.0.2.5", body);

        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }
}
