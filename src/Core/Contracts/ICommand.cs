using System.Text.Json;

namespace TGH.Contracts
{
    public interface ICommand
    { }

    public interface ICommand<out TResult> : ICommand
    { }
}
