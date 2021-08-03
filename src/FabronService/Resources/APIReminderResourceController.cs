// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            Fabron.Contracts.Job<RequestWebAPI, int>? job = await _jobManager.Schedule<RequestWebAPI, int>(req.Name, req.Command, req.Schedule);
            APIReminderResource? reminder = job.ToResource(req.Name);
            return CreatedAtAction(nameof(Get), new { name = reminder.Name }, reminder);
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> Get(string name)
        {
            Fabron.Contracts.Job<RequestWebAPI, int>? job = await _jobManager.GetJobById<RequestWebAPI, int>(name);
            if (job is null)
            {
                return NotFound();
            }

            APIReminderResource? reminder = job.ToResource(name);
            return Ok(reminder);
        }

    }
}
