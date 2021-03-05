using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using Fabron.Server.Entities;
using FabronService.Commands;

namespace Fabron.Server.Controllers
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
            var job = await _jobManager.Schedule<RequestWebAPI, int>(req.RequestId, req.Command, req.ScheduledAt);
            return CreatedAtAction(nameof(GetTransientJobState), new { Id = req.RequestId }, job);
        }

        [HttpGet("Transient/{id}")]
        public async Task<IActionResult> GetTransientJobState(string id)
        {
            var job = await _jobManager.GetJobById<RequestWebAPI, int>(id);
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
            var job = await _jobManager.GetBatchJobById(id);
            return Ok(job);
        }

        [HttpPost("Cron")]
        public async Task<IActionResult> CreateCronJob(CreateRequestWebAPICronJobRequest req)
        {
            var job = await _jobManager.Schedule(req.RequestId, req.CronExp, req.Command);
            return CreatedAtAction(nameof(GetCronJob), new { Id = req.RequestId }, job);
        }

        [HttpGet("Cron/{id}")]
        public async Task<IActionResult> GetCronJob(string id)
        {
            var job = await _jobManager.GetCronJob(id);
            return Ok(job);
        }

        [HttpGet("Cron/{id}/Detail")]
        public async Task<IActionResult> GetCronJobDetail(string id)
        {
            var job = await _jobManager.GetCronJobDetail(id);
            return Ok(job);
        }
    }
}
