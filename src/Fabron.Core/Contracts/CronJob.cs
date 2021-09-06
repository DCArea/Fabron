
using System;
using Fabron.Mando;
using Fabron.Models;

namespace Fabron.Contracts
{
    public record TypedCronJobSpec<TCommand>(
        string Schedule,
        string CommandName,
        TCommand CommandData,
        DateTime? NotBefore,
        DateTime? ExpirationTime
    );

    public record CronJob<TCommand>
    (
        CronJobMetadata Metadata,
        TypedCronJobSpec<TCommand> Spec,
        CronJobStatus Status
    ) where TCommand : ICommand;
}
