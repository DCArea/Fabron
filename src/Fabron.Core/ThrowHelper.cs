using System;

namespace Fabron
{
    public static partial class ThrowHelper
    {
        public static void ThrowConsumerNotFollowedUp(string entityId, long expect, long current)
            => throw new InvalidOperationException($"Entity({entityId}) events are not consumed, expect offset: {expect}, current offset: {current}");

        public static T ThrowInvalidEventName<T>(string entityId, long version, string name)
            => throw new InvalidOperationException($"Event({name}) is not supported for {typeof(T).Name}[{entityId}:{version}]");

        //public static T ThrowInvalidEventPayload<T>(string entityId, string version, string name)
        //    => throw new InvalidOperationException($"Event({name}) payload is invalid");
    }
}
