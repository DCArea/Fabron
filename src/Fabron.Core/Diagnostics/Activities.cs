using System.Diagnostics;

namespace Fabron.Diagnostics;

public static class Activities
{
    internal const string ActivitySourceName = "Fabron";
    internal static ActivitySource Source { get; } = new(ActivitySourceName);
}
