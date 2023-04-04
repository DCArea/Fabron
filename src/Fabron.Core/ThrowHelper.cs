using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Fabron
{
    internal static partial class ThrowHelper
    {
        [StackTraceHidden]
        [DoesNotReturn]
        internal static T ETagMismatch<T>(string? current, string? expect) => throw new InvalidDataException($"ETag mismatch, current: {current}, expect: {expect}");
    }
}
