using Microsoft.OpenApi.Models;

namespace FabronService.Hosting;

public static class SwaggerConfigureExtensions
{
    public static WebApplicationBuilder ConfigureSwagger(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsProduction())
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "FabronService", Version = "v1" });
            });
        }

        return builder;
    }

    public static WebApplication UseConfiguredSwagger(this WebApplication app)
    {
        if (!app.Environment.IsProduction())
        {
            app.MapSwagger().AllowAnonymous();
        }
        return app;
    }
}
