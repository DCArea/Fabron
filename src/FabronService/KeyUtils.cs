namespace FabronService
{
    public static class KeyUtils
    {
        public static string BuildTimedEventKey(string tenant, string key)
            => $"fabron.io/timedevents/{tenant}/{key}";

        public static string BuildCronEventKey(string tenant, string key)
            => $"fabron.io/cronevents/{tenant}/{key}";

        public static string BuildPeriodicEventKey(string tenant, string key)
            => $"fabron.io/periodicevents/{tenant}/{key}";
    }
}
