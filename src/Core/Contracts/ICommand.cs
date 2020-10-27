using System.Text.Json;

namespace Fabron.Contracts
{
    public interface ICommand
    { }

    public interface ICommand<out TResult> : ICommand
    { }
}
