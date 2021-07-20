// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Fabron.Grains.BatchJob
{
    public enum ChildJobStatus
    {
        WaitToSchedule,
        Scheduled,
        RanToCompletion,
        Canceled,
        Faulted
    }

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
        public ChildJobStatus Status { get; set; }
        public bool IsFinished
            => Status is (ChildJobStatus.RanToCompletion or ChildJobStatus.Canceled or ChildJobStatus.Faulted);
    }
}
