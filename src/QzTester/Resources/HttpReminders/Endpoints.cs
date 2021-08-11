// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Fabron;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Quartz;

namespace FabronService.Resources.HttpReminders
{
    [ApiController]
    [Route("HttpReminders")]
    public class Endpoint : ControllerBase
    {
        private readonly ILogger<Endpoint> _logger;
        private readonly ISchedulerFactory _schedulerFactory;

        public Endpoint(ILogger<Endpoint> logger,
            ISchedulerFactory schedulerFactory)
        {
            _logger = logger;
            _schedulerFactory = schedulerFactory;
        }

        [HttpPost]
        public async Task<IActionResult> Create(RegisterHttpReminderRequest req)
        {
            IJobDetail job = JobBuilder.Create<InvokeHttpRequest>()
                .WithIdentity(req.Name, "group1")
                .UsingJobData("URL", req.Command.Url)
                .UsingJobData("METHOD", req.Command.HttpMethod)
                .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(req.Name, "group1")
                .StartAt(req.Schedule)
                .Build();

            var scheduler = await _schedulerFactory.GetScheduler("Scheduler-Core") ?? throw new KeyNotFoundException();
            await scheduler.ScheduleJob(job, trigger);
            MetricsHelper.JobCount_Scheduled.Inc();

            return CreatedAtAction(nameof(Create), new { name = req.Name }, req);
        }
    }

    public class InvokeHttpRequest : IJob, IDisposable
    {
        private readonly ILogger<InvokeHttpRequest> logger;
        private readonly HttpClient _client;

        public InvokeHttpRequest(ILogger<InvokeHttpRequest> logger, HttpClient client)
        {
            this.logger = logger;
            _client = client;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            MetricsHelper.JobCount_Running.Inc();
            var tardiness = DateTimeOffset.UtcNow.Subtract(context.Trigger.StartTimeUtc);
            MetricsHelper.JobScheduleTardiness.Observe(tardiness.TotalSeconds);

            JobDataMap dataMap = context.JobDetail.JobDataMap;

            string url = dataMap.GetString("URL") ?? throw new InvalidOperationException();
            string method = dataMap.GetString("METHOD") ?? throw new InvalidOperationException();

            logger.LogDebug($"{url} {method}");
            HttpResponseMessage res = await _client.GetAsync(url);
            context.Result = res.StatusCode;

            MetricsHelper.JobCount_RanToCompletion.Inc();
        }

        public void Dispose()
        {
        }
    }
}
