using System;
using System.Diagnostics.CodeAnalysis;

namespace Fabron
{
    public static partial class ThrowHelper
    {
        public static T ThrowInvalidEventName<T>(string entityId, long version, string name)
            => throw new InvalidOperationException($"Event({name}) is not supported for {typeof(T).Name}[{entityId}:{version}]");

        //public static T ThrowInvalidEventPayload<T>(string entityId, string version, string name)
        //    => throw new InvalidOperationException($"Event({name}) payload is invalid");
    }
}
