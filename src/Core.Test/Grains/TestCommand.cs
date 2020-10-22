using System.Text.Json;
using TGH.Contracts;
using TGH.Grains;

namespace TGH.Test.Grains
{
    public record TestCommand
    (
        string Foo
    ) : ICommand<int>;

    public static class CommandExtensions
    {
        public static TGH.Grains.JobCommandInfo ToRaw(this TestCommand cmd)
            => new TGH.Grains.JobCommandInfo(nameof(TestCommand), JsonSerializer.Serialize(cmd));
        // public static TGH.Grains.JobCommandInfo ToRaw(this TestCommand cmd)
        //     => new TGH.Grains.JobCommandInfo(nameof(TestCommand), JsonSerializer.Serialize(cmd));
    }
}
