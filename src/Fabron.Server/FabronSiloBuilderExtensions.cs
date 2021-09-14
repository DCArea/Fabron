using System.Collections.Generic;
using System.Reflection;

using Fabron;
using Fabron.Events;
using Fabron.Indexer;
using Fabron.Mando;
using Fabron.Stores;
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
                        .AddSingleton<IJobEventListener, NoopJobEventListener>()
                        .RegisterJobCommandHandlers(commandAssemblies);
                });
            return siloBuilder;
        }

        public static ISiloBuilder SetEventListener<TJobEventListener, TCronJobEventListener>(this ISiloBuilder siloBuilder)
            where TJobEventListener : class, IJobEventListener
            where TCronJobEventListener : class, ICronJobEventListener =>
            // .
            siloBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IJobEventListener, TJobEventListener>();
                services.AddSingleton<ICronJobEventListener, TCronJobEventListener>();
            });

        public static ISiloBuilder UseInMemory(this ISiloBuilder siloBuilder)
        {
            siloBuilder
                .SetEventListener<LogBasedJobEventListener, LogBasedJobEventListener>()
                .UseEventStore<InMemoryJobEventStore, InMemoryCronJobEventStore>()
                .UseJobIndexer<GrainBasedInMemoryJobIndexer>()
                .UseInMemoryReminderService();
            siloBuilder.ConfigureServices(services =>
            {
                services.UseInMemoryJobQuerier();
                services.AddSingleton<IJobIndexer, GrainBasedInMemoryJobIndexer>();
            });
            return siloBuilder;
        }

        public static ISiloBuilder UseEventStore<TJobEventStore, TCronJobEventStore>(this ISiloBuilder siloBuilder)
            where TJobEventStore : class, IJobEventStore
            where TCronJobEventStore : class, ICronJobEventStore
        {
            siloBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IJobEventStore, TJobEventStore>();
                services.AddSingleton<ICronJobEventStore, TCronJobEventStore>();
            });
            return siloBuilder;
        }

        public static ISiloBuilder UseJobIndexer<TJobIndexer>(this ISiloBuilder siloBuilder) where TJobIndexer : class, IJobIndexer
        {
            siloBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IJobIndexer, TJobIndexer>();
            });
            return siloBuilder;
        }
    }
}
