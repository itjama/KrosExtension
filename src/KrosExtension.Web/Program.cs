using Microsoft.Extensions.FileProviders;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "Kros Extension Web";
});

var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5184";
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

var app = builder.Build();

var apiMethods = new[] { "GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS" };
app.MapMethods("/api/{**path}", apiMethods, ProxyToApiAsync);
app.MapMethods("/health", apiMethods, ProxyToApiAsync);

var clientDistPath = Path.Combine(app.Environment.ContentRootPath, "ClientApp", "dist");

if (Directory.Exists(clientDistPath))
{
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = new PhysicalFileProvider(clientDistPath)
    });

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(clientDistPath)
    });

    app.MapFallbackToFile("index.html", new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(clientDistPath)
    });
}
else
{
    app.MapGet("/", () => Results.Content(
        "Kros Extension Web frontend build was not found. Run npm install and npm run build in src/KrosExtension.Web/ClientApp.",
        "text/plain"));
}

app.Run();

static async Task ProxyToApiAsync(HttpContext context, IHttpClientFactory httpClientFactory)
{
    var targetPath = context.Request.Path + context.Request.QueryString;
    using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetPath);

    foreach (var header in context.Request.Headers)
    {
        if (string.Equals(header.Key, "Host", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
        {
            request.Content ??= new StreamContent(context.Request.Body);
            request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    if (context.Request.ContentLength > 0 && request.Content is null)
    {
        request.Content = new StreamContent(context.Request.Body);
        if (!string.IsNullOrWhiteSpace(context.Request.ContentType))
        {
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(context.Request.ContentType);
        }
    }

    var client = httpClientFactory.CreateClient("Api");

    try
    {
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

        context.Response.StatusCode = (int)response.StatusCode;

        foreach (var header in response.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in response.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        context.Response.Headers.Remove("transfer-encoding");
        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }
    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
    {
        // Client disconnected — expected, not an error.
    }
}
