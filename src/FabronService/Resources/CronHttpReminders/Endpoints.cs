

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabron;
using Fabron.Contracts;
using FabronService.Commands;
using FabronService.Resources.CronHttpReminders.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FabronService.Resources.CronHttpReminders
{
    [ApiController]
    [Route("CronHttpReminders")]
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

        [HttpPost(Name = "CronHttpReminders_Register")]
        public async Task<IActionResult> Create(RegisterCronHttpReminderRequest req)
        {
            string? tenantId = HttpContext.User.Identity!.Name!;
            string? resourceUri = $"tenants/{tenantId}/CronHttpReminders/{req.Name}";
            CronJob<RequestWebAPI>? job = await _jobManager.ScheduleCronJob<RequestWebAPI>(
                resourceUri,
                req.Schedule,
                req.Command,
                req.NotBefore,
                req.ExpirationTime,
                false,
                new Dictionary<string, string> { { "tenant", tenantId } },
                null);
            CronHttpReminder? reminder = job.ToResource(req.Name);
            return CreatedAtRoute("CronHttpReminders_Get", new { name = reminder.Name }, reminder);
        }

        [HttpGet("{name}", Name = "CronHttpReminders_Get")]
        public async Task<IActionResult> Get(string name)
        {
            string? tenantId = HttpContext.User.Identity!.Name;
            string? resourceUri = $"tenants/{tenantId}/CronHttpReminders/{name}";

            CronJob<RequestWebAPI>? job = await _jobManager.GetCronJobById<RequestWebAPI>(resourceUri);
            return job is null ? NotFound() : Ok(job.ToResource(name));
        }

        [HttpGet("{name}/items", Name = "CronHttpReminders_GetItems")]
        public async Task<IActionResult> GetItems(string name)
        {
            string? tenantId = HttpContext.User.Identity!.Name;
            string? resourceUri = $"tenants/{tenantId}/CronHttpReminders/{name}";

            IEnumerable<Job<RequestWebAPI, int>>? jobs = await _jobManager.GetJobByCron<RequestWebAPI, int>(resourceUri);
            return Ok(jobs.Select(job => job.ToResource()));
        }
    }
}
