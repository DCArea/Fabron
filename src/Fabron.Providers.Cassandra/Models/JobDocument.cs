#nullable disable
using System;
using System.Collections.Generic;

namespace Fabron.Providers.Cassandra.Models
{
    public class JobMetadata
    {
        public string Key { get; set; }
        public string Uid { get; set; }
        public DateTime CreationTimestamp { get; set; }
        public Dictionary<string, string> Labels { get; set; }
        public Dictionary<string, string> Annotations { get; set; }
    }

    public class JobSpec
    {
        public DateTime Schedule { get; set; }
        public string CommandName { get; set; }
        public string CommandData { get; set; }
    }

    public class JobStatus
    {
        public Fabron.Models.ExecutionStatus ExecutionStatus { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime FinishedAt { get; set; }
        public string Result { get; set; }
        public string Reason { get; set; }
        public bool Deleted { get; set; }
    }

    public class JobDocument
    {
        public JobMetadata Metadata { get; set; }
        public JobSpec Spec { get; set; }
        public JobStatus Status { get; set; }
        public long Version { get; set; }
    }

}
