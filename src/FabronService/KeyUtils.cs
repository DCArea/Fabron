namespace FabronService
{
    public static class KeyUtils
    {
        public static string BuildTimerKey(string tenant, string key)
            => $"{tenant}/{key}";
    }
}
