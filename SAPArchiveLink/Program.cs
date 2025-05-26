using NLog;
using NLog.Web;
using SAPArchiveLink;

//var logger = NLog.LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
builder.Logging.AddNLog("nlog.config");
builder.Host.UseNLog();

ServiceRegistration.RegisterServices(builder.Services);

var app = builder.Build();

// Optional: add development exception handling
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Routing & Middleware
app.UseRouting();

app.UseAuthorization(); // if you're using auth

app.MapControllers(); // map [ApiController] routes like /ContentServer

app.Run();
