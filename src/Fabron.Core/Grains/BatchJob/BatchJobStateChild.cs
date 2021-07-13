// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Fabron.Grains.BatchJob
{
    public class BatchJobStateChild
    {
#nullable disable
        public BatchJobStateChild() { }
#nullable enable
        public BatchJobStateChild(JobCommandInfo command)
        {
            Command = command;
            Id = Guid.NewGuid().ToString();
        }

        public string Id { get; }
        public JobCommandInfo Command { get; }
        public JobStatus Status { get; set; }
        public bool IsPending
            => Status is (JobStatus.Created or JobStatus.Running);
        public bool IsFinished
            => Status is (JobStatus.RanToCompletion or JobStatus.Canceled or JobStatus.Faulted);
    }
}
