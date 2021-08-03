// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

using Fabron;

using FabronService.Commands;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Orleans;

namespace FabronService.Resources
{
    [ApiController]
    [Route("Jobs/[controller]")]
    public class RequestWebAPIController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IJobManager _jobManager;
        private readonly IClusterClient _client;

        public RequestWebAPIController(ILogger<RequestWebAPIController> logger,
            IJobManager jobManager,
            IClusterClient client)
        {
            _logger = logger;
            _jobManager = jobManager;
            _client = client;
        }

        [HttpPost("Transient")]
        public async Task<IActionResult> CreateTransientJob(CreateRequestWebAPIJobRequest req)
        {
            Fabron.Contracts.Job<RequestWebAPI, int>? job = await _jobManager.Schedule<RequestWebAPI, int>(req.RequestId, req.Command, req.ScheduledAt);
            return CreatedAtAction(nameof(GetTransientJobState), new { Id = req.RequestId }, job);
        }

        [HttpGet("Transient/{id}")]
        public async Task<IActionResult> GetTransientJobState(string id)
        {
            Fabron.Contracts.Job<RequestWebAPI, int>? job = await _jobManager.GetJobById<RequestWebAPI, int>(id);
            return Ok(job);
        }

        [HttpPost("Batch")]
        public async Task<IActionResult> CreateBatchJob(BatchCreateRequestWebAPIJobRequest req)
        {
            await _jobManager.Schedule(req.RequestId, req.Commands);
            return CreatedAtAction(nameof(GetBatchJobState), new { Id = req.RequestId }, new { Id = req.RequestId });
        }

        [HttpGet("Batch/{id}")]
        public async Task<IActionResult> GetBatchJobState(string id)
        {
            Fabron.Contracts.BatchJob? job = await _jobManager.GetBatchJobById(id);
            return Ok(job);
        }

        [HttpPost("Cron")]
        public async Task<IActionResult> CreateCronJob(CreateRequestWebAPICronJobRequest req)
        {
            Fabron.Contracts.CronJob? job = await _jobManager.Schedule(req.RequestId, req.CronExp, req.Command);
            return CreatedAtAction(nameof(GetCronJob), new { Id = req.RequestId }, job);
        }

        [HttpGet("Cron/{id}")]
        public async Task<IActionResult> GetCronJob(string id)
        {
            Fabron.Contracts.CronJob? job = await _jobManager.GetCronJob(id);
            return Ok(job);
        }

        [HttpGet("Cron/{id}/Detail")]
        public async Task<IActionResult> GetCronJobDetail(string id)
        {
            Fabron.Contracts.CronJobDetail? job = await _jobManager.GetCronJobDetail(id);
            return Ok(job);
        }
    }
}
