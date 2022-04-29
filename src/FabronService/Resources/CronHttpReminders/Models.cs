
using Fabron.Contracts;
using FabronService.Commands;

namespace FabronService.Resources.CronHttpReminders.Models;

public record CronHttpReminder
(
    string Name,
    string Schedule,
    RequestWebAPI Command
);


public static class HttpReminderExtensions
{
    public static CronHttpReminder ToResource(this CronJob<RequestWebAPI> cronJob)
        => new(
            cronJob.Metadata.Name,
            cronJob.Spec.Schedule,
            cronJob.Spec.Command.Data
        );
}
