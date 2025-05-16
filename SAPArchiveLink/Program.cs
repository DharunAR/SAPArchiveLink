using SAPArchiveLink;

var builder = WebApplication.CreateBuilder(args);

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
