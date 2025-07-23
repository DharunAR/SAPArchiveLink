using SAPArchiveLink.Resources;
using NLog;
using NLog.Web;
using SAPArchiveLink;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
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

app.UseMiddleware<TrimApplicationMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

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
        var path = context.Request.Path.Value?.ToLower();
        var query = context.Request.Query;
        if (path != null && path.StartsWith("/contentserver") && !context.Request.IsHttps && query.Count > 0)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync(Resource.SecureConnRequired);
            return;
        }
    }
    await next();
});

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

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
