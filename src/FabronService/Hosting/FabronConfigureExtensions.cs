using Fabron;
using Fabron.Dispatching;
using Fabron.Providers.PostgreSQL;
using Fabron.Server;
using FabronService.FireRouters;

namespace FabronService.Hosting;

public static class FabronConfigureExtensions
{
    public static WebApplicationBuilder ConfigureFabron(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient();
        builder.Services.Configure<SchedulerOptions>(options => options.CronFormat = Cronos.CronFormat.IncludeSeconds);
        builder.Services.AddSingleton<IHttpDestinationHandler, HttpDestinationHandler>();
        var server = builder.Host.UseFabronServer()
            .AddFireRouter<DefaultFireRouter>();
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
