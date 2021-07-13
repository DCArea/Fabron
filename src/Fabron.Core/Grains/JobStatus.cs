// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Fabron.Grains
{
    public enum JobStatus
    {
        NotCreated,
        Created,
        Running,
        RanToCompletion,
        Canceled,
        Faulted
    }
}
