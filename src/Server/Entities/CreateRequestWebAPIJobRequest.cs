using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace TGH.Server.Entities
{
    public record CreateRequestWebAPIJobRequest(
        Guid RequestId,
        DateTime? ScheduledAt,
        RequestWebAPI Command);

    public record GenericCommand(string name, string data);

    public record CreateJobRequest(
        string name,
        JsonElement data
    );

    public record BatchCreateRequestWebAPIJobRequest(
        Guid RequestId,
        List<RequestWebAPI> Commands);

    public record CreateRequestWebAPICronJobRequest(
        Guid RequestId,
        [Required]
        string CronExp,
        RequestWebAPI Command);

}
