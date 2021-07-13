// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

using Fabron.Grains;
using Fabron.Mando;

namespace Fabron.Test.Grains
{
    public record TestCommand
    (
        string Foo
    ) : ICommand<int>;

    public static class CommandExtensions
    {
        public static JobCommandInfo ToRaw(this TestCommand cmd)
            => new(nameof(TestCommand), JsonSerializer.Serialize(cmd));
        // public static Fabron.Grains.JobCommandInfo ToRaw(this TestCommand cmd)
        //     => new Fabron.Grains.JobCommandInfo(nameof(TestCommand), JsonSerializer.Serialize(cmd));
    }
}
