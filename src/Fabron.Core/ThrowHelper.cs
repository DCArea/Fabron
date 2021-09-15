using System;

namespace Fabron
{
    public static partial class ThrowHelper
    {
        public static void ThrowConsumerNotFollowedUp(string entityKey, long expect, long current)
            => throw new InvalidOperationException($"Entity({entityKey}) events are not consumed, expect offset: {expect}, current offset: {current}");

        public static T ThrowInvalidEventName<T>(string entityKey, long version, string name)
            => throw new InvalidOperationException($"Event({name}) is not supported for {typeof(T).Name}[{entityKey}:{version}]");

        //public static T ThrowInvalidEventPayload<T>(string entityKey, string version, string name)
        //    => throw new InvalidOperationException($"Event({name}) payload is invalid");
    }
}
