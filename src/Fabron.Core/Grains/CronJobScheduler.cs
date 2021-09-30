using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabron.Grains;
using Fabron.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Diagnostics;
using Orleans;
using Orleans.Placement;
using Orleans.Runtime;
using static Fabron.FabronConstants;

namespace Fabron.Grains
{
    public interface ICronJobScheduler : IGrainWithStringKey
    {
        Task Start();

        Task Stop();

        Task<bool> IsRunning();

        Task Trigger();
    }

    [PreferLocalPlacement]
    public class CronJobScheduler : Grain, ICronJobScheduler, IRemindable
    {
        private readonly TimeSpan _defaultTickPeriod = TimeSpan.FromMinutes(2);
        private readonly ILogger<CronJobScheduler> _logger;
        private readonly CronJobOptions _options;
        private IGrainReminder? _tickReminder;
        public CronJobScheduler(
            ILogger<CronJobScheduler> logger,
            IOptions<CronJobOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }
        private string _key = default!;
        private ICronJobGrain _self = default!;
        private DateTime? _lastScheduledTick;
        private bool _isTicking;

        public override Task OnActivateAsync()
        {
            _key = this.GetPrimaryKeyString();
            _self = GrainFactory.GetGrain<ICronJobGrain>(_key);
            return Task.CompletedTask;
        }

        public async Task Start()
        {
            _lastScheduledTick = DateTime.UtcNow;
            await Tick();
        }

        public async Task Stop()
        {
            await StopTicker();
        }

        public async Task<bool> IsRunning()
        {
            if (_tickReminder is null)
                _tickReminder = await GetReminder(Names.TickerReminder);
            return _tickReminder is not null;
        }

        public async Task Trigger()
        {
            var state = await _self.GetState();
            Guard.IsNotNull(state, nameof(state));
            await ScheduleJob(state, DateTime.UtcNow);
        }


        private async Task Tick()
        {
            if(_isTicking) return;
            _isTicking = true;
            try
            {
                await TickInternal();
            }
            finally
            {
                _isTicking = false;
            }
        }

        private async Task TickInternal()
        {
            while (true)
            {
                var state = await _self.GetState();

                // Stopped
                if (state is null || state.Status.Deleted || state.Spec.Suspend || state.Status.CompletionTimestamp.HasValue)
                {
                    await StopTicker();
                    return;
                }

                DateTime now = DateTime.UtcNow;
                if (state.Spec.NotBefore.HasValue && now < state.Spec.NotBefore.Value)
                {
                    await TickAfter(state.Spec.NotBefore.Value.Subtract(now));
                    return;
                }

                Cronos.CronExpression cron = Cronos.CronExpression.Parse(state.Spec.Schedule, _options.CronFormat);
                DateTime? tick;
                DateTime from = now.AddSeconds(-10);
                if (_lastScheduledTick.HasValue && _lastScheduledTick.Value > from)
                {
                    from = _lastScheduledTick.Value;
                }
                tick = cron.GetNextOccurrence(from);
                // Completed
                if (tick is null || (state.Spec.ExpirationTime.HasValue && tick.Value > state.Spec.ExpirationTime.Value))
                {
                    await Complete();
                    return;
                }

                // Just at the time to schedule new job
                if (tick.Value <= now.AddSeconds(2))
                {
                    await ScheduleJob(state, tick.Value);
                }
                else // not at the time
                {
                    await TickAfter(tick.Value.Subtract(now));
                    // _logger.LogWarning("Tick missed");
                    return;
                }
                // // Check if we need to set ticker for next schedule
                // tick = cron.GetNextOccurrence(tick.Value);
                // now = DateTime.UtcNow;
                // // Completed
                // if (tick is null || (state.Spec.ExpirationTime.HasValue && tick.Value > state.Spec.ExpirationTime.Value))
                // {
                //     await Complete();
                //     return;
                // }
                // else
                // {
                //     if (tick.Value > now)
                //     {
                //         await TickAfter(tick.Value.Subtract(now));
                //         return;
                //     }
                //     else if (tick.Value <= now.AddSeconds(5))
                //     {
                //         await TickAfter(TimeSpan.Zero);
                //         return;
                //     }
                //     else
                //     {
                //         _logger.LogWarning("Tick missed");
                //     }
                // }
            }
        }

        private async Task Complete()
        {
            _logger.CompletingCronJob(_key);
            await _self.Complete();
        }

        private async Task ScheduleJob(CronJob state, DateTime schedule)
        {
            string jobKey = state.GetChildJobKeyByIndex(schedule);
            _logger.SchedulingNewJob(_key, jobKey);

            IJobGrain grain = GrainFactory.GetGrain<IJobGrain>(jobKey);
            var labels = new Dictionary<string, string>(state.Metadata.Labels)
                {
                    { LabelNames.OwnerId, state.Metadata.Uid },
                    { LabelNames.OwnerKey, state.Metadata.Key },
                    { LabelNames.OwnerType , OwnerTypes.CronJob },
                };
            var annotations = new Dictionary<string, string>(state.Metadata.Annotations)
            {
            };
            Job jobState = await grain.Schedule(
                state.Spec.CommandName,
                state.Spec.CommandData,
                schedule,
                labels,
                annotations);

            _lastScheduledTick = schedule;

            _logger.ScheduledNewJob(_key, jobKey);
        }

        private async Task TickAfter(TimeSpan dueTime)
        {
            Guard.IsBetweenOrEqualTo(dueTime.Ticks, -1, long.MaxValue, nameof(dueTime));
            _tickReminder = await RegisterOrUpdateReminder(Names.TickerReminder, dueTime, _defaultTickPeriod);
            _logger.TickerRegistered(_key, dueTime);
        }

        private async Task StopTicker()
        {
            int retry = 0;
            while (true)
            {
                _tickReminder = await GetReminder(Names.TickerReminder);
                if (_tickReminder is null) break;
                try
                {
                    await UnregisterReminder(_tickReminder);
                    _tickReminder = null;
                    _logger.TickerStopped(_key);
                    break;
                }
                catch (Orleans.Runtime.ReminderException)
                {
                    if (retry++ < 3)
                    {
                        _logger.RetryUnregisterReminder(_key);
                        continue;
                    }
                    throw;
                }
                catch (OperationCanceledException)
                {
                    // ReminderService has been stopped
                    return;
                }
            }
        }

        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            if (_tickReminder is null)
            {
                _tickReminder = await GetReminder(Names.TickerReminder);
            }
            await Tick();
        }
    }
}
