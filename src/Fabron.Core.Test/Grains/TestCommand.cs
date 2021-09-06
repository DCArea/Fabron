
using Fabron.Mando;

namespace Fabron.Test.Grains
{
    public record TestCommand
    (
        string Foo
    ) : ICommand<int>;

}
