using Fabron.Models;

namespace Fabron.Dispatching;

internal static class EnvelopeExtensions
{
    public static FireEnvelop ToEnvelop(this Models.PeriodicTimer timer, DateTimeOffset schedule)
    {
        var source = $"periodic.fabron.io/{timer.Metadata.Key}";
        var id = $"{source}/{schedule.ToUnixTimeSeconds()}";
        return new(source, schedule, timer.Data, timer.Metadata.Extensions);
    }

    public static FireEnvelop ToEnvelop(this CronTimer timer, DateTimeOffset schedule)
    {
        var source = $"cron.fabron.io/{timer.Metadata.Key}";
        var id = $"{source}/{schedule.ToUnixTimeSeconds()}";
        return new(source, schedule, timer.Data, timer.Metadata.Extensions);
    }

    public static FireEnvelop ToEnvelop(this GenericTimer timer, DateTimeOffset schedule)
    {
        var source = $"generic.fabron.io/{timer.Metadata.Key}";
        var id = $"{source}/{schedule.ToUnixTimeSeconds()}";
        return new(source, schedule, timer.Data, timer.Metadata.Extensions);
    }
}
