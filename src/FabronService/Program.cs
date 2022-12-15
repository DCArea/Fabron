using FabronService.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureFabron();
builder.ConfigureSecurity();
builder.ConfigureOpenTelemetry();
builder.ConfigureSwagger();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.MapHealthChecks("/health").AllowAnonymous();
app.UseOpenTelemetry();
app.UseConfiguredSwagger();
app.UseSecurity();
app.MapRoutes();

app.Run();

public partial class Program { }