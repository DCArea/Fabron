
using Fabron;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{

    public static class FabronServiceCollectionExtensions
    {
        public static IServiceCollection AddFabronCore(this IServiceCollection services)
        {
            services.TryAddSingleton<IJobManager, JobManager>();
            services.AddJobQuerier<NoopJobQuerier>();
            return services;
        }

        public static IServiceCollection AddJobReporter<TJobReporter>(this IServiceCollection services)
            where TJobReporter : class, IJobReporter
        {
            services.AddSingleton<IJobReporter, TJobReporter>();
            return services;
        }
        public static IServiceCollection AddJobQuerier<TJobQuerier>(this IServiceCollection services)
            where TJobQuerier : class, IJobQuerier
        {
            services.AddSingleton<IJobQuerier, TJobQuerier>();
            return services;
        }

    }
}
