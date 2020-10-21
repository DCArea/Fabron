using System.Threading;
using System.Threading.Tasks;

namespace TGH.Contracts
{
    public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
    {
        Task<TResult> Handle(TCommand command, CancellationToken token);
    }
}
