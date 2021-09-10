
using Fabron.Models;

namespace Fabron.ElasticSearch
{
    public record JobDocument(
        string Id,
        JobMetadata Metadata,
        JobSpec Spec,
        JobStatus Status,
        long Version);
}
