// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Orleans.Concurrency;

namespace Fabron.Grains
{
    [Immutable]
    public class JobCommandInfo
    {
        public JobCommandInfo(string name, string data)
        {
            Name = name;
            Data = data;
        }
        public string Name { get; }
        public string Data { get; }
    }
}
