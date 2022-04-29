using System.Collections.Generic;
using System.Reflection;

using Fabron;
using Fabron.Mando;
using Fabron.Store;
using Microsoft.Extensions.DependencyInjection;

namespace Orleans.Hosting
{
    public static class FabronSiloBuilderExtensions
    {
        public static ISiloBuilder AddFabron(this ISiloBuilder siloBuilder, IEnumerable<Assembly>? commandAssemblies = null)
        {
            siloBuilder
                .ConfigureServices(services =>
                {
                    services.AddScoped<IMediator, Mediator>()
                        .RegisterJobCommandHandlers(commandAssemblies);
                    services.AddSingleton<ISystemClock, SystemClock>();
                });
            return siloBuilder;
        }

        public static ISiloBuilder UseInMemory(this ISiloBuilder siloBuilder)
        {
            siloBuilder
                .UseInMemoryReminderService();
            siloBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IJobStore, InMemoryJobStore>();
                services.AddSingleton<ICronJobStore, InMemoryJobStore>();
            });
            return siloBuilder;
        }

    }
}
