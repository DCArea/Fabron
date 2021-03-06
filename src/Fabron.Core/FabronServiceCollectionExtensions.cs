using Fabron;
using Fabron.Mando;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class FabronServiceCollectionExtensions
    {
        public static IServiceCollection AddFabronCore(this IServiceCollection services)
        {
            services.TryAddScoped<IMediator, Mediator>();
            services.TryAddSingleton<IJobManager, JobManager>();
            return services;
        }
    }
}
