using System.Diagnostics;

namespace Fabron.Diagnostics;

public static class Activities
{
    public const string ActivitySourceName = "Fabron";
    internal static ActivitySource Source { get; } = new(ActivitySourceName);
}
