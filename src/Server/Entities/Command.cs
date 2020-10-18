using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TGH.Server.Entities
{
    public interface ICommand
    { }

    public interface ICommand<out TResult> : ICommand
    {

    }

    public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
    {
        Task<string> Handle(string data, CancellationToken token);
        Task<TResult> Handle(TCommand command, CancellationToken token);
    }

    public abstract class CommandHandler<TCommand, TResult>: ICommandHandler<TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
        public async Task<string> Handle(string data, CancellationToken token)
        {
            var typedcommand = JsonSerializer.Deserialize<TCommand>(data);
            var result = await Handle(typedcommand!, token);
            return JsonSerializer.Serialize<TResult>(result);
        }

        public abstract Task<TResult> Handle(TCommand command, CancellationToken token);
    }

}
