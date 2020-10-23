using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using TGH.Server.Entities;

namespace TGH.Server.Controllers
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
        public async Task<IActionResult> GetTransientJobState(Guid id)
        {
            var job = await _jobManager.GetJobById<RequestWebAPI, int>(id);
            return Ok(job);
        }

        [HttpPost("Batch")]
        public async Task<IActionResult> CreateBatchJob(BatchCreateRequestWebAPIJobRequest req)
        {
            await _jobManager.Enqueue(req.RequestId, req.Commands);
            return CreatedAtAction(nameof(GetBatchJobState), new { Id = req.RequestId }, new { Id = req.RequestId });
        }
        [HttpGet("Batch/{id}")]
        public async Task<IActionResult> GetBatchJobState(Guid id)
        {
            var job = await _jobManager.GetBatchJobById(id);
            return Ok(job);
        }
    }
}
