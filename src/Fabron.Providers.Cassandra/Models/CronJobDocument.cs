#nullable disable
using System;
using System.Collections.Generic;
using Fabron.Models;
using Mapster;

namespace Fabron.Providers.Cassandra.Models
{
    public class CronJobMetadata
    {
        public string Key { get; set; }
        public string Uid { get; set; }
        public DateTime CreationTimestamp { get; set; }
        public Dictionary<string, string> Labels { get; set; }
        public Dictionary<string, string> Annotations { get; set; }
    }

    public class CronJobSpec
    {
        public string Schedule { get; set; }
        public string CommandName { get; set; }
        public string CommandData { get; set; }
        public DateTime? NotBefore { get; set; }
        public DateTime? ExpirationTime { get; set; }
        public bool Suspend { get; set; }
    }

    public class CronJobStatus
    {
        public DateTime? CompletionTimestamp { get; set; }
        public string Reason { get; set; }
        public bool Deleted { get; set; }
    }

    public class CronJobDocument
    {
        public CronJobMetadata Metadata { get; set; }
        public CronJobSpec Spec { get; set; }
        public CronJobStatus Status { get; set; }
        public long Version { get; set; }

        public static CronJobDocument FromResource(Fabron.Models.CronJob resource)
        {
            return new CronJobDocument
            {
                Metadata = new CronJobMetadata
                {
                    Key = resource.Metadata.Key,
                    Uid = resource.Metadata.Uid,
                    CreationTimestamp = resource.Metadata.CreationTimestamp,
                    Labels = resource.Metadata.Labels,
                    Annotations = resource.Metadata.Annotations,
                },
                Spec = new CronJobSpec
                {
                    Schedule = resource.Spec.Schedule,
                    CommandName = resource.Spec.CommandName,
                    CommandData = resource.Spec.CommandData,
                    NotBefore = resource.Spec.NotBefore,
                    ExpirationTime = resource.Spec.ExpirationTime,
                    Suspend = resource.Spec.Suspend
                },
                Status = new CronJobStatus
                {
                    CompletionTimestamp = resource.Status.CompletionTimestamp,
                    Reason = resource.Status.Reason,
                    Deleted = resource.Status.Deleted
                },
                Version = resource.Version,
            };
        }

        public Fabron.Models.CronJob ToResource()
        {
            return this.Adapt<Fabron.Models.CronJob>();
        }
    }

}
