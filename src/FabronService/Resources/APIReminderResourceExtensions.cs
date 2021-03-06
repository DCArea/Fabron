using Fabron.Contracts;
using FabronService.Commands;

namespace FabronService.Resources
{
    public static class APIReminderResourceExtensions
    {
        public static APIReminderResource ToResource(this TransientJob<RequestWebAPI, int> job, string reminderName)
        {
            return new APIReminderResource(
                reminderName,
                job.Command,
                job.CreatedAt,
                job.ScheduledAt!.Value,
                job.StartedAt,
                job.FinishedAt,
                job.Status,
                job.Reason);
        }
    }
}
