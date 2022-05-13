using System;
using System.Diagnostics.CodeAnalysis;

namespace Fabron.Providers.PostgreSQL.Exceptions;

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
    [DoesNotReturn]
    internal static void NoItemWasUpdated()
    {
        throw new FabronPostgreSQLProviderException("no item was updated");
    }

    [DoesNotReturn]
    internal static void ETagMismatch()
    {
        throw new FabronPostgreSQLProviderException("ETag mismatch");
    }
}
