using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabron.Grains;
using Fabron.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Diagnostics;
using Orleans;
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

    public class CronJobScheduler : Grain, ICronJobScheduler, IRemindable
    {
        private readonly TimeSpan _defaultTickPeriod = TimeSpan.FromMinutes(2);
        private readonly ILogger<CronJobScheduler> _logger;
        private IGrainReminder? _tickReminder;
        public CronJobScheduler(ILogger<CronJobScheduler> logger)
        {
            _logger = logger;
        }
        private string _key = default!;
        private ICronJobGrain _self = default!;

        public override Task OnActivateAsync()
        {
            _key = this.GetPrimaryKeyString();
            _self = GrainFactory.GetGrain<ICronJobGrain>(_key);
            return Task.CompletedTask;
        }

        public async Task Start()
        {
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

            Cronos.CronExpression cron = Cronos.CronExpression.Parse(state.Spec.Schedule);
            DateTime? tick;
            tick = cron.GetNextOccurrence(now.AddSeconds(-5));
            // Completed
            if (tick is null || (state.Spec.ExpirationTime.HasValue && tick.Value > state.Spec.ExpirationTime.Value))
            {
                await Complete();
                return;
            }

            // Just at the time to schedule new job
            if (tick.Value <= now.AddSeconds(5))
            {
                await ScheduleJob(state, tick.Value);
            }
            // Check if we need to set ticker for next schedule
            tick = cron.GetNextOccurrence(tick.Value);
            now = DateTime.UtcNow;
            // Completed
            if (tick is null || (state.Spec.ExpirationTime.HasValue && tick.Value > state.Spec.ExpirationTime.Value))
            {
                await Complete();
                return;
            }
            else
            {
                if (tick.Value > now)
                {
                    await TickAfter(tick.Value.Subtract(now));
                    return;
                }
                else if (tick.Value <= now.AddSeconds(5))
                {
                    await TickAfter(TimeSpan.Zero);
                    return;
                }
                else
                {
                    _logger.LogWarning("Missed tick");
                }
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
                null,
                labels,
                annotations);

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
