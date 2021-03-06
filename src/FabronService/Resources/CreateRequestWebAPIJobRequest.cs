using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FabronService.Commands;

namespace FabronService.Resources
{
    public record CreateRequestWebAPIJobRequest(
        string RequestId,
        DateTime? ScheduledAt,
        RequestWebAPI Command);

    public record BatchCreateRequestWebAPIJobRequest(
        string RequestId,
        List<RequestWebAPI> Commands);

    public record CreateRequestWebAPICronJobRequest(
        string RequestId,
        [Required]
        string CronExp,
        RequestWebAPI Command);

}
