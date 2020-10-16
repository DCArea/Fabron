using System.Collections.Generic;
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
        Task<TResult> Handle(TCommand command, CancellationToken token);
    }

}
