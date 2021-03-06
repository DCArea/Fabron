using System;
using Fabron.Contracts;
using FabronService.Commands;

namespace FabronService.Resources
{
    public record APIReminderResource
    (
        string Name,
        JobCommand<RequestWebAPI, int> Command,
        DateTime CreatedAt,
        DateTime Schedule,
        DateTime? StartedAt,
        DateTime? FinishedAt,
        JobStatus Status,
        string? Reason
    );

    public record CreateAPIReminderResourceRequest
    (
        string Name,
        DateTime Schedule,
        RequestWebAPI Command
    );
}
