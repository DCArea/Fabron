using System.Threading.Tasks;
using Fabron;
using FabronService.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FabronService.Resources
{
    [ApiController]
    [Route("APIReminders")]
    public class APIReminderResourceController : ControllerBase
    {
        private readonly ILogger<APIReminderResourceController> _logger;
        private readonly IJobManager _jobManager;

        public APIReminderResourceController(ILogger<APIReminderResourceController> logger,
            IJobManager jobManager)
        {
            _logger = logger;
            _jobManager = jobManager;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateAPIReminderResourceRequest req)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"Creating APIReminder: {req.Name}({req.Schedule})");
            }
            var job = await _jobManager.Schedule<RequestWebAPI, int>(req.Name, req.Command, req.Schedule);
            var reminder = job.ToResource(req.Name);
            return CreatedAtAction(nameof(Get), new { name = reminder.Name }, reminder);
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> Get(string name)
        {
            var job = await _jobManager.GetJobById<RequestWebAPI, int>(name);
            if (job is null)
                return NotFound();
            var reminder = job.ToResource(name);
            return Ok(reminder);
        }

    }
}
