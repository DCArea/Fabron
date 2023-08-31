using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Fabron.Providers.PostgreSQL;

internal static class ThrowHelper
{
    [StackTraceHidden]
    [DoesNotReturn]
    internal static T NoItemWasUpdated<T>(string? expect) => throw new FabronPostgreSQLProviderException("no item was updated, etag: {expect}");
}
