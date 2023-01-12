using Fabron;
using Fabron.Events;
using Fabron.Providers.PostgreSQL;
using Fabron.Server;
using FabronService.EventRouters;

namespace FabronService.Hosting;

public static class FabronConfigureExtensions
{
    public static WebApplicationBuilder ConfigureFabron(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient();
        builder.Services.Configure<CronSchedulerOptions>(options => options.CronFormat = Cronos.CronFormat.IncludeSeconds);
        builder.Services.AddSingleton<IEventRouter, AnnotationBasedEventRouter>();
        builder.Services.AddSingleton<IHttpDestinationHandler, HttpDestinationHandler>();
        var server = builder.Host.UseFabronServer();
        var client = builder.Host.UseFabronClient(cohosted: true);

        if (builder.Environment.IsDevelopment())
        {
            server.UseLocalhostClustering()
                .UseInMemory();
        }
        else
        {
            server
                .ConfigureOrleans((ctx, siloBuilder) =>
                {
                    siloBuilder.UseKubernetesHosting();
                    siloBuilder.AddActivityPropagation();
                })
                .UsePostgreSQL(builder.Configuration["PGSQL"]!);
            client.UsePostgreSQL(builder.Configuration["PGSQL"]!);
        }

        return builder;
    }
}
