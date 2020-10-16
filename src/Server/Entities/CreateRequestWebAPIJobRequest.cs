using System;

namespace TGH.Server.Entities
{
    public record CreateRequestWebAPIJobRequest(
        Guid RequestId,
        RequestWebAPICommand Command);

}
