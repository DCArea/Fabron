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

    public class CronJobScheduler : Grain, ICronJobScheduler
    {
        private readonly TimeSpan _defaultTickPeriod = TimeSpan.FromMinutes(2);
        private readonly ILogger<CronJobScheduler> _logger;
        private IGrainReminder? _tickReminder;
        private IDisposable? _tickTimer;

        public CronJobScheduler(ILogger<CronJobScheduler> logger)
        {
            _logger = logger;
        }
        private string _key = default!;
        private ICronJobGrain _self = default!;

        public override async Task OnActivateAsync()
        {
            _key = this.GetPrimaryKeyString();
            _self = GrainFactory.GetGrain<ICronJobGrain>(_key);
            _tickReminder = await GetReminder("Ticker");
        }

        public async Task Start()
        {
            await Tick();
        }

        public async Task Stop()
        {
            await StopTicker();
        }

        public Task<bool> IsRunning() => Task.FromResult(_tickReminder is not null);

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
            now = DateTime.UtcNow;
            tick = cron.GetNextOccurrence(now.AddSeconds(-5));
            // Completed
            if (tick is null || (state.Spec.ExpirationTime.HasValue && tick.Value > state.Spec.ExpirationTime.Value))
            {
                await Complete();
                return;
            }
            else
            {
                await TickAfter(tick.Value.Subtract(now));
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
            _tickTimer?.Dispose();
            if (dueTime < _defaultTickPeriod)
            {
                _tickTimer = RegisterTimer(_ => Tick(), null, dueTime, TimeSpan.FromMilliseconds(-1));
                if (_tickReminder is null)
                {
                    _tickReminder = await RegisterOrUpdateReminder("Ticker", dueTime.Add(_defaultTickPeriod), _defaultTickPeriod);
                }
            }
            else
            {
                _tickReminder = await RegisterOrUpdateReminder("Ticker", dueTime, _defaultTickPeriod);
            }
            _logger.TickerRegistered(_key, dueTime);
        }

        private async Task StopTicker()
        {
            _tickTimer?.Dispose();
            IGrainReminder? reminder = null;
            if (_tickReminder is null)
            {
                _tickReminder = await GetReminder("Ticker");
            }
            reminder = _tickReminder;
            int retry = 0;
            while (true)
            {
                if (reminder is null) break;
                try
                {
                    await UnregisterReminder(reminder);
                    _logger.TickerStopped(_key);
                    break;
                }
                catch (Orleans.Runtime.ReminderException)
                {
                    if (retry++ < 3)
                    {
                        reminder = await GetReminder("Ticker");
                        continue;
                    }
                    throw;
                }
            }
        }
    }
}
