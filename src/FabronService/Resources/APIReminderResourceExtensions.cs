// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Fabron.Contracts;

using FabronService.Commands;

namespace FabronService.Resources
{
    public static class APIReminderResourceExtensions
    {
        public static APIReminderResource ToResource(this Job<RequestWebAPI, int> job, string reminderName) => new APIReminderResource(
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
