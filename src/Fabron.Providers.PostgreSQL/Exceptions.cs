using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Fabron.Providers.PostgreSQL;

public class FabronPostgreSQLProviderException : Exception
{
    public FabronPostgreSQLProviderException(string? message) : base(message)
    {
    }

    public FabronPostgreSQLProviderException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

internal static class ThrowHelper
{
    [StackTraceHidden]
    [DoesNotReturn]
    internal static void NoItemWasUpdated(string expected) => throw new FabronPostgreSQLProviderException("no item was updated");

    [StackTraceHidden]
    [DoesNotReturn]
    internal static void ETagMismatch(string? expect) => throw new InvalidDataException($"ETag mismatch, expect: {expect}");
}
