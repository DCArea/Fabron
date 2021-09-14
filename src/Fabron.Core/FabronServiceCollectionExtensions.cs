
using Fabron;
using Fabron.Indexer;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class FabronServiceCollectionExtensions
    {
        public static IServiceCollection AddFabron(this IServiceCollection services)
        {
            services.TryAddSingleton<IJobManager, JobManager>();
            return services;
        }

        public static IServiceCollection UseJobQuerier<TJobQuerier>(this IServiceCollection services)
            where TJobQuerier : class, IJobQuerier
        {
            services.AddSingleton<IJobQuerier, TJobQuerier>();
            return services;
        }

        public static IServiceCollection UseInMemoryJobQuerier(this IServiceCollection services)
        {
            services.UseJobQuerier<GrainBasedInMemoryJobIndexer>();
            return services;
        }
    }
}
