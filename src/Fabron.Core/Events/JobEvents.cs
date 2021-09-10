using System;
using System.Collections.Generic;

namespace Fabron.Events
{
    public record JobScheduled(
        Dictionary<string, string> Labels,
        Dictionary<string, string> Annotations,
        DateTime Schedule,
        string CommandName,
        string CommandData) : IJobEvent;

    public record JobExecutionStarted() : IJobEvent;

    public record JobExecutionSucceed(string Result) : IJobEvent;

    public record JobExecutionFailed(string Reason) : IJobEvent;

}
