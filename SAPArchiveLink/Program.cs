using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using NLog;
using NLog.Web;
using SAPArchiveLink;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
builder.Logging.AddNLogWeb("nlog.config");
builder.Host.UseNLog();
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
    });
});
ServiceRegistration.RegisterTrimConfig(builder.Services, builder.Configuration);
ServiceRegistration.RegisterServices(builder.Services);

var app = builder.Build();

var config = app.Services.GetRequiredService<IOptions<TrimConfigSettings>>();
var initState = app.Services.GetRequiredService<TrimInitialization>();
ServiceRegistration.InitializeTrimService(config, initState);

app.UseMiddleware<TrimApplicationMiddleware>();
// Optional: add development exception handling
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Routing & Middleware
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
});

app.UseRouting();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization(); // if you're using auth

app.Use(async (context, next) =>
{
    if (string.IsNullOrWhiteSpace(context.Request.Path) || context.Request.Path == "/")
    {
        var basePath = context.Request.PathBase.HasValue ? context.Request.PathBase.Value : "";
        context.Response.Redirect($"{basePath}/ContentServer", permanent: false);
        return;
    }
    if (!app.Environment.IsDevelopment())
    {
        var clientCert = await context.Connection.GetClientCertificateAsync();
        if (clientCert == null || !clientCert.Verify())
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Invalid client certificate.");
            return;
        }
    }
    await next();
});

app.MapControllers(); // map [ApiController] routes like /ContentServer

try
{
    app.Run();
}
catch (Exception ex)
{
    LogManager.GetCurrentClassLogger().Error(ex, "Application terminated unexpectedly.");
    throw;
}
finally
{
    LogManager.Shutdown();
}
