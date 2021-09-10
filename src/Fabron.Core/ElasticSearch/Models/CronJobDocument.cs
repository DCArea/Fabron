
using Fabron.Models;

namespace Fabron.ElasticSearch
{
    public record CronJobDocument(
        string Id,
        CronJobMetadata Metadata,
        CronJobSpec Spec,
        CronJobStatus Status,
        long Version);
}
