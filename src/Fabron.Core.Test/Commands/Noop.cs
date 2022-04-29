using System.Threading;
using System.Threading.Tasks;
using Fabron.Mando;
using Fabron.Models;

namespace Fabron.Core.Test.Commands;

public record NoopCommandResult();
public record NoopCommand() : ICommand<NoopCommandResult>
{
    public CommandSpec Spec => new()
    {
        Name = nameof(NoopCommand),
        Data = "{}"
    };
};

public class NoopCommandHandler : ICommandHandler<NoopCommand, NoopCommandResult>
{
    public Task<NoopCommandResult> Handle(NoopCommand command, CancellationToken token) => Task.FromResult(new NoopCommandResult());
}
