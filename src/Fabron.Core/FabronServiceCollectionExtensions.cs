// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Fabron;
using Fabron.Models.Events;
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

        public static IServiceCollection AddDefaultEventBus(this IServiceCollection services)
        {
            services.AddSingleton<IJobEventBus, DefaultJobEventBus>();
            services.AddTransient<IJobEventHandler<JobStateChanged>, DefaultJobStateChangedHandler>();
            services.AddTransient<IJobEventHandler<CronJobStateChanged>, DefaultJobStateChangedHandler>();
            services.AddTransient<IJobEventHandler<JobExecutionFailed>, DefaultJobStateChangedHandler>();
            return services;
        }

    }
}
