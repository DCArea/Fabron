using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using FabronService.Commands;

namespace Fabron.Server.Entities
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
