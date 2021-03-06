using System.Reflection;
using Fabron;
using Fabron.Grains.TransientJob;
using Fabron.Mando;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Orleans.Hosting
{
    public static class FabronSiloBuilderExtensions
    {
        public static ISiloBuilder AddFabron(this ISiloBuilder siloBuilder, Assembly commandAssembly)
        {
            siloBuilder
                .ConfigureServices((ctx, services) =>
                {
                    services.AddFabronCore();
                });
            siloBuilder
                .ConfigureApplicationParts(manager =>
                    manager.AddApplicationPart(typeof(TransientJobGrain).Assembly).WithReferences());

            siloBuilder.ConfigureServices((ctx, services) =>
            {
                services.RegisterJobCommandHandlers(commandAssembly);
            });

            return siloBuilder;
        }

        public static ISiloBuilder UseInMemoryJobStore(this ISiloBuilder siloBuilder)
        {
            siloBuilder
                .UseInMemoryReminderService()
                .AddMemoryGrainStorage("JobStore");
            return siloBuilder;
        }
    }
}
