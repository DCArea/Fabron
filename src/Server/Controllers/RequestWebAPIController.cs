using System;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using TGH.Server.Entities;
using TGH.Server.Grains;

namespace TGH.Server.Controllers
{
    [ApiController]
    [Route("Jobs/[controller]")]
    public class RequestWebAPIController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IClusterClient _client;

        public RequestWebAPIController(ILogger<RequestWebAPIController> logger,
            IClusterClient client)
        {
            _logger = logger;
            _client = client;
        }

        [HttpPost("Transient")]
        public async Task<IActionResult> CreateTransientJob(CreateRequestWebAPIJobRequest req)
        {
            _logger.LogInformation($"Creating {req.RequestId}");
            var grain = _client.GetGrain<IJobGrain<RequestWebAPICommand, int>>(req.RequestId);
            await grain.Create(req.Command);
            _logger.LogInformation($"Job {req.RequestId} Created");
            var job = await grain.GetState();
            return CreatedAtAction(nameof(GetTransientJobState), new { Id = req.RequestId }, job);
        }

        [HttpGet("Transient/{id}")]
        public async Task<IActionResult> GetTransientJobState(Guid id)
        {
            var grain = _client.GetGrain<IJobGrain<RequestWebAPICommand, int>>(id);
            var job = await grain.GetState();
            return Ok(job);
        }
    }
}
