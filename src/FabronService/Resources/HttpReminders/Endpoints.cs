
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron;
using Fabron.Contracts;
using FabronService.Commands;
using FabronService.Resources.HttpReminders.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FabronService.Resources.HttpReminders
{
    [ApiController]
    [Route("HttpReminders")]
    public class Endpoints : ControllerBase
    {
        private readonly ILogger<Endpoints> _logger;
        private readonly IJobManager _jobManager;

        public Endpoints(ILogger<Endpoints> logger,
            IJobManager jobManager)
        {
            _logger = logger;
            _jobManager = jobManager;
        }

        [HttpPost(Name = "HttpReminders_Register")]
        public async Task<IActionResult> Create(RegisterHttpReminderRequest req)
        {
            string tenantId = HttpContext.User.Identity!.Name!;
            string resourceUri = $"tenants/{tenantId}/HttpReminders/{req.Name}";
            Job<RequestWebAPI, int>? job = await _jobManager.ScheduleJob<RequestWebAPI, int>(
                resourceUri,
                req.Command,
                req.Schedule,
                new Dictionary<string, string> { { "tenant", tenantId } },
                null);
            HttpReminder reminder = job.ToResource(req.Name);
            return CreatedAtRoute("HttpReminders_Get", new { name = reminder.Name }, reminder);
        }

        [HttpGet("{name}", Name = "HttpReminders_Get")]
        public async Task<IActionResult> Get(string name)
        {
            string tenantId = HttpContext.User.Identity!.Name!;
            string resourceUri = $"tenants/{tenantId}/HttpReminders/{name}";

            Job<RequestWebAPI, int>? job = await _jobManager.GetJob<RequestWebAPI, int>(resourceUri);
            return job is null ? NotFound() : Ok(job.ToResource(name));
        }

    }
}
