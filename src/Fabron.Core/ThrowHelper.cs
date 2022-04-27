using System;
using System.Diagnostics.CodeAnalysis;
using Fabron.Models;

namespace Fabron
{
    public static partial class ThrowHelper
    {
        public static void ThrowConsumerNotFollowedUp(string entityKey, long expect, long current)
            => throw new InvalidOperationException($"Entity({entityKey}) events are not consumed, expect offset: {expect}, current offset: {current}");

        public static T ThrowInvalidEventName<T>(string entityKey, long version, string name)
            => throw new InvalidOperationException($"Event({name}) is not supported for {typeof(T).Name}[{entityKey}:{version}]");

        public static void ThrowStartCompletedCronJob(string key)
            => throw new InvalidOperationException($"Can not start a completed cron job");

        public static InvalidOperationException CreateInvalidJobExecutionState(ExecutionStatus status)
            => new($"Job state is invalid {status}");
    }
}
