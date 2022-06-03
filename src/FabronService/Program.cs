using FabronService.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

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


#pragma warning disable CA1050 // Declare types in namespaces
public partial class Program { }
#pragma warning restore CA1050 // Declare types in namespaces
