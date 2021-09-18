using System.Threading;
using System.Threading.Tasks;
using Fabron.Mando;

namespace Fabron.TestRunner.Commands
{
    public record NoopCommandResult();
    public record NoopCommand() : ICommand<NoopCommandResult>;
    public class NoopCommandHandler : ICommandHandler<NoopCommand, NoopCommandResult>
    {
        public Task<NoopCommandResult> Handle(NoopCommand command, CancellationToken token) => Task.FromResult(new NoopCommandResult());
    }
}
