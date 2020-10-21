using System.Collections.Generic;
using System.Linq;

namespace TGH.Grains.BatchJob
{
    public class CreateChildJob
    {
        public string CommandName { get; init; } = null!;
        public string CommandData { get; init; } = null!;
    }

    public class CreateBatchJob
    {
        public List<CreateChildJob> ChildJobs { get; init; } = null!;
    }

    public static class CreateBatchJobExtensions
    {
        public static BatchJobState Create(this CreateBatchJob create)
        {
            return new BatchJobState(create.ChildJobs.Select(j => j.Create()).ToList());
        }

        public static ChildJobState Create(this CreateChildJob create)
        {
            return new(new(create.CommandName, create.CommandData));
        }

    }
}
