
using System.Collections.Generic;
using System.Linq;
using Fabron.Models;

namespace Fabron.ElasticSearch
{
    public record JobDocument(
        string Id,
        JobMetadata Metadata,
        JobSpec Spec,
        JobStatus Status,
        long Version);

    public static class DocumentExtensions
    {
        public static string ToNormalized(this string source)
        {
            return source.Replace('.', '_').Replace('/', '+');
        }
        public static string ToDenormalized(this string source)
        {
            return source.Replace('_', '.').Replace('+', '/');
        }

        public static Dictionary<string, string> ToNormalized(this Dictionary<string, string> source)
        {
            return source
                .Select(kv => new KeyValuePair<string, string>(kv.Key.ToNormalized(), kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public static Dictionary<string, string> ToDenormalized(this Dictionary<string, string> source)
        {
            return source
                .Select(kv => new KeyValuePair<string, string>(kv.Key.ToDenormalized(), kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public static JobDocument ToDocument(this Fabron.Models.Job job)
        {
            JobDocument doc = new(
                job.Metadata.Key,
                job.Metadata with
                {
                    Labels = job.Metadata.Labels.ToNormalized(),
                    Annotations = job.Metadata.Annotations.ToNormalized(),
                },
                job.Spec,
                job.Status,
                job.Version);
            return doc;
        }

        public static Fabron.Models.Job ToResource(this JobDocument job)
        {
            var resource = new Models.Job(
                job.Metadata with
                {
                    Labels = job.Metadata.Labels.ToDenormalized(),
                    Annotations = job.Metadata.Annotations.ToDenormalized(),
                },
                job.Spec,
                job.Status,
                job.Version
            );
            return resource;
        }

        public static CronJobDocument ToDocument(this Fabron.Models.CronJob job)
        {
            CronJobDocument doc = new(
                job.Metadata.Key,
                job.Metadata with
                {
                    Labels = job.Metadata.Labels.ToNormalized(),
                    Annotations = job.Metadata.Annotations.ToNormalized(),
                },
                job.Spec,
                job.Status,
                job.Version);
            return doc;
        }

        public static Fabron.Models.CronJob ToResource(this CronJobDocument job)
        {
            var resource = new Models.CronJob(
                job.Metadata with
                {
                    Labels = job.Metadata.Labels.ToDenormalized(),
                    Annotations = job.Metadata.Annotations.ToDenormalized(),
                },
                job.Spec,
                job.Status,
                job.Version
            );
            return resource;
        }
    }
}
