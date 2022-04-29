using System;
using Fabron.Mando;
using Fabron.Models;

namespace Fabron.Contracts;

public record CronJob<TCommand>
(
    ObjectMetadata Metadata,
    CronJobSpec<TCommand> Spec,
    CronJobStatus Status
) where TCommand : ICommand;

public record CronJobSpec<TCommand>(
    CommandSpec<TCommand> Command,
    string Schedule,
    DateTimeOffset? NotBefore,
    DateTimeOffset? ExpirationTime,
    bool Suspend
) where TCommand : ICommand;
