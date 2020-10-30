using System.Collections.Generic;
using System.Linq;

namespace Fabron.Grains.BatchJob
{
    // public class CreateBatchJobRequestRequest
    // {
    //     public List<JobCommandInfo> Commands { get; init; } = null!;
    // }

    // public static class CreateBatchJobExtensions
    // {
    //     public static BatchJobState Create(this CreateBatchJobRequestRequest create)
    //     {
    //         return new BatchJobState(create.ChildJobs.Select(j => j.Create()).ToList());
    //     }

    //     public static ChildJobState Create(this CreateChildJob create)
    //     {
    //         return new(new(create.CommandName, create.CommandData));
    //     }

    // }
}
