using System;
using System.Diagnostics;
using Orleans.Runtime;

namespace Orleans
{
    public static class ClientBuilderExtensions
    {
        /// <summary>
        /// Add <see cref="Activity.Current"/> propagation through grain calls.
        /// Note: according to <see cref="ActivitySource.StartActivity(string, ActivityKind)"/> activity will be created only when any listener for activity exists <see cref="ActivitySource.HasListeners()"/> and <see cref="ActivityListener.Sample"/> returns <see cref="ActivitySamplingResult.PropagationData"/>.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns>The builder.</returns>
        public static IClientBuilder AddActivityPropagation(this IClientBuilder builder)
        {
            if (Activity.DefaultIdFormat != ActivityIdFormat.W3C)
            {
                throw new InvalidOperationException("Activity propagation available only for Activities in W3C format. Set Activity.DefaultIdFormat into ActivityIdFormat.W3C.");
            }

            return builder
                .AddOutgoingGrainCallFilter<ActivityPropagationOutgoingGrainCallFilter>();
        }
    }
}
