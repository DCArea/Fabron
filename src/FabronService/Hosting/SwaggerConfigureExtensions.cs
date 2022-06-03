using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace FabronService.Hosting;

public static class SwaggerConfigureExtensions
{
    public static WebApplicationBuilder ConfigureSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "FabronService", Version = "v1" });
        });

        return builder;
    }

    public static WebApplication UseConfiguredSwagger(this WebApplication app)
    {
        app.UseSwagger()
            .UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FabronService v1"));
        return app;
    }
}
