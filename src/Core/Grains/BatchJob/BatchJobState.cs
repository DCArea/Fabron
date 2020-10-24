using System.Collections.Generic;
using System.Linq;

namespace TGH.Grains.BatchJob
{

    public class BatchJobState
    {
#nullable disable
        public BatchJobState() { }
#nullable enable
        public BatchJobState(List<JobCommandInfo> commands)
        {
            _childJobs = commands
                .Select(cmd => new BatchJobStateChild(cmd))
                .ToList();
        }

        private readonly List<BatchJobStateChild> _childJobs = null!;
        public IEnumerable<BatchJobStateChild> PendingJobs => _childJobs.Where(job => job.Status == JobStatus.NotCreated);
        public IEnumerable<BatchJobStateChild> EnqueuedJobs => _childJobs.Where(job => !job.IsFinished && job.Status != JobStatus.NotCreated);
        public IEnumerable<BatchJobStateChild> FinishedJobs => _childJobs.Where(job => job.IsFinished);

        public string? Reason { get; private set; }

        public JobStatus Status { get; private set; }

        public void Start()
        {
            Status = JobStatus.Running;
        }

        public void Complete()
        {
            Status = JobStatus.RanToCompletion;
        }

        public void Cancel(string reason)
        {
            Status = JobStatus.Canceled;
            Reason = reason;
        }

    }
}
