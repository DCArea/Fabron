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



    public record DelayCommandResult();
    public record DelayCommand(int Delay) : ICommand<DelayCommandResult>;
    public class DelayCommandHandler : ICommandHandler<DelayCommand, DelayCommandResult>
    {
        public async Task<DelayCommandResult> Handle(DelayCommand command, CancellationToken token)
        {
            await Task.Delay(command.Delay, token);
            return new();
        }
    }

}

