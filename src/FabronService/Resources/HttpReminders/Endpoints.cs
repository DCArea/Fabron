// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

using Fabron;
using Fabron.Contracts;

using FabronService.Commands;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FabronService.Resources.HttpReminders
{
    [ApiController]
    [Route("HttpReminders")]
    public class Endpoint : ControllerBase
    {
        private readonly ILogger<Endpoint> _logger;
        private readonly IJobManager _jobManager;

        public Endpoint(ILogger<Endpoint> logger,
            IJobManager jobManager)
        {
            _logger = logger;
            _jobManager = jobManager;
        }

        [HttpPost]
        public async Task<IActionResult> Create(RegisterHttpReminderRequest req)
        {
            Job<RequestWebAPI, int>? job = await _jobManager.Schedule<RequestWebAPI, int>(req.Name, req.Command, req.Schedule);
            HttpReminder reminder = job.ToResource(req.Name);
            return CreatedAtAction(nameof(Get), new { name = reminder.Name }, reminder);
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> Get(string name)
        {
            Job<RequestWebAPI, int>? job = await _jobManager.GetJobById<RequestWebAPI, int>(name);
            return job is null ? NotFound() : Ok(job.ToResource(name));
        }

    }
}
