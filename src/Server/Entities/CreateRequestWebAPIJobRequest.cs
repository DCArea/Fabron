using System;
using System.Collections.Generic;
using System.Text.Json;

namespace TGH.Server.Entities
{
    public record CreateRequestWebAPIJobRequest(
        Guid RequestId,
        RequestWebAPI Command);

    public record GenericCommand(string name, string data);

    public record CreateJobRequest(
        string name,
        JsonElement data
    );

    public record BatchCreateRequestWebAPIJobRequest(
        Guid RequestId,
        List<RequestWebAPI> Commands);

}
