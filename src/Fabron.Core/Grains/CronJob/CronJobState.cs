using System;
using System.Collections.Generic;
using System.Linq;

namespace Fabron.Grains.CronJob
{
    public class CronJobState
    {
#nullable disable
        public CronJobState() { }
#nullable enable
        public CronJobState(string cronExp, JobCommandInfo command)
        {
            CronExp = cronExp;
            Command = command;
        }

        public string CronExp { get; }
        public JobCommandInfo Command { get; }
        private readonly List<CronJobStateChild> _childJobs = new();
        public CronJobStateChild? LatestJob => _childJobs.LastOrDefault();
        public IReadOnlyCollection<CronJobStateChild> ChildJobs => _childJobs.AsReadOnly();
        public IEnumerable<CronJobStateChild> NotCreatedJobs => _childJobs.Where(job => job.Status == JobStatus.NotCreated);
        public IEnumerable<CronJobStateChild> CreatedJobs => _childJobs.Where(job => job.Status != JobStatus.NotCreated);
        public IEnumerable<CronJobStateChild> PendingJobs => _childJobs.Where(job => !job.IsFinished && job.Status != JobStatus.NotCreated);
        public IEnumerable<CronJobStateChild> UnFinishedJobs => _childJobs.Where(job => !job.IsFinished);
        public IEnumerable<CronJobStateChild> FinishedJobs => _childJobs.Where(job => job.IsFinished);
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

        public void SetNextReminder()
        {

        }

        public void Schedule(DateTime toTime)
        {
            var cron = Cronos.CronExpression.Parse(CronExp);
            var lastedJob = LatestJob;
            DateTime lastestScheduledAt = lastedJob is null ? DateTime.UtcNow : lastedJob.ScheduledAt;
            var nextSchedule = cron.GetNextOccurrence(lastestScheduledAt);
            if (nextSchedule is null) return;

            var nextJob = new CronJobStateChild(nextSchedule.Value);
            _childJobs.Add(nextJob);

            if (nextSchedule < toTime)
            {
                IEnumerable<DateTime> occurrences = cron.GetOccurrences(nextSchedule.Value, toTime, false);
                IEnumerable<CronJobStateChild> jobsToSchedule = occurrences
                    .Select(occ => new CronJobStateChild(occ));
                _childJobs.AddRange(jobsToSchedule);
            }
        }

    }
}
