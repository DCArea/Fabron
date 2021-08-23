// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Fabron.Mando;

namespace Fabron.Test.Grains
{
    public record TestCommand
    (
        string Foo
    ) : ICommand<int>;

}
