using System.Collections.Generic;
using System.Reflection;

using Fabron;
using Fabron.Mando;
using Microsoft.Extensions.DependencyInjection;

namespace Orleans.Hosting
{
    public static class FabronSiloBuilderExtensions
    {
        public static ISiloBuilder AddFabron(this ISiloBuilder siloBuilder, IEnumerable<Assembly>? commandAssemblies = null)
        {
            siloBuilder
                .ConfigureServices((ctx, services) =>
                {
                    services.AddScoped<IMediator, Mediator>()
                        .RegisterJobCommandHandlers(commandAssemblies)
                        .AddJobReporter<NoopJobReporter>()
                        .AddDefaultEventBus();
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
