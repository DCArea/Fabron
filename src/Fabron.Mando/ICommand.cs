
namespace Fabron.Mando
{
    public interface ICommand
    { }

    public interface ICommand<out TResult> : ICommand
    { }
}
