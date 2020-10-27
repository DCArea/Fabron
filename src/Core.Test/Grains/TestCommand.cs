using System.Text.Json;
using Fabron.Contracts;
using Fabron.Grains;

namespace Fabron.Test.Grains
{
    public record TestCommand
    (
        string Foo
    ) : ICommand<int>;

    public static class CommandExtensions
    {
        public static Fabron.Grains.JobCommandInfo ToRaw(this TestCommand cmd)
            => new Fabron.Grains.JobCommandInfo(nameof(TestCommand), JsonSerializer.Serialize(cmd));
        // public static Fabron.Grains.JobCommandInfo ToRaw(this TestCommand cmd)
        //     => new Fabron.Grains.JobCommandInfo(nameof(TestCommand), JsonSerializer.Serialize(cmd));
    }
}
